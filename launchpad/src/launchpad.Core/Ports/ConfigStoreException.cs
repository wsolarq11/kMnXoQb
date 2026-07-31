namespace Launchpad.Core.Ports;

/// <summary>Raised when a config file exists but cannot be parsed. The file is never
/// silently discarded; the caller may surface the path to the user.</summary>
public sealed class ConfigParseException(string filePath, string reason, Exception? inner = null)
    : Exception($"Failed to parse config file '{filePath}': {reason}", inner)
{
    public string FilePath { get; } = filePath;
}
