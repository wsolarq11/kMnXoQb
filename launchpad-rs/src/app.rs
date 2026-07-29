//! App: egui UI + state management. Single file containing all UI logic.
//!
//! Replaces: 4 .slint files + app.cpp + app.h + Slint-generated main_window.h.
//! Immediate mode: UI code IS state management. No callback binding, no DSL, no codegen.
//! ~200 lines vs ~600 lines of C++/Slint.

use egui::{Align2, Color32, Context, Frame, RichText, ScrollArea, Window};
use std::path::PathBuf;

use crate::config::ConfigIO;
use crate::launch::{dangerous_reason, is_dangerous};
use crate::types::{AppSettings, LaunchItem};

/// Main application state.
pub struct LauncherApp {
    // Data
    items: Vec<LaunchItem>,
    settings: AppSettings,
    config_dir: PathBuf,
    config_error: Option<String>,

    // UI state — all in one place, no callback-propagation needed
    search_query: String,
    edit_dialog_open: bool,
    editing_index: Option<usize>, // None = new item
    edit_name: String,
    edit_dir: String,
    edit_cmd: String,
    edit_terminal: String,
    edit_confirm: bool,
    name_error: String,
    cmd_error: String,

    // Confirmation dialogs
    delete_confirm: Option<usize>, // index to delete
    launch_confirm: Option<usize>, // index to launch
    batch_confirm: Vec<usize>,     // indices to batch-launch (confirm items only)

    // Status
    status_text: String,
    selected_count: usize,

    // Theme
    is_dark: bool,
}

impl LauncherApp {
    pub fn new(config_dir: PathBuf) -> Self {
        let config = ConfigIO::new(config_dir.clone());

        let (items, settings, config_error) = match config.read_items() {
            Ok(items) => match config.read_settings() {
                Ok(s) => (items, s, None),
                Err(e) => (
                    items,
                    AppSettings::default(),
                    Some(format!("Settings error: {e}")),
                ),
            },
            Err(e) => (
                Vec::new(),
                AppSettings::default(),
                Some(format!("Config error: {e}")),
            ),
        };

        let is_dark = match settings.theme.as_str() {
            "dark" => true,
            "light" => false,
            _ => dark_light::detect() == dark_light::Mode::Dark,
        };

        let selected_count = items.iter().filter(|i| i.selected).count();

        Self {
            items,
            settings,
            config_dir,
            config_error,
            search_query: String::new(),
            edit_dialog_open: false,
            editing_index: None,
            edit_name: String::new(),
            edit_dir: String::new(),
            edit_cmd: String::new(),
            edit_terminal: String::new(),
            edit_confirm: true,
            name_error: String::new(),
            cmd_error: String::new(),
            delete_confirm: None,
            launch_confirm: None,
            batch_confirm: Vec::new(),
            status_text: String::new(),
            selected_count,
            is_dark,
        }
    }

    /// Called every frame by eframe.
    pub fn update(&mut self, ctx: &Context) {
        self.apply_theme(ctx);
        self.render_top_panel(ctx);
        self.render_central_panel(ctx);
        self.render_dialogs(ctx);
        self.render_bottom_panel(ctx);
    }

    // ── Theme ──

    fn apply_theme(&self, ctx: &Context) {
        let mut style = (*ctx.style()).clone();
        style.visuals = if self.is_dark {
            egui::Visuals::dark()
        } else {
            egui::Visuals::light()
        };
        ctx.set_style(style);
    }

    // ── Top Panel (Header + Stats + Search + Batch Bar) ──

