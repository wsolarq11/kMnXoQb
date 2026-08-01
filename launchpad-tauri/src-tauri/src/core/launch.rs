//! Launch orchestration (ported 1:1 from C# LaunchUseCase): confirmation
//! policy, plan construction via the pure planner, batch launch with per-item
//! error capture, and history mutations.

use crate::core::errors::{classify_spawn_error, AppError};
use crate::core::models::{AppSettings, LaunchItem, LaunchPlan};
use crate::core::planner;
use crate::core::ports::{ProcessSpawner, TerminalAvailability};

pub fn needs_confirm(settings: &AppSettings, item: &LaunchItem) -> bool {
    settings.confirm_enabled && (item.confirm || item.is_dangerous())
}

/// Items that must be confirmed before launching (confirm on, or dangerous).
pub fn require_confirm<'a>(
    settings: &AppSettings,
    items: &'a [LaunchItem],
) -> Vec<&'a LaunchItem> {
    items.iter().filter(|i| needs_confirm(settings, i)).collect()
}

/// Prepend to launch history, capped at max entries. Duplicates of the name
/// are removed first (matches the legacy Rust behavior).
pub fn push_history(history: &[String], name: &str, max: usize) -> Vec<String> {
    let mut deduped: Vec<String> = history.iter().filter(|h| *h != name).cloned().collect();
    deduped.insert(0, name.to_string());
    deduped.truncate(max);
    deduped
}

/// Launch orchestration service. The spawner/detector are injected ports;
/// `dir_exists` is injected per call so the core performs no filesystem I/O.
pub struct LaunchService<S: ProcessSpawner, D: TerminalAvailability> {
    pub spawner: S,
    pub detector: D,
}

impl<S: ProcessSpawner, D: TerminalAvailability> LaunchService<S, D> {
    pub fn new(spawner: S, detector: D) -> Self {
        Self { spawner, detector }
    }

    pub fn plan(&self, item: &LaunchItem) -> LaunchPlan {
        let wt = self.detector.terminal_available("wt.exe");
        let pwsh = self.detector.terminal_available("pwsh.exe");
        planner::plan_windows(item, wt, pwsh)
    }

    pub fn launch(&self, item: &LaunchItem) -> Result<(), AppError> {
        let plan = self.plan(item);
        self.spawner
            .launch(&plan)
            .map_err(|e| classify_spawn_error(&e, &plan.executable, &plan.working_directory, true))
    }

    pub fn try_launch(
        &self,
        item: &LaunchItem,
        dir_exists: impl Fn(&str) -> bool,
    ) -> Result<(), AppError> {
        let plan = self.plan(item);
        self.spawner.launch(&plan).map_err(|e| {
            classify_spawn_error(&e, &plan.executable, &plan.working_directory, dir_exists(&plan.working_directory))
        })
    }

