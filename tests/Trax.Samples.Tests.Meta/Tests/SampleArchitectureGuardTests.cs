using Trax.Api.GraphQL.Testing;
using Trax.Core.Testing;
using Trax.Effect.Data.Testing;

namespace Trax.Samples.Tests.Meta.Tests;

/// <summary>
/// Source-scanning architecture guards for the samples, run by consuming the framework guard packages
/// (Trax.Effect.Data.Testing / Trax.Api.GraphQL.Testing). Thin wrappers: the rule logic lives in the
/// packages, this only supplies sample-specific options. Any repo adopts the guards the same way.
/// </summary>
[TestFixture]
public class SampleArchitectureGuardTests
{
    private static ArchitectureGuardOptions SampleOptions =>
        new() { SourceScanRoots = ["samples"] };

    [Test]
    public void EveryDomainContext_DerivesTheSharedBase()
    {
        var result = DataLayerGuards.DomainContextsDeriveBase(SampleOptions);

        result.Offenders.Should().BeEmpty(result.FailureMessage);
        result
            .Inspected.Should()
            .BeGreaterThan(0, "the guard must find domain contexts to inspect");
    }

    [Test]
    public void EverySharedBaseContext_HasACompanionInterface()
    {
        var result = DataLayerGuards.CompanionInterfaces(SampleOptions);

        result.Offenders.Should().BeEmpty(result.FailureMessage);
        result.Inspected.Should().BeGreaterThan(0);
    }

    [Test]
    public void CrossSchemaEdgeResolvers_UseTheBatchedLoader()
    {
        var result = CrossSchemaGuards.EdgeResolversUseLoader(SampleOptions);

        result.Offenders.Should().BeEmpty(result.FailureMessage);
        result
            .Inspected.Should()
            .BeGreaterThan(0, "Bookworm has at least one cross-schema edge resolver");
    }
}
