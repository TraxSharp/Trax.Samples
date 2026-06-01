using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Trax.Samples.Shared.Data.Extensions;
using Trax.Samples.Shared.Data.Tests.Fakes;
using Trax.Samples.Shared.Testing;

namespace Trax.Samples.Shared.Data.Tests.UnitTests;

[TestFixture]
public class SampleDataContextTests
{
    #region Schema isolation

    [Test]
    public void OnModelCreating_OnNpgsql_AppliesDeclaredSchema()
    {
        // Building the model against Npgsql does not open a connection, so a throwaway connection
        // string is enough to inspect what HasDefaultSchema produced.
        using var context = new WidgetTestContext(
            new DbContextOptionsBuilder<WidgetTestContext>()
                .UseNpgsql("Host=localhost;Database=offline_model_only")
                .Options
        );

        context.Model.GetDefaultSchema().Should().Be(WidgetTestContext.SchemaName);
    }

    [Test]
    public void OnModelCreating_OnSqlite_SkipsSchema()
    {
        using var holder = SampleTestContext.Sqlite<WidgetTestContext>();

        holder
            .Context.Model.GetDefaultSchema()
            .Should()
            .BeNull("SQLite has no schema concept, so the base must skip HasDefaultSchema");
    }

    [Test]
    public void OnModelCreating_OnInMemory_SkipsSchema()
    {
        using var context = SampleTestContext.InMemory<WidgetTestContext>();

        context.Model.GetDefaultSchema().Should().BeNull();
    }

    #endregion

    #region UTC conversion

    [Test]
    public async Task DateTime_RoundTrips_AsUtc()
    {
        using var holder = SampleTestContext.Sqlite<WidgetTestContext>();
        var saved = new DateTime(2024, 6, 1, 9, 30, 0, DateTimeKind.Utc);

        holder.Context.Widgets.Add(new Widget { Name = "gizmo", CreatedAt = saved });
        await holder.Context.SaveChangesAsync();
        holder.Context.ChangeTracker.Clear();

        var read = await holder.Context.Widgets.SingleAsync();
        read.CreatedAt.Should().Be(saved);
        read.CreatedAt.Kind.Should().Be(DateTimeKind.Utc, "the base applies a UTC value converter");
    }

    #endregion

    #region Raw / registration / bootstrap

    [Test]
    public void Raw_ReturnsConcreteContext_FromInterface()
    {
        using var context = SampleTestContext.InMemory<WidgetTestContext>();
        IWidgetTestContext asInterface = context;

        var raw = ((WidgetTestContext)asInterface).Raw();

        raw.Should().BeSameAs(context);
    }

    [Test]
    public async Task AddSampleDataContext_RegistersInterface_AndBootstrapsSchema()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddSampleDataContext<IWidgetTestContext, WidgetTestContext>(options =>
            options.UseInMemoryDatabase(dbName)
        );

        await using var provider = services.BuildServiceProvider();
        await provider.EnsureSampleSchemaAsync<WidgetTestContext>();

        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IWidgetTestContext>();
        context.Widgets.Add(new Widget { Name = "registered", CreatedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        context.Widgets.Should().ContainSingle(w => w.Name == "registered");
    }

    #endregion
}
