namespace Trax.Samples.Tests.Meta.Tests;

/// <summary>
/// Source guards for the cross-schema patterns:
/// <list type="bullet">
///   <item>Every <c>OnCrossSchemaModelCreating</c> definition has the standard signature.</item>
///   <item>Cross-schema GraphQL edge resolvers (the ones that inject a <c>CrossSchemaLoader</c>)
///   live only in a <c>*.CrossSchema</c> project.</item>
///   <item>Every <c>[ExtendObjectType]</c> resolver in a <c>*.CrossSchema</c> project goes through
///   the batched loader, so a cross-schema field can never become a hidden N+1.</item>
/// </list>
/// </summary>
[TestFixture]
public class CrossSchemaConventionTests
{
    private static readonly Regex DefinitionSignature = new(
        @"public\s+static\s+void\s+OnCrossSchemaModelCreating\s*\(\s*ModelBuilder\s+\w+\s*,\s*string\s+\w+\s*\)",
        RegexOptions.Compiled
    );
    private static readonly Regex AnyDefinition = new(
        @"\bvoid\s+OnCrossSchemaModelCreating\b",
        RegexOptions.Compiled
    );
    private static readonly Regex UsesLoader = new(@"\bCrossSchemaLoader<", RegexOptions.Compiled);
    private static readonly Regex ExtendObjectType = new(
        @"\[\s*ExtendObjectType",
        RegexOptions.Compiled
    );

    private static bool InCrossSchemaProject(string repoRelativePath) =>
        repoRelativePath.Contains(".CrossSchema/", StringComparison.Ordinal);

    [Test]
    public void EveryOnCrossSchemaModelCreating_HasTheStandardSignature()
    {
        var offenders = new List<string>();
        var inspected = 0;

        foreach (var file in SourceFiles.CSharp("samples"))
        {
            var stripped = SourceText.StripCommentsAndStrings(File.ReadAllText(file));
            if (!AnyDefinition.IsMatch(stripped))
                continue;

            inspected++;
            if (!DefinitionSignature.IsMatch(stripped))
                offenders.Add(RepoRoot.Relative(file).Replace('\\', '/'));
        }

        inspected
            .Should()
            .BeGreaterThan(0, "the guard must find at least one cross-schema entity to inspect");

        offenders
            .Should()
            .BeEmpty(
                "OnCrossSchemaModelCreating must be declared exactly as "
                    + "`public static void OnCrossSchemaModelCreating(ModelBuilder builder, string schema)` "
                    + "so consuming contexts can call it uniformly. Offenders:\n  "
                    + string.Join("\n  ", offenders)
            );
    }

    [Test]
    public void CrossSchemaEdgeResolvers_LiveOnlyInTheCrossSchemaProject()
    {
        var offenders = new List<string>();
        var found = 0;

        foreach (var file in SourceFiles.CSharp("samples"))
        {
            var stripped = SourceText.StripCommentsAndStrings(File.ReadAllText(file));
            if (!ExtendObjectType.IsMatch(stripped) || !UsesLoader.IsMatch(stripped))
                continue;

            found++;
            var rel = RepoRoot.Relative(file).Replace('\\', '/');
            if (!InCrossSchemaProject(rel))
                offenders.Add(rel);
        }

        found
            .Should()
            .BeGreaterThan(0, "the guard must find at least one cross-schema edge resolver");

        offenders
            .Should()
            .BeEmpty(
                "A cross-schema edge resolver (an [ExtendObjectType] that injects a CrossSchemaLoader) "
                    + "must live in a *.CrossSchema project, the one place allowed to reference more "
                    + "than one domain. Move it there. Offenders:\n  "
                    + string.Join("\n  ", offenders)
            );
    }

    [Test]
    public void EveryCrossSchemaEdgeResolver_UsesTheBatchedLoader()
    {
        var offenders = new List<string>();
        var found = 0;

        foreach (var file in SourceFiles.CSharp("samples"))
        {
            var rel = RepoRoot.Relative(file).Replace('\\', '/');
            if (!InCrossSchemaProject(rel))
                continue;

            var stripped = SourceText.StripCommentsAndStrings(File.ReadAllText(file));
            if (!ExtendObjectType.IsMatch(stripped))
                continue;

            found++;
            if (!UsesLoader.IsMatch(stripped))
                offenders.Add(rel);
        }

        found.Should().BeGreaterThan(0, "the guard must find at least one edge resolver");

        offenders
            .Should()
            .BeEmpty(
                "Every [ExtendObjectType] resolver in a *.CrossSchema project must resolve through a "
                    + "CrossSchemaLoader<>, never an ad-hoc DbContext query, so the field is batched "
                    + "and can never be a per-row N+1. Offenders:\n  "
                    + string.Join("\n  ", offenders)
            );
    }
}
