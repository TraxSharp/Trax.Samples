using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Trax.Samples.Bookworm.Catalog.Context;
using Trax.Samples.Shared.Data.Extensions;

namespace Trax.Samples.Bookworm.Catalog.Extensions;

/// <summary>Registration for the catalog data context.</summary>
public static class CatalogServiceCollectionExtensions
{
    /// <summary>Registers <see cref="CatalogDbContext"/> behind <see cref="ICatalogDbContext"/> against PostgreSQL.</summary>
    public static IServiceCollection AddCatalogDataContext(
        this IServiceCollection services,
        string connectionString
    ) =>
        services.AddSampleDataContext<ICatalogDbContext, CatalogDbContext>(options =>
            options.UseNpgsql(connectionString)
        );
}
