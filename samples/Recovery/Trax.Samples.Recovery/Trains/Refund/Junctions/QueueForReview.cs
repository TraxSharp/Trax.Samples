using Trax.Effect.Services.EffectJunction;

namespace Trax.Samples.Recovery.Trains.Refund.Junctions;

/// <summary>Hands a refund the model was unsure about to a person.</summary>
public class QueueForReview(DemoPace pace) : EffectJunction<RefundCase, RefundOutcome>
{
    public override async Task<RefundOutcome> Run(RefundCase refund)
    {
        await Task.Delay(pace.StepDelay);
        return new RefundOutcome(refund.RunId, refund.OrderId, "QueuedForReview", 0m);
    }
}
