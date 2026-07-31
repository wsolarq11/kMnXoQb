//! Launch: zero-shell process spawning via std::process::Command.
//!
//! Clear separation: `plan()` decides (pure), `launch()` executes (effectful).

use std::process::{Child, Command};

#[cfg(target_os = "windows")]
use std::os::windows::process::CommandExt;

use crate::types::{LaunchItem, LaunchPlan};

// ── Dangerous flag detection ──

/// Single source of truth for dangerous flag detection.
const DANGEROUS_FLAGS: &[(&str, &str)] = &[
    ("dangerously", "contains --dangerously flag"),
    ("yolo", "contains --yolo flag"),
    ("skip-permissions", "contains --skip-permissions flag"),
    ("bypass-approvals", "contains --bypass-approvals flag"),
    ("bypass-sandbox", "contains --bypass-sandbox flag"),
    ("bypass.sandbox", "contains --bypass-sandbox flag"),
];

/// Check if a command contains dangerous flags.
pub fn is_dangerous(command: &str) -> bool {
    let lower = command.to_lowercase();
    DANGEROUS_FLAGS.iter().any(|(flag, _)| lower.contains(flag))
}

/// Reason why a command was flagged dangerous (for tooltip).
pub fn dangerous_reason(command: &str) -> Option<&'static str> {
    let lower = command.to_lowercase();
    DANGEROUS_FLAGS
        .iter()
        .find(|(flag, _)| lower.contains(flag))
        .map(|(_, reason)| *reason)
}

// ── Terminal detection ──

/// Check if an executable is on PATH.
fn terminal_available(name: &str) -> bool {
    which_cmd()
        .arg(name)
        .output()
        .map(|o| o.status.success())
        .unwrap_or(false)
}

#[cfg(windows)]
fn which_cmd() -> Command {
    Command::new("where")
}

#[cfg(not(windows))]
fn which_cmd() -> Command {
    Command::new("which")
}

// ── Plan: pure decision, no side effects ──

/// Build a launch plan without executing. Platform dispatch mirrors `launch()`.
pub fn plan(item: &LaunchItem) -> LaunchPlan {
    #[cfg(target_os = "windows")]
    {
        plan_windows(item)
    }
    #[cfg(target_os = "macos")]
    {
        plan_macos(item)
    }
    #[cfg(not(any(target_os = "windows", target_os = "macos")))]
    {
        plan_linux(item)
    }
}

#[cfg(target_os = "windows")]
fn plan_windows(item: &LaunchItem) -> LaunchPlan {
    let dir = &item.directory;
    let terminal = item.terminal.as_deref().unwrap_or("pwsh");
    let dangerous = is_dangerous(&item.command);

    if terminal_available("wt.exe") {
        LaunchPlan {
            executable: "wt.exe".into(),
            args: vec![
                "new-tab".into(),
                "-d".into(),
                dir.clone(),
                terminal.into(),
                "-NoExit".into(),
                "-Command".into(),
                item.command.clone(),
            ],
            working_dir: dir.clone(),
            is_dangerous: dangerous,
            terminal_override: item.terminal.clone(),
        }
    } else if terminal_available("pwsh.exe") {
        LaunchPlan {
            executable: "pwsh.exe".into(),
            args: vec![
                "-NoExit".into(),
                "-Command".into(),
                format!("cd '{}'; {}", dir, item.command),
            ],
            working_dir: dir.clone(),
            is_dangerous: dangerous,
            terminal_override: item.terminal.clone(),
        }
    } else {
        LaunchPlan {
            executable: "cmd.exe".into(),
            args: vec!["/k".into(), format!("cd /d \"{}\" && {}", dir, item.command)],
            working_dir: dir.clone(),
            is_dangerous: dangerous,
            terminal_override: item.terminal.clone(),
        }
    }
}

#[cfg(target_os = "macos")]
fn plan_macos(item: &LaunchItem) -> LaunchPlan {
    let script = format!(
        "tell app \"Terminal\" to do script \"cd '{}'; {}\"",
        item.directory.replace('\'', "'\\''"),
        item.command.replace('"', "\\\"")
    );
    LaunchPlan {
        executable: "/usr/bin/osascript".into(),
        args: vec!["-e".into(), script],
        working_dir: item.directory.clone(),
        is_dangerous: is_dangerous(&item.command),
        terminal_override: item.terminal.clone(),
    }
}

#[cfg(not(any(target_os = "windows", target_os = "macos")))]
fn plan_linux(item: &LaunchItem) -> LaunchPlan {
    for term in &["gnome-terminal", "konsole", "xfce4-terminal", "xterm"] {
        if terminal_available(term) {
            let cmd = format!("cd '{}' && {}; exec bash", item.directory, item.command);
            return LaunchPlan {
                executable: term.to_string(),
                args: vec!["-e".into(), "bash".into(), "-c".into(), cmd],
                working_dir: item.directory.clone(),
                is_dangerous: is_dangerous(&item.command),
                terminal_override: item.terminal.clone(),
            };
        }
    }
    LaunchPlan {
        executable: "(none)".into(),
        args: vec![],
        working_dir: item.directory.clone(),
        is_dangerous: is_dangerous(&item.command),
        terminal_override: item.terminal.clone(),
    }
}

// ── Launch: execute the plan ──

/// Spawn a command in a new terminal window. Zero-shell: argv goes directly to exec.
pub fn launch(item: &LaunchItem) -> anyhow::Result<Child> {
    #[cfg(target_os = "windows")]
    {
        launch_windows(item)
    }
    #[cfg(target_os = "macos")]
    {
        launch_macos(item)
    }
    #[cfg(not(any(target_os = "windows", target_os = "macos")))]
    {
        launch_linux(item)
    }
}

#[cfg(target_os = "windows")]
fn launch_windows(item: &LaunchItem) -> anyhow::Result<Child> {
    let p = plan_windows(item);
    if p.executable == "pwsh.exe" || p.executable == "cmd.exe" {
        Ok(Command::new(&p.executable)
            .args(&p.args)
            .creation_flags(0x0000_0010) // CREATE_NEW_CONSOLE
            .spawn()?)
    } else {
        Ok(Command::new(&p.executable).args(&p.args).spawn()?)
    }
}

#[cfg(target_os = "macos")]
fn launch_macos(item: &LaunchItem) -> anyhow::Result<Child> {
    let p = plan_macos(item);
    Ok(Command::new(&p.executable).args(&p.args).spawn()?)
}

#[cfg(not(any(target_os = "windows", target_os = "macos")))]
fn launch_linux(item: &LaunchItem) -> anyhow::Result<Child> {
    let p = plan_linux(item);
    if p.executable == "(none)" {
        anyhow::bail!(
            "No supported terminal found. Install gnome-terminal, konsole, xfce4-terminal, or xterm."
        )
    }
    Ok(Command::new(&p.executable).args(&p.args).spawn()?)
}
