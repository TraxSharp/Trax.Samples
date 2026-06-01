using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Trax.Samples.Shared.Data.Conversion;

/// <summary>
/// Forces every <see cref="DateTime"/> read out of the database to carry
/// <see cref="DateTimeKind.Utc"/>. Values are stored unchanged and re-tagged as UTC on the
/// way back, so application code never sees an <see cref="DateTimeKind.Unspecified"/> value
/// that silently shifts when formatted in a local timezone.
/// </summary>
public sealed class UtcValueConverter : ValueConverter<DateTime, DateTime>
{
    public UtcValueConverter()
        : base(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc)) { }
}
