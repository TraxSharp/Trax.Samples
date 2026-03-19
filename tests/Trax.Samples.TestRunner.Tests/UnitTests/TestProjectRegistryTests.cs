using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Trax.Samples.TestRunner.Services;

namespace Trax.Samples.TestRunner.Tests.UnitTests;

[TestFixture]
public class TestProjectRegistryTests
{
    private string _tempDir = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "TestProjectRegistry_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    #region Project Discovery

    [Test]
    public void Projects_ExcludesArrayLoggerProjects()
    {
        CreateFakeProject("Trax.Core", "Trax.Core.Tests.Unit", nunit: true);
        CreateFakeProject("Trax.Core", "Trax.Core.Tests.ArrayLogger", nunit: true);

        var registry = CreateRegistry();

        registry.Projects.Should().NotContain(p => p.Name.EndsWith("ArrayLogger"));
        registry.Projects.Should().HaveCount(1);
    }

    [Test]
    public void Projects_ExcludesBenchmarkProjects()
    {
        CreateFakeProject("Trax.Core", "Trax.Core.Tests.Unit", nunit: true);
        CreateFakeProject("Trax.Core", "Trax.Core.Benchmarks", nunit: true);

        var registry = CreateRegistry();

        registry.Projects.Should().NotContain(p => p.Name.EndsWith("Benchmarks"));
        registry.Projects.Should().HaveCount(1);
    }

    [Test]
    public void Projects_FlagsPostgresProjectsCorrectly()
    {
        CreateFakeProject("Trax.Effect", "Trax.Effect.Tests.Integration", nunit: true);
        CreateFakeProject("Trax.Effect", "Trax.Effect.Tests.Unit", nunit: true);

        var registry = CreateRegistry();

        var postgresProject = registry.Projects.Single(p =>
            p.Name == "Trax.Effect.Tests.Integration"
        );
        postgresProject.RequiresPostgres.Should().BeTrue();

        var unitProject = registry.Projects.Single(p => p.Name == "Trax.Effect.Tests.Unit");
        unitProject.RequiresPostgres.Should().BeFalse();
    }

    [Test]
    public void Projects_OnlyIncludesNUnitProjects()
    {
        CreateFakeProject("Trax.Core", "Trax.Core.Tests.Unit", nunit: true);
        CreateFakeProject("Trax.Core", "Trax.Core.Tests.Other", nunit: false);

        var registry = CreateRegistry();

        registry.Projects.Should().HaveCount(1);
        registry.Projects[0].Name.Should().Be("Trax.Core.Tests.Unit");
    }

    [Test]
    public void Projects_SetsRepoNameFromDirectoryStructure()
    {
        CreateFakeProject("Trax.Core", "Trax.Core.Tests.Unit", nunit: true);
        CreateFakeProject("Trax.Effect", "Trax.Effect.Tests.Unit", nunit: true);

        var registry = CreateRegistry();

        var coreProject = registry.Projects.Single(p => p.Name == "Trax.Core.Tests.Unit");
        coreProject.RepoName.Should().Be("Trax.Core");

        var effectProject = registry.Projects.Single(p => p.Name == "Trax.Effect.Tests.Unit");
        effectProject.RepoName.Should().Be("Trax.Effect");
    }

    [Test]
    public void Projects_AreSortedByRepoThenName()
    {
        CreateFakeProject("Trax.Effect", "Trax.Effect.Tests.Unit", nunit: true);
        CreateFakeProject("Trax.Core", "Trax.Core.Tests.Unit", nunit: true);
        CreateFakeProject("Trax.Core", "Trax.Core.Tests.Integration", nunit: true);

        var registry = CreateRegistry();

        var names = registry.Projects.Select(p => $"{p.RepoName}/{p.Name}").ToList();
        names.Should().BeInAscendingOrder();
    }

    #endregion

    #region Empty Directory

    [Test]
    public void Projects_WithEmptyRoot_ReturnsEmptyList()
    {
        var registry = CreateRegistry();

        registry.Projects.Should().BeEmpty();
    }

    #endregion

    #region Helpers

    private TestProjectRegistry CreateRegistry()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["TestRunner:MonorepoRoot"] = _tempDir }
            )
            .Build();

        return new TestProjectRegistry(config, NullLogger<TestProjectRegistry>.Instance);
    }

    private void CreateFakeProject(string repoName, string projectName, bool nunit)
    {
        var projectDir = Path.Combine(_tempDir, repoName, "tests", projectName);
        Directory.CreateDirectory(projectDir);

        var nunitRef = nunit
            ? """<PackageReference Include="NUnit" Version="4.*" />"""
            : """<PackageReference Include="xunit" Version="2.*" />""";

        var csproj = $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                {nunitRef}
              </ItemGroup>
            </Project>
            """;

        File.WriteAllText(Path.Combine(projectDir, $"{projectName}.csproj"), csproj);
    }

    #endregion
}
