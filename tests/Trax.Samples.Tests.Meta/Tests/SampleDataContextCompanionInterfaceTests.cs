namespace Trax.Samples.Tests.Meta.Tests;

/// <summary>
/// Every context that derives the shared base must ship a companion <c>I{Name}</c> interface in the
/// same directory. Application code (junctions, services) depends on the interface, never the
/// concrete context, and the interface is what lets cross-schema entities be exposed via explicit
/// interface implementation (hidden from GraphQL discovery).
/// </summary>
[TestFixture]
public class SampleDataContextCompanionInterfaceTests
{
    private static readonly Regex SharedBaseContextClass = new(
        @"\bclass\s+(\w+DbContext)\s*\([^)]*\)\s*:\s*SampleDataContext<",
        RegexOptions.Compiled | RegexOptions.Singleline
    );

    [Test]
    public void EverySharedBaseContext_HasACompanionInterface_InTheSameDirectory()
    {
        var offenders = new List<string>();
        var inspected = 0;

        foreach (var file in SourceFiles.CSharp("samples"))
        {
            var stripped = SourceText.StripCommentsAndStrings(File.ReadAllText(file));
            var match = SharedBaseContextClass.Match(stripped);
            if (!match.Success)
                continue;

            inspected++;
            var contextName = match.Groups[1].Value;
            var directory = Path.GetDirectoryName(file)!;
            var companion = Path.Combine(directory, $"I{contextName}.cs");

            if (!File.Exists(companion))
                offenders.Add(
                    $"{RepoRoot.Relative(file).Replace('\\', '/')} (expected I{contextName}.cs alongside it)"
                );
        }

        inspected.Should().BeGreaterThan(0, "the guard must find shared-base contexts to inspect");

        offenders
            .Should()
            .BeEmpty(
                "Every SampleDataContext-derived context needs a companion I{Name} interface in the "
                    + "same directory, declaring its DbSets (and cross-schema reads). Offenders:\n  "
                    + string.Join("\n  ", offenders)
            );
    }
}
