using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using Launchpad.Core.Domain;
using Launchpad.Infrastructure;
using Launchpad.UseCases;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Launchpad.Core.Tests;

/// <summary>
/// Machine-enforced layering (CLAUDE.md: UI → UseCases → Core ← Infrastructure).
/// Loading only the three pure-library assemblies keeps these rules fast and
/// free of WinUI runtime dependencies; the UI project rule is source-file based.
/// </summary>
public sealed class ArchitectureTests
{
    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(
            typeof(DangerousFlagDetector).Assembly,
            typeof(LaunchUseCase).Assembly,
            typeof(ConfigStore).Assembly)
        .Build();

    private static readonly IObjectProvider<IType> CoreLayer =
        Types().That().ResideInNamespace("Launchpad.Core").As("Core Layer");

    private static readonly IObjectProvider<IType> UseCasesLayer =
        Types().That().ResideInNamespace("Launchpad.UseCases").As("UseCases Layer");

    private static readonly IObjectProvider<IType> WinUiTypes =
        Types().That().ResideInNamespace("Microsoft.UI").As("WinUI Types");

    private static readonly IObjectProvider<IType> SystemIoTypes =
        Types().That().ResideInNamespace("System.IO").As("System.IO Types");

    [Fact]
    public void Core_ShouldNotDependOnWinUI()
    {
        Types().That().Are(CoreLayer).Should().NotDependOnAny(WinUiTypes)
            .Because("Core is a zero-WinUI pure library").WithoutRequiringPositiveResults().Check(Architecture);
    }

    [Fact]
    public void Core_ShouldNotDependOnSystemIO()
    {
        Types().That().Are(CoreLayer).Should().NotDependOnAny(SystemIoTypes)
            .Because("Core performs no I/O; ports abstract it").WithoutRequiringPositiveResults().Check(Architecture);
    }

    [Fact]
    public void UseCases_ShouldNotDependOnWinUI()
    {
        Types().That().Are(UseCasesLayer).Should().NotDependOnAny(WinUiTypes)
            .Because("UseCases stays UI-agnostic for testability").WithoutRequiringPositiveResults().Check(Architecture);
    }

    [Fact]
    public void Core_ShouldNotDependOnUseCasesOrInfrastructure()
    {
        Types().That().Are(CoreLayer).Should().NotDependOnAny(Types().That().ResideInNamespace("Launchpad.UseCases"))
            .AndShould().NotDependOnAny(Types().That().ResideInNamespace("Launchpad.Infrastructure"))
            .Because("dependencies point downward: Core is the bottom layer").WithoutRequiringPositiveResults().Check(Architecture);
    }
}