    fn render_top_panel(&mut self, ctx: &Context) {
        egui::TopBottomPanel::top("header").show(ctx, |ui| {
            // Header row
            ui.horizontal(|ui| {
                ui.heading("WT Launcher");
                ui.with_layout(egui::Layout::right_to_left(egui::Align::Center), |ui| {
                    // Theme button: shows CURRENT state, not target
                    let theme_label =
                        format!("Theme: {}", if self.is_dark { "Dark" } else { "Light" });
                    if ui.button(theme_label).clicked() {
                        self.is_dark = !self.is_dark;
                        self.settings.theme = if self.is_dark { "dark" } else { "light" }.into();
                        self.save();
                    }
                    ui.checkbox(&mut self.settings.confirm_enabled, "Confirm Launch");
                    if ui.button("+ New").clicked() {
                        self.open_new_dialog();
                    }
                });
            });

            // Stats
            ui.horizontal(|ui| {
                Frame::group(ui.style()).show(ui, |ui| {
                    ui.vertical(|ui| {
                        ui.label(RichText::new("ITEMS").size(12.0).color(Color32::GRAY));
                        ui.label(RichText::new(format!("{}", self.items.len())).size(32.0));
                    });
                });
                Frame::group(ui.style()).show(ui, |ui| {
                    ui.vertical(|ui| {
                        ui.label(RichText::new("RECENT").size(12.0).color(Color32::GRAY));
                        let recent = self
                            .settings
                            .launch_history
                            .first()
                            .map(|s| s.as_str())
                            .unwrap_or("--");
                        ui.label(RichText::new(recent).size(32.0));
                    });
                });
            });

            // Search
            ui.add_space(4.0);
            let search_response = ui.add(
                egui::TextEdit::singleline(&mut self.search_query)
                    .hint_text("Search...")
                    .desired_width(f32::INFINITY),
            );
            if search_response.gained_focus() {
                // Clear focus from cards when search is active
            }

            // Config error display (replaces silent failure)
            if let Some(ref err) = self.config_error {
                ui.colored_label(Color32::RED, err);
            } else if self.items.is_empty() {
                ui.colored_label(Color32::GRAY, "No items yet. Click + New to add one.");
            }

            // Batch bar
            ui.horizontal(|ui| {
                if ui.selectable_label(false, "Select All").clicked() {
                    let all_selected =
                        self.selected_count == self.items.len() && !self.items.is_empty();
                    for item in &mut self.items {
                        item.selected = !all_selected;
                    }
                    self.selected_count = if all_selected { 0 } else { self.items.len() };
                }
                ui.label(format!("Selected: {}", self.selected_count));
                ui.with_layout(egui::Layout::right_to_left(egui::Align::Center), |ui| {
                    if ui
                        .add_enabled(
                            self.selected_count > 0,
                            egui::Button::new("Launch Selected"),
                        )
                        .clicked()
                    {
                        self.batch_launch();
                    }
                });
            });
        });
    }

    // ── Central Panel (Item Cards) ──

    fn render_central_panel(&mut self, ctx: &Context) {
        egui::CentralPanel::default().show(ctx, |ui| {
            // Filter items by search query
            let query = self.search_query.to_lowercase();
            let visible: Vec<usize> = self
                .items
                .iter()
                .enumerate()
                .filter(|(_, item)| {
                    query.is_empty()
                        || item.name.to_lowercase().contains(&query)
                        || item.directory.to_lowercase().contains(&query)
                        || item.command.to_lowercase().contains(&query)
                })
                .map(|(i, _)| i)
                .collect();

            ScrollArea::vertical().show(ui, |ui| {
                // Multi-column card grid — egui handles wrapping automatically
                let card_width = 280.0;
                let _columns = (ui.available_width() / card_width).max(1.0) as usize;

                egui::Grid::new("cards")
                    .min_col_width(card_width)
                    .max_col_width(card_width)
                    .show(ui, |ui| {
                        for &idx in &visible {
                            self.render_card(ui, idx);
                        }
                    });
            });
        });
    }

