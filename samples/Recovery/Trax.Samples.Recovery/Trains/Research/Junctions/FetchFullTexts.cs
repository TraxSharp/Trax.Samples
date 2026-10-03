using Trax.Effect.Services.EffectJunction;
using Trax.Samples.Recovery.Faults;

namespace Trax.Samples.Recovery.Trains.Research.Junctions;

/// <summary>
/// The second tool call, when the findings need checking. This is the step the page can crash: it
/// fails after both model calls, so the retry has two answers to replay.
/// </summary>
public class FetchFullTexts(FaultInjector faults, DemoPace pace)
    : EffectJunction<Findings, CheckedFindings>
{
    public override async Task<CheckedFindings> Run(Findings findings)
    {
        await Task.Delay(pace.StepDelay);

        if (faults.TryFire(findings.RunId, CrashPoint.ToolCall))
            throw new HttpRequestException(
                "The full-text service dropped the connection (crash injected by the demo)."
            );

        return new CheckedFindings(findings, nameof(Depth.CrossCheck));
    }
}
