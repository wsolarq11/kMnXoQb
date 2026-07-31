namespace Launchpad.Core.Ports;

/// <summary>Checks whether an executable is reachable on PATH.</summary>
public interface ITerminalDetector
{
    bool TerminalAvailable(string name);
}
