namespace Launchpad.Core.Ports;

/// <summary>Native folder picker; returns null when the user cancels.</summary>
public interface IDirectoryPicker
{
    Task<string?> PickDirectoryAsync(string initialPath);
}
