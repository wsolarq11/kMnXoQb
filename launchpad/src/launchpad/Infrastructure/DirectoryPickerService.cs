using Launchpad.Core.Ports;
using Microsoft.UI.Xaml;
using Windows.Storage.Pickers;

namespace Launchpad.Infrastructure;

/// <summary>
/// Native FolderPicker. Unpackaged WinUI 3 apps must hand the picker an owner
/// HWND via InitializeWithWindow before ShowAsync, otherwise it throws.
/// </summary>
public sealed class DirectoryPickerService : IDirectoryPicker
{
    private IntPtr _windowHandle;

    public void Attach(IntPtr hwnd) => _windowHandle = hwnd;

    public async Task<string?> PickDirectoryAsync(string initialPath)
    {
        var picker = new FolderPicker { SuggestedStartLocation = PickerLocationId.ComputerFolder };
        picker.FileTypeFilter.Add("*");

        var hwnd = _windowHandle;
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }
}
