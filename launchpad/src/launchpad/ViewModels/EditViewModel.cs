using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Launchpad.Core.Domain;
using Launchpad.Core.Localization;
using Launchpad.Core.Models;
using Launchpad.Core.Ports;
using Launchpad.Localization;
using Launchpad.UseCases;

namespace Launchpad.ViewModels;

/// <summary>
/// Edit dialog form state: fields, live validation, danger warning,
/// and directory picking through ports (no direct I/O). Validation and danger
/// reasons are language-independent keys; the XAML translates them with
/// LanguageKeyTextConverter against the current language.
/// </summary>
public sealed partial class EditViewModel : ObservableObject
{
    private readonly IDirectoryChecker _directoryChecker;
    private readonly IDirectoryPicker _directoryPicker;
    private readonly LanguageService _language;
    private readonly string? _originalId;

    // Partial properties (C# 13): AOT-compatible source generation (MVVMTK0045).
    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Directory { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Command { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? Terminal { get; set; }

    [ObservableProperty]
    public partial bool Confirm { get; set; } = true;

    [ObservableProperty]
    public partial LanguageKey? NameError { get; set; }

    [ObservableProperty]
    public partial LanguageKey? CommandError { get; set; }

    [ObservableProperty]
    public partial LanguageKey? DangerWarning { get; set; }

    public bool IsNew { get; }

    public bool HasNameError => NameError is not null;

    public bool HasCommandError => CommandError is not null;

    public bool DirectoryExists => _directoryChecker.Exists(Directory);

    public bool ShowDirectoryValidation => !string.IsNullOrWhiteSpace(Directory);

    public bool ShowDangerWarning => DangerWarning is not null;

    public string DirGlyph => DirectoryExists ? LucideGlyph.CheckCircle : LucideGlyph.AlertTriangle;

    public LanguageKey DirectoryValidationKey => DirectoryExists
        ? LanguageKey.ValidationDirectoryExists
        : LanguageKey.ValidationDirectoryMissing;

    public Microsoft.UI.Xaml.Media.Brush DirGlyphBrush => DirectoryExists
        ? (Microsoft.UI.Xaml.Media.Brush)Microsoft.UI.Xaml.Application.Current.Resources["SuccessBrush"]
        : (Microsoft.UI.Xaml.Media.Brush)Microsoft.UI.Xaml.Application.Current.Resources["DangerBrush"];

    // --- Localized text (keys resolved through LanguageService) ---
    public string this[LanguageKey key] => _language[key];

    public string FieldNameText => _language[LanguageKey.FieldName];
    public string FieldDirectoryText => _language[LanguageKey.FieldDirectory];
    public string FieldCommandText => _language[LanguageKey.FieldCommand];
    public string FieldTerminalText => _language[LanguageKey.FieldTerminal];
    public string PlaceholderRequiredText => _language[LanguageKey.PlaceholderRequired];
    public string PlaceholderDirectoryText => _language[LanguageKey.PlaceholderDirectory];
    public string PlaceholderTerminalText => _language[LanguageKey.PlaceholderTerminal];
    public string CheckboxConfirmText => _language[LanguageKey.CheckboxConfirmBeforeLaunch];

    public EditViewModel(
        IDirectoryChecker checker,
        IDirectoryPicker picker,
        LanguageService language,
        LaunchItem? item)
    {
        _directoryChecker = checker;
        _directoryPicker = picker;
        _language = language;
        _language.PropertyChanged += OnLanguageChanged;
        IsNew = item is null;
        if (item is not null)
        {
            // Property assignment triggers OnCommandChanged/OnDirectoryChanged
            // once each; both only re-evaluate derived state (idempotent).
            Name = item.Name;
            Directory = item.Directory;
            Command = item.Command;
            Terminal = item.Terminal;
            Confirm = item.Confirm;
            _originalId = item.Id;
        }

        RefreshDangerWarning();
    }

    private void OnLanguageChanged(object? sender, PropertyChangedEventArgs e) => OnPropertyChanged(string.Empty);

    partial void OnCommandChanged(string value) => RefreshDangerWarning();

    partial void OnDirectoryChanged(string value)
    {
        OnPropertyChanged(nameof(DirectoryExists));
        OnPropertyChanged(nameof(ShowDirectoryValidation));
        OnPropertyChanged(nameof(DirGlyph));
        OnPropertyChanged(nameof(DirectoryValidationKey));
        OnPropertyChanged(nameof(DirGlyphBrush));
    }

    private void RefreshDangerWarning()
    {
        // Property assignment raises PropertyChanged(DangerWarning) itself.
        DangerWarning = DangerousFlagDetector.DangerousReason(Command);
        OnPropertyChanged(nameof(ShowDangerWarning));
    }

    public bool Validate()
    {
        var errors = ItemValidator.Validate(Name, Command);
        NameError = errors.NameError;
        CommandError = errors.CommandError;
        OnPropertyChanged(nameof(HasNameError));
        OnPropertyChanged(nameof(HasCommandError));
        return errors.IsValid;
    }

    public LaunchItem BuildItem(IReadOnlyList<LaunchItem> existing)
    {
        var fresh = ItemUseCase.NewItem(Name.Trim(), Directory.Trim(), Command.Trim(), Confirm, Terminal, existing);
        return IsNew ? fresh : fresh with { Id = _originalId ?? fresh.Id };
    }

    [RelayCommand]
    private async Task PickDirectoryAsync()
    {
        var initial = string.IsNullOrWhiteSpace(Directory)
            ? System.IO.Directory.GetCurrentDirectory()
            : Directory;
        var picked = await _directoryPicker.PickDirectoryAsync(initial);
        if (picked is not null)
        {
            Directory = picked;
        }
    }
}
