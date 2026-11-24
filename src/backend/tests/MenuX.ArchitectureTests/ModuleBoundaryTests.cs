using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace MenuX.ArchitectureTests;

/// <summary>
/// Type-level module boundary rules (doc 05 §3, rule 1): a module may use another module
/// only through that module's Contracts. Protected test group: tenant isolation and
/// boundaries (doc 10 §12.2).
/// </summary>
public sealed class ModuleBoundaryTests
{
    private static readonly Architecture Architecture =
        new ArchLoader().LoadAssemblies(Modules.Assemblies).Build();

    [Theory]
    [MemberData(nameof(Modules.All), MemberType = typeof(Modules))]
    public void Module_uses_other_modules_only_through_their_contracts(string module)
    {
        var others = Modules.Names.Where(n => n != module);

        Types().That().ResideInNamespaceMatching(Modules.ModulePattern(module))
            .Should().NotDependOnAny(
                Types().That().ResideInNamespaceMatching(Modules.InternalsPattern(others)))
            .Because("modules communicate only through *.Contracts (doc 05 §3)")
            .Check(Architecture);
    }

    [Fact]
    public void Contracts_do_not_depend_on_module_internals_or_infrastructure()
    {
        Types().That().ResideInNamespaceMatching(@"^MenuX\.\w+\.Contracts(\..*)?$")
            .Should().NotDependOnAny(
                Types().That().ResideInNamespaceMatching(Modules.InternalsPattern(Modules.Names))
                    .Or().ResideInNamespaceMatching(@"^MenuX\.Infrastructure(\..*)?$")
                    .Or().ResideInNamespaceMatching(@"^MenuX\.Host(\..*)?$"))
            .Because("Contracts are a module's public API and must stay free of internals")
            .Check(Architecture);
    }

    [Fact]
    public void Building_blocks_do_not_depend_on_modules_or_hosts()
    {
        Types().That().ResideInNamespaceMatching(@"^MenuX\.(SharedKernel|Infrastructure)(\..*)?$")
            .Should().NotDependOnAny(
                Types().That().ResideInNamespaceMatching(
                        $@"^MenuX\.({string.Join('|', Modules.Names)})(\..*)?$")
                    .Or().ResideInNamespaceMatching(@"^MenuX\.Host(\..*)?$"))
            .Because("building blocks sit below the modules (doc 05 §2)")
            .Check(Architecture);
    }

    [Fact]
    public void Shared_kernel_does_not_depend_on_infrastructure()
    {
        Types().That().ResideInNamespaceMatching(@"^MenuX\.SharedKernel(\..*)?$")
            .Should().NotDependOnAny(
                Types().That().ResideInNamespaceMatching(@"^MenuX\.Infrastructure(\..*)?$"))
            .Because("SharedKernel holds value types only (doc 05 §3 rule 5)")
            .Check(Architecture);
    }

    [Fact]
    public void Modules_do_not_depend_on_hosts()
    {
        Types().That().ResideInNamespaceMatching(
                $@"^MenuX\.({string.Join('|', Modules.Names)})(\..*)?$")
            .Should().NotDependOnAny(
                Types().That().ResideInNamespaceMatching(@"^MenuX\.Host(\..*)?$"))
            .Check(Architecture);
    }

    [Theory]
    [MemberData(nameof(Modules.All), MemberType = typeof(Modules))]
    public void Every_module_is_loaded_with_its_contracts(string module)
    {
        // Guards against the rules above passing vacuously because a module was not loaded.
        Assert.Contains(Architecture.Types, t => t.Namespace.FullName == $"MenuX.{module}");
        Assert.Contains(Architecture.Types, t => t.Namespace.FullName == $"MenuX.{module}.Contracts");
    }
}
