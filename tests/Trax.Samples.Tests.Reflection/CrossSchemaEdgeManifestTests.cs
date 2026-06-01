using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Trax.Samples.Bookworm.CrossSchema;
using Trax.Samples.Bookworm.CrossSchema.Extensions;
using Trax.Samples.Shared.Api.CrossSchema;

namespace Trax.Samples.Tests.Reflection;

/// <summary>
/// Pins Bookworm's cross-schema edge manifest (<see cref="CrossSchemaEdges.All"/>) against reality:
/// every declared edge must have a real integer foreign key on its source, a target owned by the
/// declared context, a camelCase field name, and a registered batched loader. Adding an edge without
/// the matching FK / target / loader fails the build.
/// </summary>
[TestFixture]
public class CrossSchemaEdgeManifestTests
{
    private static readonly Regex CamelCase = new("^[a-z][A-Za-z0-9]*$", RegexOptions.Compiled);

    [Test]
    public void Manifest_IsNotEmpty()
    {
        CrossSchemaEdges.All.Should().NotBeEmpty("the guard is meaningless with no edges to check");
    }

    [Test]
    public void EveryEdge_HasARealIntegerForeignKey_OnItsSource()
    {
        foreach (var edge in CrossSchemaEdges.All)
        {
            var fk = edge.Source.GetProperty(edge.Fk, BindingFlags.Public | BindingFlags.Instance);

            fk.Should().NotBeNull($"{edge.Source.Name}.{edge.Fk} must exist");

            var type = Nullable.GetUnderlyingType(fk!.PropertyType) ?? fk.PropertyType;
            type.Should()
                .Be(typeof(int), $"{edge.Source.Name}.{edge.Fk} must be an int foreign key");
        }
    }

    [Test]
    public void EveryEdge_Target_IsADbSetOnItsDeclaredTargetContext()
    {
        foreach (var edge in CrossSchemaEdges.All)
        {
            var ownsTarget = edge
                .TargetContext.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Any(p =>
                    p.PropertyType.IsGenericType
                    && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>)
                    && p.PropertyType.GetGenericArguments()[0] == edge.Target
                );

            ownsTarget
                .Should()
                .BeTrue(
                    $"{edge.TargetContext.Name} must expose DbSet<{edge.Target.Name}>; it is the "
                        + "declared owner of the edge target"
                );
        }
    }

    [Test]
    public void EveryEdge_FieldName_IsCamelCase()
    {
        foreach (var edge in CrossSchemaEdges.All)
            CamelCase
                .IsMatch(edge.FieldName)
                .Should()
                .BeTrue($"edge field '{edge.FieldName}' must be camelCase");
    }

    [Test]
    public void EveryEdge_HasARegisteredLoader()
    {
        var services = new ServiceCollection();
        services.AddBookwormCrossSchema();

        foreach (var edge in CrossSchemaEdges.All)
        {
            var loaderType = typeof(CrossSchemaLoader<,>).MakeGenericType(
                edge.TargetContext,
                edge.Target
            );

            services
                .Any(d => d.ServiceType == loaderType)
                .Should()
                .BeTrue(
                    $"AddBookwormCrossSchema must register CrossSchemaLoader<{edge.TargetContext.Name}, "
                        + $"{edge.Target.Name}> for the {edge.Source.Name}.{edge.FieldName} edge"
                );
        }
    }
}
