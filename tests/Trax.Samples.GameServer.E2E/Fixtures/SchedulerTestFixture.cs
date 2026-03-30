using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Trax.Effect.Data.Services.DataContext;
using Trax.Effect.Data.Services.IDataContextFactory;
using Trax.Mediator.Services.TrainBus;
using Trax.Samples.GameServer.E2E.Factories;
using Trax.Scheduler.Configuration;

namespace Trax.Samples.GameServer.E2E.Fixtures;

[TestFixture]
public abstract class SchedulerTestFixture
{
    private GameServerSchedulerFactory Factory { get; set; } = null!;

    protected IServiceScope Scope { get; private set; } = null!;

    protected ITrainBus TrainBus { get; private set; } = null!;

    protected IDataContext DataContext { get; private set; } = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        Factory = new GameServerSchedulerFactory();

        // Accessing Services triggers host startup, which runs SchedulerStartupService
        // (seeds manifests, recovers stuck jobs, prunes orphans).
        _ = Factory.Services;

        // Wait for SchedulerStartupService to finish seeding manifests.
        await WaitForManifestsSeeded();

        // Disable the ManifestManager after manifests are seeded to prevent automatic
        // work queue creation that competes with test-created entries.
        // The JobDispatcher stays enabled to dispatch test work queue entries.
        var config = Factory.Services.GetRequiredService<SchedulerConfiguration>();
        config.ManifestManagerEnabled = false;
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await Factory.DisposeAsync();
    }

    [SetUp]
    public virtual async Task SetUp()
    {
        Scope = Factory.Services.CreateScope();
        TrainBus = Scope.ServiceProvider.GetRequiredService<ITrainBus>();

        var dataContextFactory =
            Scope.ServiceProvider.GetRequiredService<IDataContextProviderFactory>();
        DataContext = (IDataContext)dataContextFactory.Create();

        await CleanExecutionData();
    }

    [TearDown]
    public void TearDown()
    {
        if (DataContext is IDisposable disposable)
            disposable.Dispose();

        Scope.Dispose();
    }

    /// <summary>
    /// Re-enables the ManifestManager for tests that need it (e.g., dependency chain tests
    /// where the ManifestManager must detect parent completion and enqueue dependents).
    /// </summary>
    protected void EnableManifestManager()
    {
        var config = Factory.Services.GetRequiredService<SchedulerConfiguration>();
        config.ManifestManagerEnabled = true;
    }

    /// <summary>
    /// Disables the ManifestManager to prevent automatic scheduling interference.
    /// </summary>
    protected void DisableManifestManager()
    {
        var config = Factory.Services.GetRequiredService<SchedulerConfiguration>();
        config.ManifestManagerEnabled = false;
    }

    /// <summary>
    /// Cleans execution data (metadata, work queues, dead letters, logs, background jobs)
    /// but preserves manifests and manifest groups so they don't need to be re-seeded.
    /// </summary>
    private async Task CleanExecutionData()
    {
        await DataContext.BackgroundJobs.ExecuteDeleteAsync();
        await DataContext.Logs.ExecuteDeleteAsync();
        await DataContext.WorkQueues.ExecuteDeleteAsync();
        await DataContext.DeadLetters.ExecuteDeleteAsync();
        await DataContext.Metadatas.ExecuteDeleteAsync();
        DataContext.Reset();
    }

    private async Task WaitForManifestsSeeded()
    {
        using var scope = Factory.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDataContextProviderFactory>();
        var dc = (IDataContext)factory.Create();

        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            dc.Reset();
            var count = await dc.Manifests.AsNoTracking().CountAsync();
            if (count > 0)
                return;
            await Task.Delay(250);
        }

        throw new TimeoutException("Manifests were not seeded within 15 seconds.");
    }
}