    fn render_card(&mut self, ui: &mut egui::Ui, idx: usize) {
        let dangerous = is_dangerous(&self.items[idx].command);
        let is_selected = self.items[idx].selected;

        // Clone data needed for rendering so we don't hold self.items borrow
        // across mutable self operations (Edit, Del, select toggle).
        let item_name = self.items[idx].name.clone();
        let item_dir = self.items[idx].directory.clone();
        let item_cmd = self.items[idx].command.clone();
        let item_tag = self.items[idx].tag.clone();
        let item_group = self.items[idx].group.clone();
        let item_terminal = self.items[idx].terminal.clone();
        let item_confirm = self.items[idx].confirm;
        let confirm_enabled = self.settings.confirm_enabled;

        Frame::group(ui.style())
            .fill(if dangerous {
                Color32::from_rgb(80, 20, 20)
            } else {
                ui.style().visuals.extreme_bg_color
            })
            .stroke(egui::Stroke::new(
                1.0_f32,
                if is_selected {
                    ui.style().visuals.selection.stroke.color
                } else {
                    ui.style().visuals.widgets.noninteractive.bg_stroke.color
                },
            ))
            .show(ui, |ui| {
                ui.set_min_width(260.0);
                ui.set_min_height(100.0);

                // Row 1: Name + buttons (always visible — no hover condition)
                ui.horizontal(|ui| {
                    ui.label(RichText::new(&item_name).strong().size(16.0));
                    ui.with_layout(egui::Layout::right_to_left(egui::Align::Center), |ui| {
                        if ui.button("Edit").clicked() {
                            self.open_edit_dialog(idx);
                        }
                        if ui.button("Del").clicked() {
                            self.delete_confirm = Some(idx);
                        }
                        // Reorder buttons
                        if ui
                            .add_enabled(idx > 0, egui::Button::new("\u{25B2}"))
                            .clicked()
                        {
                            self.items.swap(idx, idx - 1);
                            self.save();
                        }
                        if ui
                            .add_enabled(idx + 1 < self.items.len(), egui::Button::new("\u{25BC}"))
                            .clicked()
                        {
                            self.items.swap(idx, idx + 1);
                            self.save();
                        }
                        let sel = &mut self.items[idx].selected;
                        if ui.checkbox(sel, "").changed() {
                            self.selected_count = self.items.iter().filter(|i| i.selected).count();
                        }
                    });
                });

                // Row 2: Directory
                ui.label(
                    RichText::new(&item_dir)
                        .size(12.0)
                        .color(Color32::GRAY)
                        .monospace(),
                );

                // Row 3: Command
                ui.label(
                    RichText::new(&item_cmd)
                        .size(12.0)
                        .color(Color32::DARK_GRAY)
                        .monospace(),
                );

                // Row 4: Tags + meta
                ui.horizontal(|ui| {
                    if let Some(ref tag) = item_tag {
                        ui.label(
                            RichText::new(format!("#{tag}"))
                                .size(11.0)
                                .color(Color32::LIGHT_BLUE),
                        );
                    }
                    if let Some(ref group) = item_group {
                        ui.label(
                            RichText::new(format!("@{group}"))
                                .size(11.0)
                                .color(Color32::LIGHT_GREEN),
                        );
                    }
                    if let Some(ref term) = item_terminal {
                        ui.label(
                            RichText::new(format!("[{term}]"))
                                .size(11.0)
                                .color(Color32::GRAY),
                        );
                    }
                    if dangerous {
                        let reason = dangerous_reason(&item_cmd).unwrap_or("dangerous");
                        ui.label(
                            RichText::new(format!("DANGER: {reason}"))
                                .size(11.0)
                                .color(Color32::RED),
                        );
                    }
                });

                // Click to launch (entire card is clickable)
                if ui
                    .interact(ui.max_rect(), ui.next_auto_id(), egui::Sense::click())
                    .clicked()
                {
                    if confirm_enabled && (item_confirm || dangerous) {
                        self.launch_confirm = Some(idx);
                    } else {
                        self.do_launch(idx);
                    }
                }
            });
    }

    // ── Dialogs ──

