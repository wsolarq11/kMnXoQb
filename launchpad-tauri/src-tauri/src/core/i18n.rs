//! Stable language keys + zh-CN/en-US translation tables + pure language
//! resolution helpers. Ported 1:1 from C# LanguageKey / Translations.

/// Language-independent stable keys for every user-visible message.
/// The numeric order defines the table index; keep in sync with TS side.
#[derive(Debug, Clone, Copy, PartialEq, Eq, serde::Serialize)]
#[repr(u8)]
pub enum LanguageKey {
    ToggleConfirm = 0,
    BtnNew,
    BtnSelectAll,
    BtnLaunchSelected,
    SearchPlaceholder,
    StatItems,
    StatRecent,
    EmptyTitle,
    EmptySubtitle,
    EmptyButton,
    NoMatchTitle,
    NoMatchSubtitle,
    TooltipEdit,
    TooltipDelete,
    TooltipMoveUp,
    TooltipMoveDown,
    TooltipTheme,
    TooltipLanguage,
    LanguageAuto,
    LanguageZh,
    LanguageEn,
    FieldName,
    FieldDirectory,
    FieldCommand,
    FieldTerminal,
    PlaceholderRequired,
    PlaceholderDirectory,
    PlaceholderTerminal,
    CheckboxConfirmBeforeLaunch,
    EditTitleNew,
    EditTitleEdit,
    BtnSave,
    BtnCancel,
    BtnDelete,
    ValidationDirectoryExists,
    ValidationDirectoryMissing,
    ValidationNameRequired,
    ValidationCommandRequired,
    DialogConfirmLaunchTitle,
    BtnLaunch,
    DialogDeleteItemTitle,
    DialogDeleteItemMessage,
    DialogBatchTitle,
    BtnLaunchAll,
    DialogLabelName,
    DialogLabelCommand,
    DialogLabelDirectory,
    StatusAdded,
    StatusUpdated,
    StatusDeleted,
    StatusLaunched,
    StatusLaunchedN,
    StatusLaunchedPartial,
    StatusConfigError,
    StatusSaveFailed,
    StatusLaunchFailed,
    StatusRecovered,
    DangerReasonDangerously,
    DangerReasonYolo,
    DangerReasonSkipPermissions,
    DangerReasonBypassApprovals,
    DangerReasonBypassSandbox,
    BootLoading,
}

pub const KEY_COUNT: usize = 63;

#[derive(Debug, Clone, Copy, PartialEq, Eq, serde::Serialize)]
pub enum AppLanguage {
    #[serde(rename = "auto")]
    Auto,
    #[serde(rename = "zh-CN")]
    ZhCn,
    #[serde(rename = "en-US")]
    EnUs,
}

const ZH_CN: [&str; KEY_COUNT] = [
    "确认",
    "新建",
    "全选",
    "启动选中项",
    "搜索...",
    "条目",
    "最近",
    "暂无条目",
    "点击「新建」添加第一个启动条目",
    "新建条目",
    "无匹配结果",
    "换个关键词试试",
    "编辑",
    "删除",
    "上移",
    "下移",
    "主题：自动（跟随系统）— 点击循环：自动 → 深色 → 浅色",
    "语言：自动（跟随系统）— 点击循环：自动 → 中文 → English",
    "自动",
    "中文",
    "EN",
    "名称",
    "目录",
    "命令",
    "终端（可选）",
    "必填",
    "例如 D:\\projects\\demo",
    "例如 pwsh、cmd、powershell",
    "启动前确认",
    "新建条目",
    "编辑条目",
    "保存",
    "取消",
    "删除",
    "目录存在",
    "目录不存在",
    "名称不能为空",
    "命令不能为空",
    "确认启动",
    "启动",
    "删除条目",
    "删除「{0}」？\n此操作无法撤销。",
    "确认 {0} 个启动",
    "全部启动",
    "名称：{0}",
    "命令：{0}",
    "目录：{0}",
    "已添加",
    "已更新",
    "已删除",
    "已启动：{0}",
    "已启动 {0} 个条目",
    "已启动 {0}/{1} 个条目（{2} 个失败）",
    "配置错误：{0}",
    "保存失败：{0}",
    "启动失败：{0}",
    "config.json 已损坏，已从 config.json.bak 恢复",
    "包含 --dangerously 标志",
    "包含 --yolo 标志",
    "包含 --skip-permissions 标志",
    "包含 --bypass-approvals 标志",
    "包含 --bypass-sandbox 标志",
    "正在启动...",
];

const EN_US: [&str; KEY_COUNT] = [
    "Confirm",
    "New",
    "Select All",
    "Launch Selected",
    "Search...",
    "ITEMS",
    "RECENT",
    "No items yet",
    "Click + New to add your first launch item",
    "New Item",
    "No matches",
    "Try a different search query",
    "Edit",
    "Delete",
    "Move Up",
    "Move Down",
    "Theme: auto (follow system) — click cycles: auto → dark → light",
    "Language: auto (follow system) — click cycles: auto → Chinese → English",
    "Auto",
    "中文",
    "EN",
    "Name",
    "Directory",
    "Command",
    "Terminal (optional)",
    "Required",
    "e.g. D:\\projects\\demo",
    "e.g. pwsh, cmd, powershell",
    "Confirm before launch",
    "New Item",
    "Edit Item",
    "Save",
    "Cancel",
    "Delete",
    "Directory exists",
    "Directory does not exist",
    "Name is required",
    "Command is required",
    "Confirm Launch",
    "Launch",
    "Delete Item",
    "Delete '{0}'?\nThis cannot be undone.",
    "Confirm {0} launches",
    "Launch All",
    "Name: {0}",
    "Command: {0}",
    "Directory: {0}",
    "Added",
    "Updated",
    "Deleted",
    "Launched: {0}",
    "Launched {0} items",
    "Launched {0} of {1} items ({2} failed)",
    "Config error: {0}",
    "Save failed: {0}",
    "Launch failed: {0}",
    "config.json was corrupted; restored from config.json.bak",
    "contains --dangerously flag",
    "contains --yolo flag",
    "contains --skip-permissions flag",
    "contains --bypass-approvals flag",
    "contains --bypass-sandbox flag",
    "Starting...",
];

