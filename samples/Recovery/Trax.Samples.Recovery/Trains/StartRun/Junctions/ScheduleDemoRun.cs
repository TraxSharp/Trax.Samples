using Trax.Effect.Services.EffectJunction;
using Trax.Samples.Recovery.Faults;
using Trax.Samples.Recovery.Records;
using Trax.Samples.Recovery.Trains.Refund;
using Trax.Samples.Recovery.Trains.Research;
using Trax.Scheduler.Services.TraxScheduler;

namespace Trax.Samples.Recovery.Trains.StartRun.Junctions;

/// <summary>
/// Opens the run's case file, arms the crash, and schedules a one-off manifest that runs now and
/// retries a failed run up to <see cref="MaxRetries"/> times.
/// </summary>
public class ScheduleDemoRun(ITraxScheduler scheduler, CaseFiles caseFiles, FaultInjector faults)
    : EffectJunction<StartRunInput, StartRunOutput>
{
    /// <summary>Two retries: three attempts in all.</summary>
    public const int MaxRetries = 2;

    public override async Task<StartRunOutput> Run(StartRunInput input)
    {
        var runId = Guid.NewGuid().ToString("N")[..12];
        var externalId = $"recovery-{runId}";

        // The crash is armed beside the run, never in the manifest's input: a retry replays the
        // first attempt's decisions only when the input is byte-identical between attempts.
        var crash = input.CrashOnce
            ? input.Scenario == Scenario.Research
                ? CrashPoint.ToolCall
                : CrashPoint.Payment
            : CrashPoint.None;
        faults.Arm(runId, crash);

        var manifest = input.Scenario switch
        {
            Scenario.Research => await ScheduleResearch(
                runId,
                externalId,
                input.Topic ?? "Durable execution research"
            ),
            Scenario.Refund => await ScheduleRefund(runId, externalId, input.OrderId ?? "A-1001"),
            _ => throw new ArgumentOutOfRangeException(nameof(input), input.Scenario, null),
        };

        return new StartRunOutput
        {
            RunId = runId,
            ManifestId = manifest.Id,
            ManifestExternalId = manifest.ExternalId,
            TrainName = manifest.Name,
            ArmedCrash = crash,
            MaxRetries = MaxRetries,
        };
    }

    private Task<Trax.Effect.Models.Manifest.Manifest> ScheduleResearch(
        string runId,
        string externalId,
        string topic
    )
    {
        if (string.IsNullOrWhiteSpace(topic) || topic.Length > 200)
            throw new ArgumentException("A topic is 1 to 200 characters.", nameof(topic));

        caseFiles.OpenResearch(runId);
        return scheduler.ScheduleOnceAsync<IResearchTopicTrain, ResearchInput, ResearchReport>(
            externalId,
            new ResearchInput { RunId = runId, Topic = topic.Trim() },
            TimeSpan.Zero,
            options => options.MaxRetries(MaxRetries)
        );
    }

    private Task<Trax.Effect.Models.Manifest.Manifest> ScheduleRefund(
        string runId,
        string externalId,
        string orderId
    )
    {
        caseFiles.OpenRefund(runId, orderId);
        return scheduler.ScheduleOnceAsync<IApproveRefundTrain, RefundInput, RefundResult>(
            externalId,
            new RefundInput { RunId = runId, OrderId = orderId },
            TimeSpan.Zero,
            options => options.MaxRetries(MaxRetries)
        );
    }
}
