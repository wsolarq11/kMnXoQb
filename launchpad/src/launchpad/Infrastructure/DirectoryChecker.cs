using Launchpad.Core.Ports;

namespace Launchpad.Infrastructure;

/// <summary>Filesystem existence check; the only I/O the edit form performs.</summary>
public sealed class DirectoryChecker : IDirectoryChecker
{
    public bool Exists(string path) => Directory.Exists(path);
}
