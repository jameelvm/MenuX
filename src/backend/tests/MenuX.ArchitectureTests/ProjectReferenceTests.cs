using System.Xml.Linq;

namespace MenuX.ArchitectureTests;

/// <summary>
/// Project-level boundary rules, written as allowlists per project kind (doc 05 §2–§3).
/// They catch a forbidden reference as soon as it is added to a .csproj, before any type is used.
/// Every project under src/ must be one of the known kinds, so a new project cannot escape the rules.
/// </summary>
public sealed class ProjectReferenceTests
{
    private const string SharedKernel = "MenuX.SharedKernel";
    private const string Infrastructure = "MenuX.Infrastructure";

    private static readonly string BackendRoot = FindBackendRoot();
    private static readonly string SourceRoot = Path.Combine(BackendRoot, "src");
    private static readonly IReadOnlyList<Project> Projects = DiscoverProjects();

    public static TheoryData<string> AllProjectNames()
    {
        var data = new TheoryData<string>();
        foreach (var p in Projects)
        {
            data.Add(p.Name);
        }

        return data;
    }

    // (a)
    [Theory]
    [MemberData(nameof(AllProjectNames))]
    public void Every_project_is_a_known_kind(string project)
    {
        var p = Projects.Single(x => x.Name == project);
        Assert.True(p.Kind != ProjectKind.Unknown,
            $"{p.RelativePath} is not a host, building block, module or Contracts project. " +
            "Add it to the architecture rules before adding it to the solution.");
    }

    // (b)
    [Fact]
    public void Module_list_matches_module_folders()
    {
        var folders = Directory.GetDirectories(Path.Combine(SourceRoot, "Modules"))
            .Select(d => Path.GetFileName(d)!)
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.Equal(Modules.Names.Order(StringComparer.Ordinal).ToList(), folders);
    }

    [Theory]
    [MemberData(nameof(Modules.All), MemberType = typeof(Modules))]
    public void Each_module_has_exactly_a_contracts_project_and_an_internal_project(string module)
    {
        var inFolder = Projects.Where(p => p.Module == module).Select(p => p.Name).Order(StringComparer.Ordinal);
        Assert.Equal([$"MenuX.{module}", $"MenuX.{module}.Contracts"], inFolder);
    }

    // (c)
    [Theory]
    [MemberData(nameof(Modules.All), MemberType = typeof(Modules))]
    public void Module_references_only_its_contracts_building_blocks_and_other_contracts(string module)
    {
        var allowed = new HashSet<string>(OtherContracts(module), StringComparer.Ordinal)
        {
            $"MenuX.{module}.Contracts",
            SharedKernel,
            Infrastructure,
        };

        AssertOnlyAllowed(Get($"MenuX.{module}"), allowed);
    }

    // (d)
    [Theory]
    [MemberData(nameof(Modules.All), MemberType = typeof(Modules))]
    public void Contracts_reference_only_shared_kernel_and_other_contracts(string module)
    {
        var allowed = new HashSet<string>(OtherContracts(module), StringComparer.Ordinal) { SharedKernel };
        AssertOnlyAllowed(Get($"MenuX.{module}.Contracts"), allowed);
    }

    // (e)
    [Fact]
    public void Shared_kernel_references_no_projects() =>
        AssertOnlyAllowed(Get(SharedKernel), []);

    [Fact]
    public void Infrastructure_references_only_shared_kernel() =>
        AssertOnlyAllowed(Get(Infrastructure), [SharedKernel]);

    // (f)
    [Theory]
    [InlineData("MenuX.Host.Api")]
    [InlineData("MenuX.Host.Worker")]
    public void Host_references_only_modules_and_building_blocks(string host)
    {
        var allowed = new HashSet<string>(StringComparer.Ordinal) { SharedKernel, Infrastructure };
        foreach (var m in Modules.Names)
        {
            allowed.Add($"MenuX.{m}");
            allowed.Add($"MenuX.{m}.Contracts");
        }

        AssertOnlyAllowed(Get(host), allowed);
    }

    // (g)
    [Theory]
    [MemberData(nameof(PackageFreeProjects))]
    public void Contracts_and_shared_kernel_have_no_package_or_framework_references(string project)
    {
        var p = Get(project);
        Assert.Empty(p.PackageReferences);
        Assert.Empty(p.FrameworkReferences);
    }

    public static TheoryData<string> PackageFreeProjects()
    {
        var data = new TheoryData<string> { SharedKernel };
        foreach (var m in Modules.Names)
        {
            data.Add($"MenuX.{m}.Contracts");
        }

        return data;
    }

    private static IEnumerable<string> OtherContracts(string module) =>
        Modules.Names.Where(n => n != module).Select(n => $"MenuX.{n}.Contracts");

    private static void AssertOnlyAllowed(Project project, IEnumerable<string> allowed)
    {
        var allowedSet = allowed.ToHashSet(StringComparer.Ordinal);
        var forbidden = project.ProjectReferences.Where(r => !allowedSet.Contains(r)).ToList();
        Assert.True(forbidden.Count == 0,
            $"{project.Name} has references outside its allowlist: {string.Join(", ", forbidden)}");
    }

    private static Project Get(string name)
    {
        var p = Projects.SingleOrDefault(x => x.Name == name);
        Assert.NotNull(p);
        return p;
    }

    private static List<Project> DiscoverProjects() =>
        Directory.EnumerateFiles(SourceRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(f => !f.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj"))
            .Select(Project.Load)
            .ToList();

    private static string FindBackendRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "global.json")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("global.json not found above the test output folder.");
    }

    private enum ProjectKind
    {
        Unknown,
        Host,
        SharedKernel,
        Infrastructure,
        Module,
        Contracts,
    }

    private sealed record Project(
        string Name,
        string RelativePath,
        ProjectKind Kind,
        string? Module,
        IReadOnlyList<string> ProjectReferences,
        IReadOnlyList<string> PackageReferences,
        IReadOnlyList<string> FrameworkReferences)
    {
        public static Project Load(string path)
        {
            var name = Path.GetFileNameWithoutExtension(path);
            var relative = Path.GetRelativePath(SourceRoot, path).Replace('\\', '/');
            var segments = relative.Split('/');
            var doc = XDocument.Load(path);

            var (kind, module) = segments switch
            {
                [var folder, _] when folder.StartsWith("Host.", StringComparison.Ordinal)
                    && name == $"MenuX.{folder}" => (ProjectKind.Host, (string?)null),
                ["BuildingBlocks", SharedKernel, _] when name == SharedKernel => (ProjectKind.SharedKernel, null),
                ["BuildingBlocks", Infrastructure, _] when name == Infrastructure => (ProjectKind.Infrastructure, null),
                ["Modules", var m, var folder, _] when folder == name && name == $"MenuX.{m}" => (ProjectKind.Module, m),
                ["Modules", var m, var folder, _] when folder == name && name == $"MenuX.{m}.Contracts" => (ProjectKind.Contracts, m),
                ["Modules", var m, ..] => (ProjectKind.Unknown, m),
                _ => (ProjectKind.Unknown, null),
            };

            return new Project(
                name,
                relative,
                kind,
                module,
                Includes(doc, "ProjectReference").Select(i => Path.GetFileNameWithoutExtension(i.Replace('\\', '/'))).ToList(),
                Includes(doc, "PackageReference").ToList(),
                Includes(doc, "FrameworkReference").ToList());
        }

        private static IEnumerable<string> Includes(XDocument doc, string element) =>
            doc.Descendants(element).Select(e => (string?)e.Attribute("Include") ?? string.Empty);
    }
}
