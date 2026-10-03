namespace Trax.Samples.Tests.Meta.Tests;

/// <summary>
/// A AwesomeAssertions async assertion (<c>ThrowAsync</c>, <c>NotThrowAsync</c>,
/// <c>CompleteWithinAsync</c>, ...) returns a Task that carries the verdict. Called without
/// <c>await</c> from a <c>void</c> test, the Task is dropped, the assertion never runs, and the
/// test passes whatever the code under test does.
///
/// <para>Not ADR-enforcing: it checks how an unawaited Task behaves, not a choice between
/// alternatives.</para>
/// </summary>
[TestFixture]
public class AsyncAssertionsAreAwaitedTests
{
    private static readonly Regex AsyncAssertion = new(
        @"\.Should\(\)\s*\.\s*(ThrowAsync|ThrowExactlyAsync|NotThrowAsync|NotThrowAfterAsync|CompleteWithinAsync|NotCompleteWithinAsync)\b",
        RegexOptions.Compiled
    );

    private static readonly Regex AwaitedOrReturned = new(
        @"\b(await|return)\b",
        RegexOptions.Compiled
    );

    [Test]
    public void Every_async_assertion_in_the_tests_is_awaited()
    {
        var offenders = new List<string>();

        foreach (var file in SourceFiles.CSharp("tests"))
        {
            if (file.EndsWith("AsyncAssertionsAreAwaitedTests.cs", StringComparison.Ordinal))
                continue;

            var stripped = SourceText.StripCommentsAndStrings(File.ReadAllText(file));
            foreach (var (line, text) in SourceText.MatchingLines(stripped, AsyncAssertion))
            {
                var beforeAssertion = text[..AsyncAssertion.Match(text).Index];
                if (!AwaitedOrReturned.IsMatch(beforeAssertion))
                    offenders.Add($"{RepoRoot.Relative(file)}:{line}");
            }
        }

        offenders
            .Should()
            .BeEmpty(
                "an async assertion that is not awaited is discarded, so the test passes even when "
                    + "the code under test does not throw. Make the test async Task and await it. "
                    + "Offenders:\n  "
                    + string.Join("\n  ", offenders)
            );
    }
}
