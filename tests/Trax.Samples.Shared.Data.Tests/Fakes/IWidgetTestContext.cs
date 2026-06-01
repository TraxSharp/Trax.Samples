using Microsoft.EntityFrameworkCore;

namespace Trax.Samples.Shared.Data.Tests.Fakes;

/// <summary>Companion interface for <see cref="WidgetTestContext"/>.</summary>
public interface IWidgetTestContext : ISampleDataContext
{
    DbSet<Widget> Widgets { get; }
}
