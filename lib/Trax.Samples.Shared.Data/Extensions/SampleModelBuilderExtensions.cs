using Microsoft.EntityFrameworkCore;
using Trax.Samples.Shared.Data.Conversion;

namespace Trax.Samples.Shared.Data.Extensions;

/// <summary>
/// Model-builder helpers shared by every domain data context.
/// </summary>
public static class SampleModelBuilderExtensions
{
    /// <summary>
    /// Applies <see cref="UtcValueConverter"/> to every <see cref="DateTime"/> /
    /// <c>DateTime?</c> property in the model so all timestamps round-trip as UTC.
    /// </summary>
    public static ModelBuilder ApplyUtcDateTimeConverter(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime) || property.ClrType == typeof(DateTime?))
                    property.SetValueConverter(new UtcValueConverter());
            }
        }

        return modelBuilder;
    }
}
