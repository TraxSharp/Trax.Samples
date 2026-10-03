using Trax.Effect.Services.EffectJunction;
using Trax.Samples.Recovery.Faults;

namespace Trax.Samples.Recovery.Trains.Research.Junctions;

/// <summary>
/// The second tool call, when the findings are enough as they are. Like <see cref="FetchFullTexts"/>
/// it can be crashed, so the demo crashes whichever track the depth decision took.
/// </summary>
public class SkimSources(FaultInjector faults, DemoPace pace)
    : EffectJunction<Findings, CheckedFindings>
{
    public override async Task<CheckedFindings> Run(Findings findings)
    {
        await Task.Delay(pace.StepDelay);

        if (faults.TryFire(findings.RunId, CrashPoint.ToolCall))
            throw new HttpRequestException(
                "The page fetcher dropped the connection (crash injected by the demo)."
            );

        return new CheckedFindings(findings, nameof(Depth.Skim));
    }
}
