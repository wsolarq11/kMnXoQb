using System.Globalization;

namespace Launchpad.Core.Localization;

/// <summary>Language setting value semantics; Auto follows the system language.</summary>
public enum AppLanguage
{
    Auto,
    ZhCn,
    EnUs,
}

/// <summary>
/// Pure translation table: key → text per language, plus the pure helpers that
/// resolve the setting value and the system language. No I/O, no UI state —
/// the imperative shell (LanguageService in the UI project) owns the current
/// language and calls back into these functions.
/// </summary>
public static class Translations
{
    private static readonly IReadOnlyDictionary<LanguageKey, string> ZhCn = new Dictionary<LanguageKey, string>
    {
        [LanguageKey.ToggleConfirm] = "确认",
        [LanguageKey.BtnNew] = "新建",
        [LanguageKey.BtnSelectAll] = "全选",
        [LanguageKey.BtnLaunchSelected] = "启动选中项",
        [LanguageKey.SearchPlaceholder] = "搜索...",
        [LanguageKey.StatItems] = "条目",
        [LanguageKey.StatRecent] = "最近",
        [LanguageKey.EmptyTitle] = "暂无条目",
        [LanguageKey.EmptySubtitle] = "点击「新建」添加第一个启动条目",
        [LanguageKey.EmptyButton] = "新建条目",
        [LanguageKey.NoMatchTitle] = "无匹配结果",
        [LanguageKey.NoMatchSubtitle] = "换个关键词试试",
        [LanguageKey.TooltipEdit] = "编辑",
        [LanguageKey.TooltipDelete] = "删除",
        [LanguageKey.TooltipMoveUp] = "上移",
        [LanguageKey.TooltipMoveDown] = "下移",
        [LanguageKey.TooltipTheme] = "主题：自动（跟随系统）— 点击循环：自动 → 深色 → 浅色",
        [LanguageKey.TooltipLanguage] = "语言：自动（跟随系统）— 点击循环：自动 → 中文 → English",
        [LanguageKey.LanguageAuto] = "自动",
        [LanguageKey.LanguageZh] = "中文",
        [LanguageKey.LanguageEn] = "EN",
        [LanguageKey.FieldName] = "名称",
        [LanguageKey.FieldDirectory] = "目录",
        [LanguageKey.FieldCommand] = "命令",
        [LanguageKey.FieldTerminal] = "终端（可选）",
        [LanguageKey.PlaceholderRequired] = "必填",
        [LanguageKey.PlaceholderDirectory] = "例如 D:\\projects\\demo",
        [LanguageKey.PlaceholderTerminal] = "例如 pwsh、cmd、powershell",
        [LanguageKey.CheckboxConfirmBeforeLaunch] = "启动前确认",
        [LanguageKey.EditTitleNew] = "新建条目",
        [LanguageKey.EditTitleEdit] = "编辑条目",
        [LanguageKey.BtnSave] = "保存",
        [LanguageKey.BtnCancel] = "取消",
        [LanguageKey.BtnDelete] = "删除",
        [LanguageKey.ValidationDirectoryExists] = "目录存在",
        [LanguageKey.ValidationDirectoryMissing] = "目录不存在",
        [LanguageKey.ValidationNameRequired] = "名称不能为空",
        [LanguageKey.ValidationCommandRequired] = "命令不能为空",
        [LanguageKey.DialogConfirmLaunchTitle] = "确认启动",
        [LanguageKey.BtnLaunch] = "启动",
        [LanguageKey.DialogDeleteItemTitle] = "删除条目",
        [LanguageKey.DialogDeleteItemMessage] = "删除「{0}」？\n此操作无法撤销。",
        [LanguageKey.DialogBatchTitle] = "确认 {0} 个启动",
        [LanguageKey.BtnLaunchAll] = "全部启动",
        [LanguageKey.DialogLabelName] = "名称：{0}",
        [LanguageKey.DialogLabelCommand] = "命令：{0}",
        [LanguageKey.DialogLabelDirectory] = "目录：{0}",
        [LanguageKey.StatusAdded] = "已添加",
        [LanguageKey.StatusUpdated] = "已更新",
        [LanguageKey.StatusDeleted] = "已删除",
        [LanguageKey.StatusLaunched] = "已启动：{0}",
        [LanguageKey.StatusLaunchedN] = "已启动 {0} 个条目",
        [LanguageKey.StatusLaunchedPartial] = "已启动 {0}/{1} 个条目（{2} 个失败）",
        [LanguageKey.StatusConfigError] = "配置错误：{0}",
        [LanguageKey.StatusSaveFailed] = "保存失败：{0}",
        [LanguageKey.StatusLaunchFailed] = "启动失败：{0}",
        [LanguageKey.StatusRecovered] = "config.json 已损坏，已从 config.json.bak 恢复",
        [LanguageKey.DangerReasonDangerously] = "包含 --dangerously 标志",
        [LanguageKey.DangerReasonYolo] = "包含 --yolo 标志",
        [LanguageKey.DangerReasonSkipPermissions] = "包含 --skip-permissions 标志",
        [LanguageKey.DangerReasonBypassApprovals] = "包含 --bypass-approvals 标志",
        [LanguageKey.DangerReasonBypassSandbox] = "包含 --bypass-sandbox 标志",
    };

