using Microsoft.EntityFrameworkCore;

namespace Trax.Samples.Shared.Data.Tests.Fakes;

/// <summary>
/// A concrete <see cref="SampleDataContext{TSelf}"/> used only to exercise the shared base across
/// the provider matrix. Owns the <c>widgets</c> schema.
/// </summary>
public class WidgetTestContext(DbContextOptions<WidgetTestContext> options)
    : SampleDataContext<WidgetTestContext>(options),
        IWidgetTestContext
{
    public const string SchemaName = "widgets";

    public DbSet<Widget> Widgets => Set<Widget>();

    protected override string Schema => SchemaName;

    protected override void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Widget>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Name).IsRequired();
        });
    }
}
