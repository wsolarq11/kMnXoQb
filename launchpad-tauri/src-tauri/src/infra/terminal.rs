//! Terminal availability probe (where.exe on Windows) with a process-lifetime
//! cache: the probe runs once per terminal name, so repeated Plan() calls do
//! not spawn a child process every launch (ported 1:1 from C# TerminalDetector).

use std::collections::HashMap;
use std::sync::Mutex;

use crate::core::ports::TerminalAvailability;

pub struct TerminalDetector {
    probe: Box<dyn Fn(&str) -> bool + Send + Sync>,
    cache: Mutex<HashMap<String, bool>>,
}

impl TerminalDetector {
    pub fn new() -> Self {
        Self {
            probe: Box::new(probe_where),
            cache: Mutex::new(HashMap::new()),
        }
    }

    /// Probe override for tests.
    pub fn with_probe(probe: impl Fn(&str) -> bool + Send + Sync + 'static) -> Self {
        Self {
            probe: Box::new(probe),
            cache: Mutex::new(HashMap::new()),
        }
    }
}

impl Default for TerminalDetector {
    fn default() -> Self {
        Self::new()
    }
}

impl TerminalAvailability for TerminalDetector {
    fn terminal_available(&self, name: &str) -> bool {
        let mut cache = self.cache.lock().unwrap();
        if let Some(available) = cache.get(name) {
            return *available;
        }
        let available = (self.probe)(name);
        cache.insert(name.to_string(), available);
        available
    }
}

fn probe_where(name: &str) -> bool {
    std::process::Command::new("where")
        .arg(name)
        .stdin(std::process::Stdio::null())
        .stdout(std::process::Stdio::null())
        .stderr(std::process::Stdio::null())
        .status()
        .map(|s| s.success())
        .unwrap_or(false)
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn caches_probe_result_per_name() {
        let calls = std::sync::Arc::new(std::sync::Mutex::new(0));
        let calls_clone = calls.clone();
        let detector = TerminalDetector::with_probe(move |_| {
            *calls_clone.lock().unwrap() += 1;
            true
        });

        assert!(detector.terminal_available("wt.exe"));
        assert!(detector.terminal_available("wt.exe"));
        assert!(detector.terminal_available("wt.exe"));
        assert_eq!(1, *calls.lock().unwrap());
    }

    #[test]
    fn caches_false_results_too() {
        let calls = std::sync::Arc::new(std::sync::Mutex::new(0));
        let calls_clone = calls.clone();
        let detector = TerminalDetector::with_probe(move |_| {
            *calls_clone.lock().unwrap() += 1;
            false
        });

        assert!(!detector.terminal_available("pwsh.exe"));
        assert!(!detector.terminal_available("pwsh.exe"));
        assert_eq!(1, *calls.lock().unwrap());
    }

    #[test]
    fn distinct_names_probe_independently() {
        let calls = std::sync::Arc::new(std::sync::Mutex::new(0));
        let calls_clone = calls.clone();
        let detector = TerminalDetector::with_probe(move |name| {
            *calls_clone.lock().unwrap() += 1;
            name == "wt.exe"
        });

        assert!(detector.terminal_available("wt.exe"));
        assert!(!detector.terminal_available("pwsh.exe"));
        assert_eq!(2, *calls.lock().unwrap());
    }
}