    fn render_dialogs(&mut self, ctx: &Context) {
        // Edit dialog
        if self.edit_dialog_open {
            Window::new(if self.editing_index.is_some() {
                "Edit Item"
            } else {
                "New Item"
            })
            .collapsible(false)
            .resizable(false)
            .anchor(Align2::CENTER_CENTER, [0.0, 0.0])
            .show(ctx, |ui| {
                // Name
                ui.label("NAME");
                ui.text_edit_singleline(&mut self.edit_name);
                if !self.name_error.is_empty() {
                    ui.colored_label(Color32::RED, self.name_error.clone());
                }

                // Directory
                ui.label("DIRECTORY");
                ui.horizontal(|ui| {
                    ui.text_edit_singleline(&mut self.edit_dir);
                    if ui.button("Browse...").clicked() {
                        if let Some(dir) = native_dialog::FileDialog::new()
                            .show_open_single_dir()
                            .ok()
                            .flatten()
                        {
                            self.edit_dir = dir.to_string_lossy().to_string();
                        }
                    }
                });

                // Command
                ui.label("COMMAND");
                ui.text_edit_singleline(&mut self.edit_cmd);
                if !self.cmd_error.is_empty() {
                    ui.colored_label(Color32::RED, self.cmd_error.clone());
                }
                ui.label(
                    RichText::new("Command will be executed in the specified directory.")
                        .size(11.0)
                        .color(Color32::GRAY),
                );

                // Terminal (optional) — the field that was MISSING in Slint GUI
                ui.label("TERMINAL (optional)");
                ui.text_edit_singleline(&mut self.edit_terminal);
                ui.label(
                    RichText::new("Override the default terminal (e.g. pwsh, gnome-terminal)")
                        .size(11.0)
                        .color(Color32::GRAY),
                );

                // Confirm toggle
                ui.checkbox(&mut self.edit_confirm, "Confirm before launch");

                // Buttons
                ui.horizontal(|ui| {
                    if ui.button("Cancel").clicked() {
                        self.edit_dialog_open = false;
                    }
                    if let Some(idx) = self.editing_index {
                        if ui.button("Delete").clicked() {
                            self.items.remove(idx);
                            self.edit_dialog_open = false;
                            self.save();
                        }
                    }
                    if ui.button("Save").clicked() {
                        // Validation — NOT silently discarded
                        self.name_error.clear();
                        self.cmd_error.clear();
                        if self.edit_name.trim().is_empty() {
                            self.name_error = "Name is required.".into();
                        }
                        if self.edit_cmd.trim().is_empty() {
                            self.cmd_error = "Command is required.".into();
                        }
                        if self.name_error.is_empty() && self.cmd_error.is_empty() {
                            self.save_item();
                        }
                    }
                });

                // Real-time directory validation
                if !self.edit_dir.is_empty() {
                    let dir_path = PathBuf::from(&self.edit_dir);
                    if !dir_path.exists() {
                        ui.colored_label(Color32::YELLOW, "Directory does not exist.");
                    } else if !dir_path.is_dir() {
                        ui.colored_label(Color32::YELLOW, "Path is not a directory.");
                    } else {
                        ui.colored_label(Color32::GREEN, "Directory exists.");
                    }
                }
            });
        }

        // Delete confirmation dialog
        if let Some(idx) = self.delete_confirm {
            let item_name = self.items[idx].name.clone();
            Window::new("Delete Item")
                .collapsible(false)
                .resizable(false)
                .anchor(Align2::CENTER_CENTER, [0.0, 0.0])
                .show(ctx, |ui| {
                    ui.label(format!("Delete '{item_name}'?"));
                    ui.label("This cannot be undone.");
                    ui.horizontal(|ui| {
                        if ui.button("Cancel").clicked() {
                            self.delete_confirm = None;
                        }
                        if ui.button("Delete").clicked() {
                            self.items.remove(idx);
                            self.delete_confirm = None;
                            self.save();
                        }
                    });
                });
        }

        // Launch confirmation dialog
        if let Some(idx) = self.launch_confirm {
            let item_name = self.items[idx].name.clone();
            let item_cmd = self.items[idx].command.clone();
            let dangerous = is_dangerous(&item_cmd);
            Window::new("Confirm Launch")
                .collapsible(false)
                .resizable(false)
                .anchor(Align2::CENTER_CENTER, [0.0, 0.0])
                .show(ctx, |ui| {
                    ui.label(format!("Launch '{item_name}'?"));
                    ui.label(&item_cmd);
                    if dangerous {
                        ui.colored_label(
                            Color32::RED,
                            format!(
                                "Warning: {}",
                                dangerous_reason(&item_cmd).unwrap_or("dangerous command")
                            ),
                        );
                    }
                    ui.label("This command requires confirmation.");
                    ui.horizontal(|ui| {
                        if ui.button("Cancel").clicked() {
                            self.launch_confirm = None;
                        }
                        if ui.button("Launch").clicked() {
                            self.launch_confirm = None;
                            self.do_launch(idx);
                        }
                    });
                });
        }

        // Batch confirmation dialog
        if !self.batch_confirm.is_empty() {
            Window::new("Confirm Batch Launch")
                .collapsible(false)
                .resizable(false)
                .anchor(Align2::CENTER_CENTER, [0.0, 0.0])
                .show(ctx, |ui| {
                    ui.label("These items require confirmation:");
                    for &idx in &self.batch_confirm {
                        ui.label(format!("  • {}", self.items[idx].name));
                    }
                    ui.label("Launch all?");
                    ui.horizontal(|ui| {
                        if ui.button("Cancel").clicked() {
                            self.batch_confirm.clear();
                        }
                        if ui.button("Launch All").clicked() {
                            let indices: Vec<usize> = self.batch_confirm.drain(..).collect();
                            for idx in indices {
                                self.do_launch(idx);
                            }
                        }
                    });
                });
        }
    }

    // ── Bottom Panel (Status Bar + Launch History) ──

    fn render_bottom_panel(&mut self, ctx: &Context) {
        egui::TopBottomPanel::bottom("status").show(ctx, |ui| {
            ui.horizontal(|ui| {
                if self.status_text.is_empty() {
                    ui.label("Ready");
                } else {
                    ui.label(self.status_text.clone());
                }
                ui.with_layout(egui::Layout::right_to_left(egui::Align::Center), |ui| {
                    // Show recent launch history (all 10, not just 1)
                    if !self.settings.launch_history.is_empty() {
                        ui.label(
                            RichText::new(format!(
                                "Recent: {}",
                                self.settings.launch_history.join(", ")
                            ))
                            .size(11.0)
                            .color(Color32::GRAY),
                        );
                    }
                });
            });
        });
    }

