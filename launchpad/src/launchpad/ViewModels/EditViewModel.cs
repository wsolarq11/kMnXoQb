using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Launchpad.Core.Domain;
using Launchpad.Core.Models;
using Launchpad.Core.Ports;
using Launchpad.UseCases;

namespace Launchpad.ViewModels;

/// <summary>
/// Edit dialog form state: fields, live validation, danger warning,
/// and directory picking through ports (no direct I/O).
/// </summary>
public sealed partial class EditViewModel : ObservableObject
{
    private readonly IDirectoryChecker _directoryChecker;
    private readonly IDirectoryPicker _directoryPicker;
    private readonly string? _originalId;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _directory = string.Empty;

    [ObservableProperty]
    private string _command = string.Empty;

    [ObservableProperty]
    private string? _terminal;

    [ObservableProperty]
    private bool _confirm = true;

    [ObservableProperty]
    private string? _nameError;

    [ObservableProperty]
    private string? _commandError;

    [ObservableProperty]
    private string? _dangerWarning;

    public bool IsNew { get; }

    public bool DirectoryExists => _directoryChecker.Exists(_directory);

    public bool ShowDirectoryValidation => !string.IsNullOrWhiteSpace(_directory);

    public bool ShowDangerWarning => _dangerWarning is not null;

    public string DirGlyph => DirectoryExists ? LucideGlyph.CheckCircle : LucideGlyph.AlertTriangle;

    public string DirectoryValidationText => DirectoryExists ? "Directory exists" : "Directory does not exist";

    public Microsoft.UI.Xaml.Media.Brush DirGlyphBrush => DirectoryExists
        ? (Microsoft.UI.Xaml.Media.Brush)Microsoft.UI.Xaml.Application.Current.Resources["SuccessBrush"]
        : (Microsoft.UI.Xaml.Media.Brush)Microsoft.UI.Xaml.Application.Current.Resources["DangerBrush"];

    public EditViewModel(IDirectoryChecker checker, IDirectoryPicker picker, LaunchItem? item)
    {
        _directoryChecker = checker;
        _directoryPicker = picker;
        IsNew = item is null;
        if (item is not null)
        {
            _name = item.Name;
            _directory = item.Directory;
            _command = item.Command;
            _terminal = item.Terminal;
            _confirm = item.Confirm;
            _originalId = item.Id;
        }

        RefreshDangerWarning();
    }

    partial void OnCommandChanged(string value) => RefreshDangerWarning();

    partial void OnDirectoryChanged(string value)
    {
        OnPropertyChanged(nameof(DirectoryExists));
        OnPropertyChanged(nameof(ShowDirectoryValidation));
        OnPropertyChanged(nameof(DirGlyph));
        OnPropertyChanged(nameof(DirectoryValidationText));
        OnPropertyChanged(nameof(DirGlyphBrush));
    }

    private void RefreshDangerWarning()
    {
        _dangerWarning = DangerousFlagDetector.DangerousReason(_command);
        OnPropertyChanged(nameof(DangerWarning));
        OnPropertyChanged(nameof(ShowDangerWarning));
    }

    public bool Validate()
    {
        var errors = ItemValidator.Validate(_name, _command);
        NameError = errors.NameError;
        CommandError = errors.CommandError;
        return errors.IsValid;
    }

    public LaunchItem BuildItem()
    {
        var fresh = ItemUseCase.NewItem(_name.Trim(), _directory.Trim(), _command.Trim(), _confirm, _terminal);
        return IsNew ? fresh : fresh with { Id = _originalId ?? fresh.Id };
    }

    [RelayCommand]
    private async Task PickDirectoryAsync()
    {
        var initial = string.IsNullOrWhiteSpace(_directory)
            ? System.IO.Directory.GetCurrentDirectory()
            : _directory;
        var picked = await _directoryPicker.PickDirectoryAsync(initial);
        if (picked is not null)
        {
            Directory = picked;
        }
    }
}
