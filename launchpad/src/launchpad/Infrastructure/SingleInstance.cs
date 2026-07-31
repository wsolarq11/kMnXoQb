using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Launchpad.Infrastructure;

/// <summary>
/// Enforces a single running instance via a per-session named mutex.
/// A second instance activates the first window (best effort) and reports
/// it is not primary; the caller exits.
/// </summary>
public sealed class SingleInstance
{
    private const string MutexName = @"Local\WT_Launcher_SingleInstance";
    private readonly Mutex _mutex;

    public SingleInstance()
    {
        _mutex = new Mutex(initiallyOwned: true, MutexName, out var isPrimary);
        IsPrimary = isPrimary;
        if (!IsPrimary)
        {
            ActivateExistingWindow();
        }
    }

    public bool IsPrimary { get; }

    private static void ActivateExistingWindow()
    {
        foreach (var process in Process.GetProcessesByName("launchpad"))
        {
            if (process.MainWindowHandle != IntPtr.Zero)
            {
                SetForegroundWindow(process.MainWindowHandle);
                return;
            }
        }
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
}
