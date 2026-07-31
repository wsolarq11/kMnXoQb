namespace Launchpad.Core.Ports;

/// <summary>Directory existence check, separated from UI for testability.</summary>
public interface IDirectoryChecker
{
    bool Exists(string path);
}
