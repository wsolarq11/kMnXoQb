//! Tauri command thin shell: argument conversion → app-layer orchestration →
//! serialization. No business decisions live here (design decision D1).

pub mod items;
pub mod launch;
pub mod misc;
pub mod settings;
