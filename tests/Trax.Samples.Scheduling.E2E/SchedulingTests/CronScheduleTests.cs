using Trax.Samples.Scheduling.E2E.Fixtures;

namespace Trax.Samples.Scheduling.E2E.SchedulingTests;

public class CronScheduleTests : SchedulingTestFixture
{
    [Test]
    public async Task New_cron_manifest_waits_for_its_next_occurrence_in_utc()
    {
        var digest = await Db.Manifest(ManifestNames.SendDailyDigest);
        var runs = await Db.Runs(digest.Id, afterId: 0);

        var seededAt = SharedSchedulingSetup.StartedAt;
        var next07 = seededAt.Date.AddHours(7);
        if (next07 <= seededAt)
            next07 = next07.AddDays(1);

        digest.NextScheduledRun.Should().Be(next07);
        runs.Should().BeEmpty("a new cron manifest does not run at startup");
    }
}