    private static readonly IReadOnlyDictionary<LanguageKey, string> EnUs = new Dictionary<LanguageKey, string>
    {
        [LanguageKey.ToggleConfirm] = "Confirm",
        [LanguageKey.BtnNew] = "New",
        [LanguageKey.BtnSelectAll] = "Select All",
        [LanguageKey.BtnLaunchSelected] = "Launch Selected",
        [LanguageKey.SearchPlaceholder] = "Search...",
        [LanguageKey.StatItems] = "ITEMS",
        [LanguageKey.StatRecent] = "RECENT",
        [LanguageKey.EmptyTitle] = "No items yet",
        [LanguageKey.EmptySubtitle] = "Click + New to add your first launch item",
        [LanguageKey.EmptyButton] = "New Item",
        [LanguageKey.NoMatchTitle] = "No matches",
        [LanguageKey.NoMatchSubtitle] = "Try a different search query",
        [LanguageKey.TooltipEdit] = "Edit",
        [LanguageKey.TooltipDelete] = "Delete",
        [LanguageKey.TooltipMoveUp] = "Move Up",
        [LanguageKey.TooltipMoveDown] = "Move Down",
        [LanguageKey.TooltipTheme] = "Theme: auto (follow system) — click cycles: auto → dark → light",
        [LanguageKey.TooltipLanguage] = "Language: auto (follow system) — click cycles: auto → Chinese → English",
        [LanguageKey.LanguageAuto] = "Auto",
        [LanguageKey.LanguageZh] = "中文",
        [LanguageKey.LanguageEn] = "EN",
        [LanguageKey.FieldName] = "Name",
        [LanguageKey.FieldDirectory] = "Directory",
        [LanguageKey.FieldCommand] = "Command",
        [LanguageKey.FieldTerminal] = "Terminal (optional)",
        [LanguageKey.PlaceholderRequired] = "Required",
        [LanguageKey.PlaceholderDirectory] = "e.g. D:\\projects\\demo",
        [LanguageKey.PlaceholderTerminal] = "e.g. pwsh, cmd, powershell",
        [LanguageKey.CheckboxConfirmBeforeLaunch] = "Confirm before launch",
        [LanguageKey.EditTitleNew] = "New Item",
        [LanguageKey.EditTitleEdit] = "Edit Item",
        [LanguageKey.BtnSave] = "Save",
        [LanguageKey.BtnCancel] = "Cancel",
        [LanguageKey.BtnDelete] = "Delete",
        [LanguageKey.ValidationDirectoryExists] = "Directory exists",
        [LanguageKey.ValidationDirectoryMissing] = "Directory does not exist",
        [LanguageKey.ValidationNameRequired] = "Name is required",
        [LanguageKey.ValidationCommandRequired] = "Command is required",
        [LanguageKey.DialogConfirmLaunchTitle] = "Confirm Launch",
        [LanguageKey.BtnLaunch] = "Launch",
        [LanguageKey.DialogDeleteItemTitle] = "Delete Item",
        [LanguageKey.DialogDeleteItemMessage] = "Delete '{0}'?\nThis cannot be undone.",
        [LanguageKey.DialogBatchTitle] = "Confirm {0} launches",
        [LanguageKey.BtnLaunchAll] = "Launch All",
        [LanguageKey.DialogLabelName] = "Name: {0}",
        [LanguageKey.DialogLabelCommand] = "Command: {0}",
        [LanguageKey.DialogLabelDirectory] = "Directory: {0}",
        [LanguageKey.StatusAdded] = "Added",
        [LanguageKey.StatusUpdated] = "Updated",
        [LanguageKey.StatusDeleted] = "Deleted",
        [LanguageKey.StatusLaunched] = "Launched: {0}",
        [LanguageKey.StatusLaunchedN] = "Launched {0} items",
        [LanguageKey.StatusLaunchedPartial] = "Launched {0} of {1} items ({2} failed)",
        [LanguageKey.StatusConfigError] = "Config error: {0}",
        [LanguageKey.StatusSaveFailed] = "Save failed: {0}",
        [LanguageKey.StatusLaunchFailed] = "Launch failed: {0}",
        [LanguageKey.StatusRecovered] = "config.json was corrupted; restored from config.json.bak",
        [LanguageKey.DangerReasonDangerously] = "contains --dangerously flag",
        [LanguageKey.DangerReasonYolo] = "contains --yolo flag",
        [LanguageKey.DangerReasonSkipPermissions] = "contains --skip-permissions flag",
        [LanguageKey.DangerReasonBypassApprovals] = "contains --bypass-approvals flag",
        [LanguageKey.DangerReasonBypassSandbox] = "contains --bypass-sandbox flag",
    };

    /// <summary>Resolves the settings value ("auto" / "zh-CN" / "en-US"); unknown
    /// or empty values fall back to Auto so old settings files keep working.</summary>
    public static AppLanguage Resolve(string? value) => value switch
    {
        "zh-CN" => AppLanguage.ZhCn,
        "en-US" => AppLanguage.EnUs,
        _ => AppLanguage.Auto,
    };

    /// <summary>Maps the system's first preferred language to an app language;
    /// anything zh-prefixed selects Chinese, everything else falls back to English.</summary>
    public static AppLanguage FromSystemLanguage(string? firstLanguage) =>
        firstLanguage?.StartsWith("zh", StringComparison.OrdinalIgnoreCase) == true
            ? AppLanguage.ZhCn
            : AppLanguage.EnUs;

    /// <summary>The language actually shown: the explicit setting wins, Auto follows the system.</summary>
    public static AppLanguage Effective(AppLanguage setting, AppLanguage system) =>
        setting == AppLanguage.Auto ? system : setting;

    public static string T(LanguageKey key, AppLanguage language) =>
        language == AppLanguage.ZhCn ? ZhCn[key] : EnUs[key];

    /// <summary>Key text with positional placeholders ({0}, {1}, ...) filled in.</summary>
    public static string Format(LanguageKey key, AppLanguage language, params object[] args) =>
        string.Format(CultureInfo.InvariantCulture, T(key, language), args);
}
