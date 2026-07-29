//! Launch: zero-shell process spawning via std::process::Command.

use std::process::{Child, Command};

#[cfg(target_os = "windows")]
use std::os::windows::process::CommandExt;

use crate::types::LaunchItem;

/// Check if a command contains dangerous flags (same rules as C++ is_dangerous).
pub fn is_dangerous(command: &str) -> bool {
    let lower = command.to_lowercase();
    lower.contains("dangerously")
        || lower.contains("yolo")
        || lower.contains("skip-permissions")
        || lower.contains("bypass-approvals")
        || lower.contains("bypass-sandbox")
        || lower.contains("bypass.sandbox")
}

/// Reason why a command was flagged dangerous (for tooltip).
pub fn dangerous_reason(command: &str) -> Option<&'static str> {
    let lower = command.to_lowercase();
    if lower.contains("dangerously") {
        Some("contains --dangerously flag")
    } else if lower.contains("yolo") {
        Some("contains --yolo flag")
    } else if lower.contains("skip-permissions") {
        Some("contains --skip-permissions flag")
    } else if lower.contains("bypass-approvals") {
        Some("contains --bypass-approvals flag")
    } else if lower.contains("bypass-sandbox") {
        Some("contains --bypass-sandbox flag")
    } else {
        None
    }
}

/// Check if a command is available on PATH.
#[allow(dead_code)]
fn terminal_available(name: &str) -> bool {
    Command::new("where")
        .arg(name)
        .output()
        .map(|o| o.status.success())
        .unwrap_or(false)
}

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
    let dir = &item.directory;
    let terminal = item.terminal.as_deref().unwrap_or("pwsh");

    if terminal_available("wt.exe") {
        Ok(Command::new("wt.exe")
            .args([
                "new-tab",
                "-d",
                dir,
                terminal,
                "-NoExit",
                "-Command",
                &item.command,
            ])
            .spawn()?)
    } else if terminal_available("pwsh.exe") {
        Ok(Command::new("pwsh.exe")
            .args([
                "-NoExit",
                "-Command",
                &format!("cd '{}'; {}", dir, item.command),
            ])
            .creation_flags(0x0000_0010) // CREATE_NEW_CONSOLE
            .spawn()?)
    } else {
        Ok(Command::new("cmd.exe")
            .args(["/k", &format!("cd /d \"{}\" && {}", dir, item.command)])
            .creation_flags(0x0000_0010)
            .spawn()?)
    }
}

#[cfg(target_os = "macos")]
fn launch_macos(item: &LaunchItem) -> anyhow::Result<Child> {
    let script = format!(
        "tell app \"Terminal\" to do script \"cd '{}'; {}\"",
        item.directory.replace('\'', "'\\''"),
        item.command.replace('"', "\\\"")
    );
    Ok(Command::new("/usr/bin/osascript")
        .args(["-e", &script])
        .spawn()?)
}

#[cfg(not(any(target_os = "windows", target_os = "macos")))]
fn launch_linux(item: &LaunchItem) -> anyhow::Result<Child> {
    for term in &["gnome-terminal", "konsole", "xfce4-terminal", "xterm"] {
        if terminal_available(term) {
            let cmd = format!("cd '{}' && {}; exec bash", item.directory, item.command);
            return Ok(Command::new(term)
                .args(["-e", "bash", "-c", &cmd])
                .spawn()?);
        }
    }
    anyhow::bail!(
        "No supported terminal found. Install gnome-terminal, konsole, xfce4-terminal, or xterm."
    )
}
