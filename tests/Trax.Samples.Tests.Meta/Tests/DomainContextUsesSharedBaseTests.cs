namespace Trax.Samples.Tests.Meta.Tests;

/// <summary>
/// Every domain data context (a <c>*DbContext</c> class under samples/) must derive the shared
/// <c>SampleDataContext&lt;TSelf&gt;</c> base, which enforces the one-project-one-schema-one-context
/// rule, schema isolation, and UTC datetime handling. Samples that have not yet been migrated are
/// listed in <see cref="KnownExceptions"/> with a justification; the test fails if an allowlisted
/// file no longer exists or has actually been migrated (a stale exception).
/// </summary>
[TestFixture]
public class DomainContextUsesSharedBaseTests
{
    private static readonly Regex ContextClass = new(
        @"\bclass\s+(\w+DbContext)\b",
        RegexOptions.Compiled
    );
    private static readonly Regex InheritsSharedBase = new(
        @":\s*SampleDataContext<",
        RegexOptions.Compiled
    );

    /// <summary>
    /// Sample contexts allowed to skip the shared base. Empty: every data-bearing sample has been
    /// migrated onto SampleDataContext. Add an entry (with a justification) only if a sample
    /// genuinely cannot derive the base; the anti-stale test fails if an entry stops being needed.
    /// </summary>
    private static readonly HashSet<string> KnownExceptions = new(StringComparer.Ordinal);

    [Test]
    public void EveryDomainContext_DerivesTheSharedBase()
    {
        var offenders = new List<string>();
        var inspected = 0;

        foreach (var file in SourceFiles.CSharp("samples"))
        {
            var content = File.ReadAllText(file);
            var stripped = SourceText.StripCommentsAndStrings(content);
            if (!ContextClass.IsMatch(stripped))
                continue;

            inspected++;
            var rel = RepoRoot.Relative(file).Replace('\\', '/');
            if (KnownExceptions.Contains(rel))
                continue;

            if (!InheritsSharedBase.IsMatch(stripped))
                offenders.Add(rel);
        }

        inspected
            .Should()
            .BeGreaterThan(0, "the guard must actually find domain contexts to inspect");

        offenders
            .Should()
            .BeEmpty(
                "Every domain *DbContext must derive SampleDataContext<TSelf> (one project : one "
                    + "schema : one context). Add `: SampleDataContext<YourDbContext>` and implement "
                    + "the Schema property and ConfigureModel(...). If a sample legitimately cannot "
                    + "migrate yet, add it to KnownExceptions with a justification. Offenders:\n  "
                    + string.Join("\n  ", offenders)
            );
    }

    [Test]
    public void KnownExceptions_AreNotStale()
    {
        var stale = new List<string>();

        foreach (var rel in KnownExceptions)
        {
            var absolute = RepoRoot.Combine(rel.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(absolute))
            {
                stale.Add($"{rel} (file no longer exists)");
                continue;
            }

            var stripped = SourceText.StripCommentsAndStrings(File.ReadAllText(absolute));
            if (InheritsSharedBase.IsMatch(stripped))
                stale.Add($"{rel} (now derives the shared base — migration done)");
        }

        stale
            .Should()
            .BeEmpty(
                "A KnownExceptions entry is stale. Remove these entries so the allowlist reflects "
                    + "reality:\n  "
                    + string.Join("\n  ", stale)
            );
    }
}
