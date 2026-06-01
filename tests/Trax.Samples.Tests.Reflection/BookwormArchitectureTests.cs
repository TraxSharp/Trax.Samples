using Trax.Api.GraphQL.Testing;
using Trax.Effect.Data.Testing;
using Trax.Mediator.Testing;
using Trax.Samples.Bookworm.Catalog.Context;
using Trax.Samples.Bookworm.CrossSchema;
using Trax.Samples.Bookworm.Lending.Context;

namespace Trax.Samples.Tests.Reflection;

/// <summary>
/// Reflection-based architecture guards for the Bookworm flagship, run by consuming the framework
/// guard packages (Trax.Effect.Data.Testing / Trax.Api.GraphQL.Testing / Trax.Mediator.Testing). Each
/// test is a thin wrapper that calls a packaged checker and asserts no offenders; the rule logic lives
/// in the packages, so any consumer adopts these guards the same way.
/// </summary>
[TestFixture]
public class BookwormArchitectureTests
{
    [Test]
    public void EveryDomainContext_OwnsADistinctSchema()
    {
        var result = DataLayerGuards.OneSchemaPerContext([
            typeof(CatalogDbContext),
            typeof(LendingDbContext),
        ]);

        result.Offenders.Should().BeEmpty(result.FailureMessage);
    }

    [Test]
    public void CrossSchemaEdgeManifest_IsValid()
    {
        var result = CrossSchemaGuards.EdgeManifestIsValid(CrossSchemaEdges.All);

        result.Offenders.Should().BeEmpty(result.FailureMessage);
        result.Inspected.Should().BeGreaterThan(0, "Bookworm declares at least the loan.book edge");
    }

    [Test]
    public void EveryTrain_HasACompanionInterface()
    {
        var result = TrainGuards.EveryTrainHasInterface([
            typeof(Trax.Samples.Bookworm.AssemblyMarker).Assembly,
        ]);

        result.Offenders.Should().BeEmpty(result.FailureMessage);
        result.Inspected.Should().BeGreaterThan(0, "Bookworm defines trains");
    }
}
