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
    public partial string? NameError { get; set; }

    [ObservableProperty]
    public partial string? CommandError { get; set; }

    [ObservableProperty]
    public partial string? DangerWarning { get; set; }

    public bool IsNew { get; }

    public bool DirectoryExists => _directoryChecker.Exists(Directory);

    public bool ShowDirectoryValidation => !string.IsNullOrWhiteSpace(Directory);

    public bool ShowDangerWarning => DangerWarning is not null;

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
        // Property assignment raises PropertyChanged(DangerWarning) itself.
        DangerWarning = DangerousFlagDetector.DangerousReason(Command);
        OnPropertyChanged(nameof(ShowDangerWarning));
    }

    public bool Validate()
    {
        var errors = ItemValidator.Validate(Name, Command);
        NameError = errors.NameError;
        CommandError = errors.CommandError;
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
