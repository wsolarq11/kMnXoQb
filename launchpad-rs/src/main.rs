//! WT Launcher — Rust/egui POC entry point.
//!
//! CLI dispatch via clap derive (18 lines vs 75 lines of hand-written argv parsing).
//! GUI via eframe/egui.
//!
//! Usage (subcommands, auto-generated help via `launchpad-rs help`):
//!   launchpad-rs                     Launch GUI
//!   launchpad-rs check <config>      Validate all items (JSON to stdout)
//!   launchpad-rs list <config>       List all items (table)
//!   launchpad-rs launch <config> <id>          Launch item by id
//!   launchpad-rs launch --dry-run <config> <id> Preview launch plan

use std::path::{Path, PathBuf};

use clap::{Parser, Subcommand};
use launchpad_rs::{app, config, launch};

#[derive(Parser)]
#[command(
    name = "launchpad-rs",
    about = "Cross-platform terminal launcher for AI coding agents"
)]
struct Cli {
    #[command(subcommand)]
    command: Option<Command>,
}

#[derive(Subcommand)]
enum Command {
    /// Launch the GUI (default when no subcommand given)
    Gui {
        /// Optional config directory override
        #[arg(short, long)]
        config: Option<PathBuf>,
    },
    /// Validate all items in config (JSON output to stdout)
    Check {
        /// Path to config.json
        config: PathBuf,
    },
    /// List all items in a readable table
    List {
        /// Path to config.json
        config: PathBuf,
    },
    /// Launch a specific item by ID
    Launch {
        /// Path to config.json
        config: PathBuf,
        /// Item ID to launch
        id: String,
        /// Preview the launch plan without executing
        #[arg(long)]
        dry_run: bool,
    },
}

fn main() -> anyhow::Result<()> {
    let cli = Cli::parse();

    match cli.command.unwrap_or(Command::Gui { config: None }) {
        Command::Gui { config } => run_gui(config),
        Command::Check { config } => cmd_check(&config),
        Command::List { config } => cmd_list(&config),
        Command::Launch {
            config,
            id,
            dry_run,
        } => {
            if dry_run {
                cmd_dry_run(&config, &id)
            } else {
                cmd_launch(&config, &id)
            }
        }
    }
}

// ── GUI ──

fn run_gui(config_override: Option<PathBuf>) -> anyhow::Result<()> {
    let config_dir = config_override.unwrap_or_else(|| {
        // Portable-first: config/ next to exe
        std::env::current_exe()
            .ok()
            .and_then(|p| p.parent().map(|d| d.join("config")))
            .filter(|p| p.exists())
            .unwrap_or_else(|| {
                // Fallback to platform-standard config dir
                dirs::config_dir()
                    .unwrap_or_else(|| PathBuf::from("."))
                    .join("launchpad")
                    .join("config")
            })
    });

    // Ensure config directory exists
    std::fs::create_dir_all(&config_dir).ok();

    // Single-instance lock
    let lock_path = config_dir.join(".lock");
    let lock_file = std::fs::OpenOptions::new()
        .create_new(true)
        .write(true)
        .open(&lock_path);

    if lock_file.is_err() {
        // Another instance is running — don't silently exit
        let _ = native_dialog::MessageDialog::new()
            .set_title("WT Launcher")
            .set_text("Another instance is already running.")
            .set_type(native_dialog::MessageType::Info)
            .show_alert();
        return Ok(());
    }

    // Clean up lock file on exit
    let _guard = LockGuard(lock_path.clone());

    let app_state = app::LauncherApp::new(config_dir.clone());

    // Restore window state from settings
    let win_size = app_state.window_size();
    let native_options = eframe::NativeOptions {
        viewport: egui::ViewportBuilder::default()
            .with_inner_size([win_size.0 as f32, win_size.1 as f32])
            .with_min_inner_size([400.0, 300.0]),
        ..Default::default()
    };

    eframe::run_native(
        "WT Launcher",
        native_options,
        Box::new(move |_cc| Ok(Box::new(AppWrapper { state: app_state }))),
    )
    .map_err(|e| anyhow::anyhow!("Failed to run GUI: {e}"))?;

    Ok(())
}

/// Wrapper that delegates to LauncherApp::update and saves on exit.
struct AppWrapper {
    state: app::LauncherApp,
}

impl eframe::App for AppWrapper {
    fn update(&mut self, ctx: &egui::Context, _frame: &mut eframe::Frame) {
        self.state.update(ctx);
    }

