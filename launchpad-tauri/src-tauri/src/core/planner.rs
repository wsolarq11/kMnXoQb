//! Pure launch decision: given a launch item and terminal availability,
//! produce the exact argv to spawn. Ported 1:1 from C# LaunchPlanner,
//! including the two bug fixes over the legacy Rust POC:
//! 1. cmd fallback: `cd /d "..." &&` prefix breaks on quoted dirs (cmd /k does
//!    not parse standard argv quoting) — the directory travels via
//!    current_dir instead.
//! 2. pwsh fallback: single quotes in the directory are doubled.

use crate::core::danger;
use crate::core::models::{LaunchItem, LaunchPlan};

pub fn plan_windows(item: &LaunchItem, wt_available: bool, pwsh_available: bool) -> LaunchPlan {
    let dir = &item.directory;
    let terminal = item.terminal.as_deref().unwrap_or("pwsh");
    let dangerous = danger::is_dangerous(&item.command);

    if wt_available {
        LaunchPlan {
            executable: "wt.exe".to_string(),
            args: vec![
                "new-tab".to_string(),
                "-d".to_string(),
                dir.clone(),
                terminal.to_string(),
                "-NoExit".to_string(),
                "-Command".to_string(),
                item.command.clone(),
            ],
            working_directory: dir.clone(),
            is_dangerous: dangerous,
            terminal_override: item.terminal.clone(),
        }
    } else if pwsh_available {
        LaunchPlan {
            executable: "pwsh.exe".to_string(),
            args: vec![
                "-NoExit".to_string(),
                "-Command".to_string(),
                format!("cd '{}'; {}", escape_pwsh_quotes(dir), item.command),
            ],
            working_directory: dir.clone(),
            is_dangerous: dangerous,
            terminal_override: item.terminal.clone(),
        }
    } else {
        LaunchPlan {
            executable: "cmd.exe".to_string(),
            args: vec!["/k".to_string(), item.command.clone()],
            working_directory: dir.clone(),
            is_dangerous: dangerous,
            terminal_override: item.terminal.clone(),
        }
    }
}

/// PowerShell single-quoted strings escape a quote by doubling it.
pub fn escape_pwsh_quotes(path: &str) -> String {
    path.replace('\'', "''")
}

#[cfg(test)]
mod tests {
    use super::*;

    fn item(command: &str, dir: &str, terminal: Option<&str>) -> LaunchItem {
        LaunchItem {
            name: "demo".to_string(),
            directory: dir.to_string(),
            command: command.to_string(),
            confirm: true,
            id: "demo".to_string(),
            selected: false,
            terminal: terminal.map(str::to_string),
            tag: None,
            group: None,
        }
    }

    fn default_item() -> LaunchItem {
        item("snow", r"D:\projects\demo", None)
    }

    #[test]
    fn prefers_windows_terminal() {
        let plan = plan_windows(&default_item(), true, true);
        assert_eq!("wt.exe", plan.executable);
        assert_eq!(
            vec!["new-tab", "-d", r"D:\projects\demo", "pwsh", "-NoExit", "-Command", "snow"],
            plan.args
        );
        assert_eq!(r"D:\projects\demo", plan.working_directory);
    }

    #[test]
    fn falls_back_to_pwsh() {
        let plan = plan_windows(&default_item(), false, true);
        assert_eq!("pwsh.exe", plan.executable);
        assert_eq!(
            vec!["-NoExit", "-Command", "cd 'D:\\projects\\demo'; snow"],
            plan.args
        );
    }

    #[test]
    fn falls_back_to_cmd() {
        let plan = plan_windows(&default_item(), false, false);
        assert_eq!("cmd.exe", plan.executable);
        // Directory travels via working_directory: cmd /k does not use
        // standard argv quoting, a cd prefix would break.
        assert_eq!(vec!["/k", "snow"], plan.args);
        assert_eq!(r"D:\projects\demo", plan.working_directory);
    }

    #[test]
    fn uses_terminal_override_when_present() {
        let plan = plan_windows(&item("snow", r"D:\projects\demo", Some("pwsh")), true, true);
        assert_eq!("pwsh", plan.args[3]);
        assert_eq!(Some("pwsh".to_string()), plan.terminal_override);
    }

    #[test]
    fn defaults_terminal_to_pwsh() {
        let plan = plan_windows(&default_item(), true, true);
        assert_eq!("pwsh", plan.args[3]);
    }

    #[test]
    fn marks_dangerous_commands() {
        let plan = plan_windows(&item("claude --dangerously-skip-permissions", r"D:\projects\demo", None), true, true);
        assert!(plan.is_dangerous);
    }

    #[test]
    fn escape_pwsh_quotes_doubles_single_quotes() {
        assert_eq!(r"D:\a''b", escape_pwsh_quotes(r"D:\a'b"));
    }

    #[test]
    fn pwsh_fallback_escapes_directory_with_single_quote() {
        let plan = plan_windows(&item("snow", r"D:\a'b", None), false, true);
        assert_eq!(vec!["-NoExit", "-Command", "cd 'D:\\a''b'; snow"], plan.args);
    }

    #[test]
    fn cmd_fallback_keeps_command_and_uses_working_directory() {
        let plan = plan_windows(&item("snow", "D:\\a\"b", None), false, false);
        assert_eq!("/k", plan.args[0]);
        assert_eq!("snow", plan.args[1]);
        assert_eq!("D:\\a\"b", plan.working_directory);
    }
}
