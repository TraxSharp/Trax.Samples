using Trax.Effect.Services.EffectJunction;

namespace Trax.Samples.Recovery.Trains.Refund.Junctions;

/// <summary>Records that the refund was declined.</summary>
public class DeclineRefund(DemoPace pace) : EffectJunction<RefundCase, RefundOutcome>
{
    public override async Task<RefundOutcome> Run(RefundCase refund)
    {
        await Task.Delay(pace.StepDelay);
        return new RefundOutcome(refund.RunId, refund.OrderId, "Declined", 0m);
    }
}
