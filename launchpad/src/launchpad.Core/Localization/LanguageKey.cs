namespace Launchpad.Core.Localization;

/// <summary>
/// Stable string keys for every user-visible message. Code must reference keys,
/// never literals; the current language's text comes from <see cref="Translations"/>.
/// Keys are language-independent, so tests assert keys instead of prose.
/// </summary>
public enum LanguageKey
{
    // Main screen
    ToggleConfirm,
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

    // Edit dialog
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

    // Confirm dialogs
    DialogConfirmLaunchTitle,
    BtnLaunch,
    DialogDeleteItemTitle,
    DialogDeleteItemMessage,
    DialogBatchTitle,
    BtnLaunchAll,
    DialogLabelName,
    DialogLabelCommand,
    DialogLabelDirectory,

    // Status bar
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

    // Dangerous-command reasons
    DangerReasonDangerously,
    DangerReasonYolo,
    DangerReasonSkipPermissions,
    DangerReasonBypassApprovals,
    DangerReasonBypassSandbox,
}