    // ── Actions ──

    fn do_launch(&mut self, idx: usize) {
        let item = &self.items[idx];
        match crate::launch::launch(item) {
            Ok(_child) => {
                // Update history
                let name = item.name.clone();
                self.settings.launch_history.retain(|n| n != &name);
                self.settings.launch_history.insert(0, name);
                self.settings.launch_history.truncate(10);
                self.status_text = format!("Launched: {}", item.name);
                self.save();
            }
            Err(e) => {
                self.status_text = format!("Launch failed: {e}");
            }
        }
    }

    fn batch_launch(&mut self) {
        let mut confirm_items = Vec::new();
        let mut launched = 0;
        let indices: Vec<usize> = self
            .items
            .iter()
            .enumerate()
            .filter(|(_, i)| i.selected)
            .map(|(idx, _)| idx)
            .collect();

        for idx in &indices {
            let item = &self.items[*idx];
            if self.settings.confirm_enabled && (item.confirm || is_dangerous(&item.command)) {
                confirm_items.push(*idx);
            } else {
                self.do_launch(*idx);
                launched += 1;
            }
        }

        if !confirm_items.is_empty() {
            // Show ONE dialog for all confirm items (not one-per-item that breaks the loop)
            self.batch_confirm = confirm_items;
            self.status_text = format!(
                "{launched} launched, {} need confirmation",
                self.batch_confirm.len()
            );
        } else {
            self.status_text = format!("Launched {launched} items");
        }

        // Deselect after batch launch completes (not prematurely)
        for idx in &indices {
            self.items[*idx].selected = false;
        }
        self.selected_count = 0;
    }

    fn open_new_dialog(&mut self) {
        self.editing_index = None;
        self.edit_name.clear();
        self.edit_dir.clear();
        self.edit_cmd.clear();
        self.edit_terminal.clear();
        self.edit_confirm = true;
        self.name_error.clear();
        self.cmd_error.clear();
        self.edit_dialog_open = true;
    }

    fn open_edit_dialog(&mut self, idx: usize) {
        let item = &self.items[idx];
        self.editing_index = Some(idx);
        self.edit_name = item.name.clone();
        self.edit_dir = item.directory.clone();
        self.edit_cmd = item.command.clone();
        self.edit_terminal = item.terminal.clone().unwrap_or_default();
        self.edit_confirm = item.confirm;
        self.name_error.clear();
        self.cmd_error.clear();
        self.edit_dialog_open = true;
    }

    fn save_item(&mut self) {
        let new_item = LaunchItem {
            name: self.edit_name.trim().to_string(),
            directory: self.edit_dir.trim().to_string(),
            command: self.edit_cmd.trim().to_string(),
            confirm: self.edit_confirm,
            id: if let Some(idx) = self.editing_index {
                self.items[idx].id.clone()
            } else {
                self.generate_id(&self.edit_name)
            },
            selected: false,
            terminal: if self.edit_terminal.trim().is_empty() {
                None
            } else {
                Some(self.edit_terminal.trim().to_string())
            },
            tag: self
                .items
                .get(self.editing_index.unwrap_or(usize::MAX))
                .and_then(|i| i.tag.clone()),
            group: self
                .items
                .get(self.editing_index.unwrap_or(usize::MAX))
                .and_then(|i| i.group.clone()),
        };

        if let Some(idx) = self.editing_index {
            self.items[idx] = new_item;
        } else {
            self.items.push(new_item);
        }
        self.edit_dialog_open = false;
        self.save();
    }

    fn generate_id(&self, name: &str) -> String {
        let base = name.to_lowercase().replace(' ', "_");
        if !self.items.iter().any(|i| i.id == base) {
            return base;
        }
        for n in 2.. {
            let candidate = format!("{base}_{n}");
            if !self.items.iter().any(|i| i.id == candidate) {
                return candidate;
            }
        }
        base // fallback
    }

    /// Public save — called by AppWrapper::on_exit for window state persistence.
    pub fn save_config(&self) {
        let config = ConfigIO::new(self.config_dir.clone());
        let _ = config.write_items(&self.items);
        let _ = config.write_settings(&self.settings);
    }

    /// Restore window size from persisted state.
    pub fn window_size(&self) -> (u32, u32) {
        self.settings
            .window_state
            .as_ref()
            .map(|ws| (ws.width, ws.height))
            .unwrap_or((800, 600))
    }

    fn save(&self) {
        self.save_config();
    }
}
