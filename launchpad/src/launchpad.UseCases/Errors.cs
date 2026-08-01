using ErrorOr;

namespace Launchpad.UseCases;

/// <summary>
/// Structured errors for expected launch failures. The UI surfaces
/// <c>Error.Description</c> in the status bar instead of swallowing exceptions.
/// </summary>
public static class LaunchErrors
{
    // Descriptions stay in English on purpose: they carry diagnostic details
    // (paths, exceptions) and are never localized — the UI translates the
    // status-bar prefix, not the technical detail. See spec i18n section.
    public static Error ProcessNotFound(string executable) =>
        Error.NotFound("Launch.ProcessNotFound", $"Executable not found: {executable}");

    public static Error WorkingDirectoryMissing(string directory) =>
        Error.Validation("Launch.WorkingDirectoryMissing", $"Working directory does not exist: {directory}");

    public static Error AccessDenied(string executable) =>
        Error.Unauthorized("Launch.AccessDenied", $"Access denied starting: {executable}");

    public static Error Unknown(string message) =>
        Error.Failure("Launch.Unknown", message);
}

public static class StoreErrors
{
    public static Error WriteFailed(string path, string message) =>
        Error.Failure("Store.WriteFailed", $"Failed to write {path}: {message}");

    public static Error ReadFailed(string path, string message) =>
        Error.Failure("Store.ReadFailed", $"Failed to read {path}: {message}");
}

/// <summary>Win32 process-spawn error codes surfaced by Process.Start.</summary>
internal static class Win32ErrorCode
{
    public const int FileNotFound = 2;
    public const int PathNotFound = 3;
    public const int AccessDenied = 5;
    public const int InvalidDirectory = 267; // ERROR_DIRECTORY
}
