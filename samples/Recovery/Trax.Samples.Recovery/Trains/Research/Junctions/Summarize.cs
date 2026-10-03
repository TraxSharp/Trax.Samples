using Trax.Effect.Services.EffectJunction;

namespace Trax.Samples.Recovery.Trains.Research.Junctions;

/// <summary>Writes the report from whatever the tracks found.</summary>
public class Summarize(DemoPace pace) : EffectJunction<CheckedFindings, ResearchReport>
{
    public override async Task<ResearchReport> Run(CheckedFindings input)
    {
        await Task.Delay(pace.StepDelay);
        var findings = input.Findings;
        return new ResearchReport(
            findings.Topic,
            findings.Source,
            input.Depth,
            $"{findings.Topic}: {string.Join("; ", findings.Notes)}."
        );
    }
}