    /// Batch launch with per-item error capture: success count + failed indexes.
    pub fn launch_many(&self, items: &[LaunchItem]) -> (usize, Vec<usize>) {
        let mut succeeded = 0;
        let mut failed = Vec::new();
        for (i, item) in items.iter().enumerate() {
            if self.try_launch(item, |_| true).is_err() {
                failed.push(i);
            } else {
                succeeded += 1;
            }
        }
        (succeeded, failed)
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::core::ports::{SpawnError, ERROR_PATH_NOT_FOUND};

    fn item(name: &str, command: &str, confirm: bool) -> LaunchItem {
        LaunchItem {
            name: name.to_string(),
            directory: r"D:\projects\demo".to_string(),
            command: command.to_string(),
            confirm,
            id: name.replace(' ', "_"),
            selected: false,
            terminal: None,
            tag: None,
            group: None,
        }
    }

    #[derive(Default)]
    struct FakeSpawner {
        plans: std::sync::Mutex<Vec<LaunchPlan>>,
    }

    impl ProcessSpawner for FakeSpawner {
        fn launch(&self, plan: &LaunchPlan) -> Result<(), SpawnError> {
            self.plans.lock().unwrap().push(plan.clone());
            Ok(())
        }
    }

    struct FakeDetector(Vec<String>);

    impl TerminalAvailability for FakeDetector {
        fn terminal_available(&self, name: &str) -> bool {
            self.0.iter().any(|n| n.eq_ignore_ascii_case(name))
        }
    }

    fn use_case(spawner: impl ProcessSpawner + 'static, available: &[&str]) -> LaunchService<impl ProcessSpawner, FakeDetector> {
        let available: Vec<String> = available.iter().map(|s| s.to_string()).collect();
        LaunchService::new(spawner, FakeDetector(available))
    }

    #[test]
    fn needs_confirm_true_when_global_on_and_item_or_dangerous() {
        let settings = AppSettings {
            confirm_enabled: true,
            ..Default::default()
        };
        assert!(needs_confirm(&settings, &item("a", "snow", true)));
        assert!(needs_confirm(&settings, &item("b", "claude --yolo", false)));
    }

    #[test]
    fn needs_confirm_false_when_global_off() {
        let settings = AppSettings::default();
        assert!(!needs_confirm(&settings, &item("a", "snow", true)));
        assert!(!needs_confirm(&settings, &item("b", "claude --yolo", false)));
    }

    #[test]
    fn plan_detects_available_terminals() {
        let uc = use_case(FakeSpawner::default(), &["wt.exe"]);
        assert_eq!("wt.exe", uc.plan(&item("a", "snow", false)).executable);
    }

    #[test]
    fn plan_falls_back_to_cmd_when_nothing_detected() {
        let uc = use_case(FakeSpawner::default(), &[]);
        assert_eq!("cmd.exe", uc.plan(&item("a", "snow", false)).executable);
    }

    #[test]
    fn launch_spawns_exact_argv() {
        let spawner = FakeSpawner::default();
        let available = vec!["wt.exe".to_string()];
        let uc = LaunchService::new(spawner, FakeDetector(available));
        uc.launch(&item("a", "snow --flag", false)).unwrap();
        let plan = uc.spawner.plans.lock().unwrap().pop().unwrap();
        assert_eq!("wt.exe", plan.executable);
        assert!(plan.args.iter().any(|a| a == "snow --flag"));
        assert_eq!(r"D:\projects\demo", plan.working_directory);
    }

    struct Win32ThrowingSpawner(u32);

    impl ProcessSpawner for Win32ThrowingSpawner {
        fn launch(&self, _plan: &LaunchPlan) -> Result<(), SpawnError> {
            Err(SpawnError::Win32 { code: self.0 })
        }
    }

    struct OtherThrowingSpawner;

    impl ProcessSpawner for OtherThrowingSpawner {
        fn launch(&self, _plan: &LaunchPlan) -> Result<(), SpawnError> {
            Err(SpawnError::Other("invalid directory".to_string()))
        }
    }

    #[test]
    fn try_launch_returns_success_on_ok() {
        let uc = use_case(FakeSpawner::default(), &["wt.exe"]);
        assert!(uc.try_launch(&item("a", "snow", false), |_| true).is_ok());
    }

    #[test]
    fn try_launch_returns_structured_error_on_spawn_failure() {
        let uc = LaunchService::new(OtherThrowingSpawner, FakeDetector(vec!["wt.exe".to_string()]));
        let err = uc.try_launch(&item("a", "snow", false), |_| true).unwrap_err();
        assert!(err.description().contains("invalid directory"));
        assert_eq!("Launch.Unknown", err.kind());
    }

    #[test]
    fn try_launch_path_not_found_with_existing_working_dir_reports_executable() {
        let uc = LaunchService::new(Win32ThrowingSpawner(ERROR_PATH_NOT_FOUND), FakeDetector(vec!["wt.exe".to_string()]));
        let err = uc
            .try_launch(&item("a", "snow", false), |_| true)
            .unwrap_err();
        assert_eq!("Launch.ProcessNotFound", err.kind());
    }

    #[test]
    fn try_launch_path_not_found_with_missing_working_dir_reports_working_directory() {
        let uc = LaunchService::new(Win32ThrowingSpawner(ERROR_PATH_NOT_FOUND), FakeDetector(vec!["wt.exe".to_string()]));
        let err = uc
            .try_launch(&item("a", "snow", false), |_| false)
            .unwrap_err();
        assert_eq!("Launch.WorkingDirectoryMissing", err.kind());
    }

    #[test]
    fn push_history_prepends_and_caps_at_max() {
        let history = vec!["a".to_string(), "b".to_string(), "c".to_string()];
        assert_eq!(vec!["new", "a", "b"], push_history(&history, "new", 3));
    }

    #[test]
    fn push_history_removes_duplicate_before_prepending() {
        let history = vec!["a".to_string(), "b".to_string(), "a".to_string()];
        assert_eq!(vec!["a", "b"], push_history(&history, "a", 10));
    }

    #[test]
    fn push_history_no_duplicate_when_absent() {
        let history = vec!["x".to_string(), "y".to_string()];
        assert_eq!(vec!["z", "x", "y"], push_history(&history, "z", 10));
    }

    #[test]
    fn require_confirm_returns_only_items_needing_confirmation() {
        let settings = AppSettings {
            confirm_enabled: true,
            ..Default::default()
        };
        let items = [
            item("a", "snow", false),
            item("b", "claude --yolo", false),
            item("c", "snow", true),
        ];
        let r = require_confirm(&settings, &items);
        assert_eq!(2, r.len());
        assert_eq!("b", r[0].name);
        assert_eq!("c", r[1].name);
    }

    #[test]
    fn require_confirm_empty_when_global_off() {
        let items = [item("a", "claude --yolo", false)];
        assert!(require_confirm(&AppSettings::default(), &items).is_empty());
    }

    #[test]
    fn launch_many_all_succeed() {
        let uc = use_case(FakeSpawner::default(), &["wt.exe"]);
        let items = [item("a", "snow a", false), item("b", "snow b", false)];
        let (succeeded, failed) = uc.launch_many(&items);
        assert_eq!(2, succeeded);
        assert!(failed.is_empty());
    }

    #[test]
    fn launch_many_collects_failed_indexes() {
        struct PartialSpawner;
        impl ProcessSpawner for PartialSpawner {
            fn launch(&self, plan: &LaunchPlan) -> Result<(), SpawnError> {
                if plan.args.iter().any(|a| a == "fail") {
                    Err(SpawnError::Other("boom".to_string()))
                } else {
                    Ok(())
                }
            }
        }
        let uc = LaunchService::new(PartialSpawner, FakeDetector(vec!["wt.exe".to_string()]));
        let items = [
            item("a", "ok", false),
            item("b", "fail", false),
            item("c", "ok2", false),
        ];
        let (succeeded, failed) = uc.launch_many(&items);
        assert_eq!(2, succeeded);
        assert_eq!(vec![1], failed);
    }
}
