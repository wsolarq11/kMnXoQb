//! Pure functional core: zero external I/O (no std::fs / std::process / Tauri).
//! Behavior mirrors the C# launchpad.Core + launchpad.UseCases pure layer 1:1;
//! the test assertions are the contract (ported from launchpad.Core.Tests).

pub mod danger;
pub mod errors;
pub mod i18n;
pub mod items;
pub mod launch;
pub mod models;
pub mod planner;
pub mod ports;
pub mod settings;
pub mod validator;
pub mod window_pos;
