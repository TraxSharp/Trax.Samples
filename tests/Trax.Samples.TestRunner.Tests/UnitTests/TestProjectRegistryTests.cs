using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Trax.Samples.TestRunner.Services;

namespace Trax.Samples.TestRunner.Tests.UnitTests;

[TestFixture]
public class TestProjectRegistryTests
{
    #region Project Discovery

    [Test]
    public void Projects_ExcludesArrayLoggerProjects()
    {
        var registry = CreateRegistry();

        registry.Projects.Should().NotContain(p => p.Name.EndsWith("ArrayLogger"));
    }

    [Test]
    public void Projects_ExcludesBenchmarkProjects()
    {
        var registry = CreateRegistry();

        registry.Projects.Should().NotContain(p => p.Name.EndsWith("Benchmarks"));
    }

    [Test]
    public void Projects_FlagsPostgresProjectsCorrectly()
    {
        var registry = CreateRegistry();
        var postgresProjects = registry.Projects.Where(p => p.RequiresPostgres).ToList();

        postgresProjects
            .Select(p => p.Name)
            .Should()
            .BeSubsetOf(
                new[]
                {
                    "Trax.Effect.Tests.Integration",
                    "Trax.Mediator.Tests.Postgres.Integration",
                    "Trax.Scheduler.Tests.Integration",
                    "Trax.Scheduler.Tests.Stress",
                }
            );
    }

    [Test]
    public void Projects_OnlyIncludesNUnitProjects()
    {
        var registry = CreateRegistry();

        // All discovered projects should have names containing "Tests"
        registry.Projects.Should().OnlyContain(p => p.Name.Contains("Tests"));
    }

    [Test]
    public void Projects_SetsRepoNameFromDirectoryStructure()
    {
        var registry = CreateRegistry();

        var coreTests = registry.Projects.Where(p => p.RepoName == "Trax.Core").ToList();
        coreTests.Should().NotBeEmpty();
        coreTests.Should().OnlyContain(p => p.Name.StartsWith("Trax.Core.Tests"));
    }

    [Test]
    public void Projects_AreSortedByRepoThenName()
    {
        var registry = CreateRegistry();

        var names = registry.Projects.Select(p => $"{p.RepoName}/{p.Name}").ToList();
        names.Should().BeInAscendingOrder();
    }

    #endregion

    #region Empty Directory

    [Test]
    public void Projects_WithEmptyRoot_ReturnsEmptyList()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?> { ["TestRunner:MonorepoRoot"] = tempDir }
                )
                .Build();

            var registry = new TestProjectRegistry(
                config,
                NullLogger<TestProjectRegistry>.Instance
            );

            registry.Projects.Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    #endregion

    #region Helpers

    private static TestProjectRegistry CreateRegistry()
    {
        // Point at the actual Trax monorepo root for integration-style testing
        var monorepoRoot = FindMonorepoRoot();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["TestRunner:MonorepoRoot"] = monorepoRoot }
            )
            .Build();

        return new TestProjectRegistry(config, NullLogger<TestProjectRegistry>.Instance);
    }

    private static string FindMonorepoRoot()
    {
        // Walk up from the test project to find the Trax workspace root
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "pack-local.sh")))
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new InvalidOperationException(
            "Could not find Trax monorepo root (looking for pack-local.sh)"
        );
    }

    #endregion
}
