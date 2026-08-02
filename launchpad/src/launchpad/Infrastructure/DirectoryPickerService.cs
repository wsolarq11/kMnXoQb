using Launchpad.Core.Ports;
using Windows.Storage.Pickers;

namespace Launchpad.Infrastructure;

/// <summary>
/// Native FolderPicker. Unpackaged WinUI 3 apps must hand the picker an owner
/// HWND via InitializeWithWindow before ShowAsync, otherwise it throws.
/// </summary>
public sealed class DirectoryPickerService : IDirectoryPicker
{
    private readonly IWindowHandleProvider _windowHandleProvider;

    public DirectoryPickerService(IWindowHandleProvider windowHandleProvider)
    {
        _windowHandleProvider = windowHandleProvider;
    }

    public async Task<string?> PickDirectoryAsync(string initialPath)
    {
        var picker = new FolderPicker { SuggestedStartLocation = PickerLocationId.ComputerFolder };
        picker.FileTypeFilter.Add("*");

        WinRT.Interop.InitializeWithWindow.Initialize(picker, _windowHandleProvider.WindowHandle);

        var folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }
}
