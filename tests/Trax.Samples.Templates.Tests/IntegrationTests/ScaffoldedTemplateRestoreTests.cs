using AwesomeAssertions;
using static Trax.Samples.Templates.Tests.Utils.Dotnet;

namespace Trax.Samples.Templates.Tests.IntegrationTests;

/// <summary>
/// A project scaffolded from the packed <c>Trax.Samples.Templates</c> restores on its own,
/// outside this repository. Inside the repo the template projects take their package versions
/// from the root <c>Directory.Packages.props</c>, which a scaffolded project never sees, so the
/// package has to carry the versions with it.
///
/// <para>Enforces <c>docs/adr/0004-the-template-package-carries-its-package-versions.md</c>.</para>
/// </summary>
[TestFixture]
[Property("adr", "docs/adr/0004-the-template-package-carries-its-package-versions.md")]
public class ScaffoldedTemplateRestoreTests
{
    private const string Adr =
        "see docs/adr/0004-the-template-package-carries-its-package-versions.md";

    private string _workDir = null!;
    private string _hive = null!;

    [OneTimeSetUp]
    public async Task PackAndInstallTheTemplates()
    {
        // A directory with no Directory.Packages.props, Directory.Build.props or nuget.config
        // above it, which is where a consumer runs `dotnet new`.
        _workDir = Path.Combine(Path.GetTempPath(), "trax-template-restore-" + Guid.NewGuid());
        _hive = Path.Combine(_workDir, "hive");
        var nupkgDir = Path.Combine(_workDir, "nupkg");
        Directory.CreateDirectory(_workDir);

        var templatesProject = Path.Combine(
            RepoRoot(),
            "templates",
            "Trax.Samples.Templates.csproj"
        );

        // An empty TraxLocalVersion packs the committed pins even in a workspace checkout
        // where trax-local.props points every Trax package at the local feed.
        await Run(
            _workDir,
            "pack",
            templatesProject,
            "--output",
            nupkgDir,
            "-p:Version=0.0.0-scaffold-test",
            "-p:TraxLocalVersion="
        );

        var nupkg = Directory.GetFiles(nupkgDir, "*.nupkg").Single();
        await Run(_workDir, "new", "install", nupkg, "--debug:custom-hive", _hive);
    }

    [OneTimeTearDown]
    public void DeleteTheWorkDirectory()
    {
        if (Directory.Exists(_workDir))
            Directory.Delete(_workDir, recursive: true);
    }

    [TestCase("trax-hub")]
    [TestCase("trax-scheduler")]
    [TestCase("trax-api")]
    public async Task A_scaffolded_project_restores_outside_the_repo(string shortName)
    {
        var output = Path.Combine(_workDir, shortName);
        await Run(
            _workDir,
            "new",
            shortName,
            "--name",
            "Scaffolded",
            "--output",
            output,
            "--debug:custom-hive",
            _hive
        );

        var restore = await Run(output, allowFailure: true, "restore");

        restore
            .ExitCode.Should()
            .Be(0, $"a scaffolded {shortName} must restore on its own ({Adr}):\n{restore.Output}");
    }
}