    fn on_exit(&mut self, _gl: Option<&eframe::glow::Context>) {
        self.state.save_config();
    }
}

// ── CLI Commands ──

fn cmd_check(config_path: &std::path::Path) -> anyhow::Result<()> {
    let config_dir = config_path
        .parent()
        .map(|p| p.to_path_buf())
        .unwrap_or_else(|| PathBuf::from("."));
    let config_io = config::ConfigIO::new(config_dir);
    let items = config_io.read_items()?;

    println!("{{");
    println!("  \"items\": [");
    for (i, item) in items.iter().enumerate() {
        let valid = !item.command.is_empty();
        let dangerous = launch::is_dangerous(&item.command);
        println!(
            "    {{ \"id\": \"{}\", \"name\": \"{}\", \"valid\": {}, \"dangerous\": {} }}{}",
            item.id,
            item.name,
            valid,
            dangerous,
            if i + 1 < items.len() { "," } else { "" }
        );
    }
    println!("  ]");
    println!("}}");

    let invalid: Vec<_> = items.iter().filter(|i| i.command.is_empty()).collect();
    if !invalid.is_empty() {
        std::process::exit(1);
    }
    Ok(())
}

fn cmd_list(config_path: &Path) -> anyhow::Result<()> {
    let config_dir = config_path
        .parent()
        .map(|p| p.to_path_buf())
        .unwrap_or_else(|| PathBuf::from("."));
    let config_io = config::ConfigIO::new(config_dir);
    let items = config_io.read_items()?;

    if items.is_empty() {
        println!("(no items)");
        return Ok(());
    }

    // Table format: ID | Name | Command
    println!("{:<24}  {:<24}  {:<48}", "ID", "NAME", "COMMAND");
    println!("{}  {}  {}", "─".repeat(24), "─".repeat(24), "─".repeat(48));
    for item in &items {
        let id = truncate(&item.id, 24);
        let name = truncate(&item.name, 24);
        let cmd = truncate(&item.command, 48);
        println!("{id:<24}  {name:<24}  {cmd:<48}");
    }
    Ok(())
}

fn truncate(s: &str, max: usize) -> String {
    if s.len() <= max {
        s.to_string()
    } else {
        format!("{}...", &s[..max.saturating_sub(3)])
    }
}

fn cmd_launch(config_path: &Path, id: &str) -> anyhow::Result<()> {
    let config_dir = config_path
        .parent()
        .map(|p| p.to_path_buf())
        .unwrap_or_else(|| PathBuf::from("."));
    let config_io = config::ConfigIO::new(config_dir);
    let items = config_io.read_items()?;

    let Some(item) = items.iter().find(|i| i.id == id) else {
        anyhow::bail!("Item not found: {id}. Use --list to see available IDs.");
    };

    let _child = launch::launch(item)?;
    println!("Launched: {id}");
    Ok(())
}

fn cmd_dry_run(config_path: &Path, id: &str) -> anyhow::Result<()> {
    let config_dir = config_path
        .parent()
        .map(|p| p.to_path_buf())
        .unwrap_or_else(|| PathBuf::from("."));
    let config_io = config::ConfigIO::new(config_dir);
    let items = config_io.read_items()?;

    let Some(item) = items.iter().find(|i| i.id == id) else {
        anyhow::bail!("Item not found: {id}. Use --list to see available IDs.");
    };

    println!("id:             {id}");
    println!("name:           {}", item.name);
    println!("command:        {}", item.command);
    println!("working_dir:    {}", item.directory);
    println!("is_dangerous:   {}", launch::is_dangerous(&item.command));
    println!(
        "terminal:       {}",
        item.terminal.as_deref().unwrap_or("(default)")
    );

    #[cfg(target_os = "windows")]
    {
        println!("executable:     wt.exe");
        println!(
            "args:           new-tab -d \"{}\" pwsh -NoExit -Command \"{}\"",
            item.directory, item.command
        );
    }
    #[cfg(not(target_os = "windows"))]
    {
        println!("executable:     (platform default terminal)");
        println!("args:           (shell: {})", item.command);
    }

    Ok(())
}

// ── Single-instance lock guard ──

/// RAII guard that removes the lock file on drop.
struct LockGuard(PathBuf);
impl Drop for LockGuard {
    fn drop(&mut self) {
        let _ = std::fs::remove_file(&self.0);
    }
}
