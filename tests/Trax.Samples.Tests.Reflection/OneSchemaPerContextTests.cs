using Microsoft.EntityFrameworkCore;
using Trax.Samples.Bookworm.Catalog;
using Trax.Samples.Bookworm.Catalog.Context;
using Trax.Samples.Bookworm.Lending;
using Trax.Samples.Bookworm.Lending.Context;

namespace Trax.Samples.Tests.Reflection;

/// <summary>
/// Enforces the schema half of the 1:1:1 rule: each domain context declares its own non-null default
/// schema, and no two contexts share a schema. Building the Npgsql model offline (no connection) is
/// enough to read the default schema each context applied. New domain contexts should be added here.
/// </summary>
[TestFixture]
public class OneSchemaPerContextTests
{
    private static readonly IReadOnlyList<Type> DomainContexts =
    [
        typeof(CatalogDbContext),
        typeof(LendingDbContext),
    ];

    private static string? SchemaOf<TContext>()
        where TContext : DbContext
    {
        using var context = (TContext)
            Activator.CreateInstance(
                typeof(TContext),
                new DbContextOptionsBuilder<TContext>()
                    .UseNpgsql("Host=localhost;Database=offline_model_only")
                    .Options
            )!;
        return context.Model.GetDefaultSchema();
    }

    [Test]
    public void EachContext_DeclaresItsOwnSchema()
    {
        SchemaOf<CatalogDbContext>().Should().Be(CatalogSchema.Name);
        SchemaOf<LendingDbContext>().Should().Be(LendingSchema.Name);
    }

    [Test]
    public void NoTwoContexts_ShareASchema()
    {
        var schemas = new[] { SchemaOf<CatalogDbContext>(), SchemaOf<LendingDbContext>() };

        schemas.Should().OnlyContain(s => !string.IsNullOrEmpty(s), "every context owns a schema");
        schemas
            .Should()
            .OnlyHaveUniqueItems("no two domain contexts may share a PostgreSQL schema (1:1:1)");

        // Keep the offline list honest: if a domain context is added or removed, update DomainContexts.
        DomainContexts.Should().HaveCount(schemas.Length);
    }
}
