using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Trax.Samples.Bookworm.Lending.Context;
using Trax.Samples.Shared.Data.Extensions;

namespace Trax.Samples.Bookworm.Lending.Extensions;

/// <summary>Registration for the lending data context.</summary>
public static class LendingServiceCollectionExtensions
{
    /// <summary>Registers <see cref="LendingDbContext"/> behind <see cref="ILendingDbContext"/> against PostgreSQL.</summary>
    public static IServiceCollection AddLendingDataContext(
        this IServiceCollection services,
        string connectionString
    ) =>
        services.AddSampleDataContext<ILendingDbContext, LendingDbContext>(options =>
            options.UseNpgsql(connectionString)
        );
}
