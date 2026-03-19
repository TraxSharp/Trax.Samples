using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Trax.Samples.TestRunner.Models;

namespace Trax.Samples.TestRunner.Services;

public class TestProjectRegistry
{
    private static readonly HashSet<string> PostgresProjects =
    [
        "Trax.Effect.Tests.Integration",
        "Trax.Mediator.Tests.Postgres.Integration",
        "Trax.Scheduler.Tests.Integration",
        "Trax.Scheduler.Tests.Stress",
    ];

    private static readonly HashSet<string> ExcludedSuffixes = ["ArrayLogger", "Benchmarks"];

    private readonly Lazy<IReadOnlyList<TestProject>> _projects;

    public TestProjectRegistry(IConfiguration configuration, ILogger<TestProjectRegistry> logger)
    {
        var monorepoRoot =
            configuration["TestRunner:MonorepoRoot"]
            ?? throw new InvalidOperationException(
                "TestRunner:MonorepoRoot configuration is required."
            );

        var resolvedRoot = Path.GetFullPath(monorepoRoot);
        logger.LogInformation("TestProjectRegistry scanning monorepo at {Root}", resolvedRoot);

        _projects = new Lazy<IReadOnlyList<TestProject>>(() => ScanProjects(resolvedRoot, logger));
    }

    public IReadOnlyList<TestProject> Projects => _projects.Value;

    private static List<TestProject> ScanProjects(string root, ILogger logger)
    {
        var projects = new List<TestProject>();

        foreach (var repoDir in Directory.GetDirectories(root))
        {
            var repoName = Path.GetFileName(repoDir);
            var testsDir = Path.Combine(repoDir, "tests");

            if (!Directory.Exists(testsDir))
                continue;

            foreach (var projectDir in Directory.GetDirectories(testsDir))
            {
                var projectName = Path.GetFileName(projectDir);

                if (ExcludedSuffixes.Any(suffix => projectName.EndsWith(suffix)))
                    continue;

                var csprojPath = Path.Combine(projectDir, $"{projectName}.csproj");
                if (!File.Exists(csprojPath))
                    continue;

                if (!IsNUnitProject(csprojPath))
                    continue;

                projects.Add(
                    new TestProject
                    {
                        Name = projectName,
                        ProjectPath = csprojPath,
                        RepoName = repoName,
                        RequiresPostgres = PostgresProjects.Contains(projectName),
                    }
                );

                logger.LogDebug("Discovered test project {Name} in {Repo}", projectName, repoName);
            }
        }

        logger.LogInformation("Discovered {Count} test projects", projects.Count);
        return projects.OrderBy(p => p.RepoName).ThenBy(p => p.Name).ToList();
    }

    private static bool IsNUnitProject(string csprojPath)
    {
        try
        {
            var doc = XDocument.Load(csprojPath);
            return doc.Descendants("PackageReference")
                .Any(e =>
                    string.Equals(
                        e.Attribute("Include")?.Value,
                        "NUnit",
                        StringComparison.OrdinalIgnoreCase
                    )
                );
        }
        catch
        {
            return false;
        }
    }
}
