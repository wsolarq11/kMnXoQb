using Xunit;

namespace Launchpad.Core.Tests;

/// <summary>
/// Source-level architecture rule for the WinUI project (not loadable into
/// ArchUnitNET here without the WASDK runtime): UI views/viewmodels must not
/// reference Infrastructure types directly — only App.xaml.cs wires DI.
/// </summary>
public sealed class SourceFileRuleTests
{
    [Fact]
    public void UiProject_Sources_DoNotReferenceInfrastructure()
    {
        var uiDir = FindRepoRoot();
        var files = Directory.GetFiles(uiDir, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
            .Where(f => !f.EndsWith("App.xaml.cs", StringComparison.OrdinalIgnoreCase));

        var offenders = new List<string>();
        foreach (var file in files)
        {
            if (File.ReadAllText(file).Contains("using Launchpad.Infrastructure;", StringComparison.Ordinal))
            {
                offenders.Add(file);
            }
        }

        Assert.True(offenders.Count == 0, "UI sources must not reference Infrastructure:\n" + string.Join("\n", offenders));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 10 && dir is not null; i++, dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "src", "launchpad");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Could not locate launchpad/src/launchpad from test output directory");
    }
}
