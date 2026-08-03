//! Dual-track config-directory resolution (design decision D2):
//! - Portable: upward search from the exe for an ancestor containing config/
//!   (mirrors C# ResolveConfigDir; keeps the directory movable).
//! - Installed (MSI): <user app data dir>/launchpad/config.
//!
//! Both inputs (exe dir, appdata dir) are injected so the resolver is pure.

use std::path::{Path, PathBuf};

#[derive(Debug, Clone, Copy, PartialEq, Eq)]
pub enum InstallForm {
    Portable,
    Installed,
}

pub struct ConfigPaths {
    pub dir: PathBuf,
}

/// Upward search: walk from the exe's directory towards the root, returning
/// the first ancestor's `config/` subdirectory; fall back to `<exe_dir>/config`.
/// Mirrors the C# ResolveConfigDir behavior for the portable form (the config
/// files live INSIDE the config/ folder, not next to it).
pub fn upward_search(exe_dir: &Path) -> PathBuf {
    let mut current = Some(exe_dir.to_path_buf());
    while let Some(dir) = current {
        if dir.join("config").is_dir() {
            return dir.join("config");
        }
        current = dir.parent().map(|p| p.to_path_buf());
    }
    exe_dir.join("config")
}

/// Resolves the config directory for the given install form.
pub fn resolve(install_form: InstallForm, exe_dir: &Path, appdata: Option<&Path>) -> ConfigPaths {
    match install_form {
        InstallForm::Portable => ConfigPaths {
            dir: upward_search(exe_dir),
        },
        InstallForm::Installed => ConfigPaths {
            dir: appdata
                .map(|base| base.join("launchpad").join("config"))
                .unwrap_or_else(|| upward_search(exe_dir)),
        },
    }
}

/// Portable marker: the release zip ships an empty `launchpad.portable` file
/// next to the exe; MSI installs do not have it. Presence decides the form.
pub fn detect_install_form(exe_dir: &Path) -> InstallForm {
    if exe_dir.join("launchpad.portable").exists() {
        InstallForm::Portable
    } else {
        InstallForm::Installed
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    fn scratch(name: &str) -> PathBuf {
        let dir =
            std::env::temp_dir().join(format!("launchpad-paths-{name}-{}", std::process::id()));
        let _ = std::fs::remove_dir_all(&dir);
        std::fs::create_dir_all(&dir).unwrap();
        dir
    }

    #[test]
    fn portable_finds_ancestor_config_dir() {
        let root = scratch("ancestor");
        std::fs::create_dir_all(root.join("a/b/c")).unwrap();
        std::fs::create_dir_all(root.join("config")).unwrap();

        // exe lives at root/a/b/c/bin; the ancestor's config/ folder wins.
        let exe_dir = root.join("a/b/c/bin");
        std::fs::create_dir_all(&exe_dir).unwrap();
        assert_eq!(root.join("config"), upward_search(&exe_dir));
    }

    #[test]
    fn portable_exe_dir_itself_with_config() {
        let root = scratch("own");
        std::fs::create_dir_all(root.join("config")).unwrap();
        assert_eq!(root.join("config"), upward_search(&root));
    }

    #[test]
    fn portable_falls_back_to_exe_dir_config_when_no_ancestor() {
        let root = scratch("fallback");
        std::fs::create_dir_all(root.join("a/b")).unwrap();
        let exe_dir = root.join("a/b");
        assert_eq!(exe_dir.join("config"), upward_search(&exe_dir));
    }

    #[test]
    fn installed_uses_appdata_dir() {
        let appdata = scratch("appdata");
        let exe_dir = scratch("installed-exe");
        let resolved = resolve(InstallForm::Installed, &exe_dir, Some(&appdata));
        assert_eq!(appdata.join("launchpad").join("config"), resolved.dir);
    }

    #[test]
    fn installed_without_appdata_falls_back_to_portable_search() {
        let root = scratch("no-appdata");
        std::fs::create_dir_all(root.join("config")).unwrap();
        let resolved = resolve(InstallForm::Installed, &root, None);
        assert_eq!(root.join("config"), resolved.dir);
    }
}