/// Resolves the settings value ("auto" / "zh-CN" / "en-US"); unknown or empty
/// values fall back to Auto so old settings files keep working.
pub fn resolve(value: Option<&str>) -> AppLanguage {
    match value {
        Some("zh-CN") => AppLanguage::ZhCn,
        Some("en-US") => AppLanguage::EnUs,
        _ => AppLanguage::Auto,
    }
}

/// Maps the system's first preferred language to an app language; anything
/// zh-prefixed selects Chinese, everything else falls back to English.
pub fn from_system_language(first_language: Option<&str>) -> AppLanguage {
    match first_language {
        Some(lang) if lang.to_ascii_lowercase().starts_with("zh") => AppLanguage::ZhCn,
        _ => AppLanguage::EnUs,
    }
}

/// The language actually shown: the explicit setting wins, Auto follows the system.
pub fn effective(setting: AppLanguage, system: AppLanguage) -> AppLanguage {
    if setting == AppLanguage::Auto {
        system
    } else {
        setting
    }
}

/// Key text for the given language. Missing keys panic — the completeness
/// test is the guard (mirrors C# KeyNotFoundException failure mode).
pub fn t(key: LanguageKey, language: AppLanguage) -> &'static str {
    let table = match language {
        AppLanguage::ZhCn => &ZH_CN,
        _ => &EN_US,
    };
    table[key as usize]
}

/// Key text with positional placeholders ({0}, {1}, ...) filled in.
pub fn format(key: LanguageKey, language: AppLanguage, args: &[&str]) -> String {
    let mut out = t(key, language).to_string();
    for (i, arg) in args.iter().enumerate() {
        out = out.replace(&format!("{{{i}}}"), arg);
    }
    out
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn every_key_exists_in_both_languages() {
        for idx in 0..KEY_COUNT {
            let key = LanguageKey::from(idx as u8);
            assert!(!t(key, AppLanguage::ZhCn).is_empty(), "zh-CN missing {idx}");
            assert!(!t(key, AppLanguage::EnUs).is_empty(), "en-US missing {idx}");
        }
    }

    impl LanguageKey {
        fn from(idx: u8) -> LanguageKey {
            assert!(idx < KEY_COUNT as u8, "key index {idx} out of range");
            // SAFETY: idx < KEY_COUNT and the enum has exactly KEY_COUNT
            // sequential variants starting at 0.
            unsafe { std::mem::transmute(idx) }
        }
    }

    #[test]
    fn resolve_maps_settings_values() {
        assert_eq!(AppLanguage::Auto, resolve(None));
        assert_eq!(AppLanguage::Auto, resolve(Some("auto")));
        assert_eq!(AppLanguage::Auto, resolve(Some("unknown-value")));
        assert_eq!(AppLanguage::ZhCn, resolve(Some("zh-CN")));
        assert_eq!(AppLanguage::EnUs, resolve(Some("en-US")));
    }

    #[test]
    fn from_system_language_maps_first_preferred() {
        assert_eq!(AppLanguage::ZhCn, from_system_language(Some("zh-CN")));
        assert_eq!(AppLanguage::ZhCn, from_system_language(Some("zh-Hans-CN")));
        assert_eq!(AppLanguage::EnUs, from_system_language(Some("en-US")));
        assert_eq!(AppLanguage::EnUs, from_system_language(Some("fr-FR")));
        assert_eq!(AppLanguage::EnUs, from_system_language(None));
    }

    #[test]
    fn effective_auto_follows_system() {
        assert_eq!(
            AppLanguage::ZhCn,
            effective(AppLanguage::Auto, AppLanguage::ZhCn)
        );
        assert_eq!(
            AppLanguage::EnUs,
            effective(AppLanguage::Auto, AppLanguage::EnUs)
        );
        assert_eq!(
            AppLanguage::ZhCn,
            effective(AppLanguage::ZhCn, AppLanguage::EnUs)
        );
        assert_eq!(
            AppLanguage::EnUs,
            effective(AppLanguage::EnUs, AppLanguage::ZhCn)
        );
    }

    #[test]
    #[should_panic]
    fn t_unknown_key_resolution_panics() {
        // Mirror of C# TranslationsTests.T_UnknownKeyResolution_Throws.
        let _ = t(LanguageKey::from(KEY_COUNT as u8), AppLanguage::ZhCn);
    }

    #[test]
    fn format_fills_placeholders() {
        let zh = format(
            LanguageKey::DialogDeleteItemMessage,
            AppLanguage::ZhCn,
            &["snow"],
        );
        assert!(zh.contains("snow"));
        let en = format(LanguageKey::DialogBatchTitle, AppLanguage::EnUs, &["3"]);
        assert_eq!("Confirm 3 launches", en);
    }
}
