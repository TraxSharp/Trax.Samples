using Trax.Effect.Services.EffectJunction;

namespace Trax.Samples.Recovery.Trains.Refund.Junctions;

/// <summary>Tells the customer what happened, whichever track ran.</summary>
public class NotifyCustomer(DemoPace pace) : EffectJunction<RefundOutcome, RefundResult>
{
    public override async Task<RefundResult> Run(RefundOutcome outcome)
    {
        await Task.Delay(pace.StepDelay);
        return new RefundResult(
            outcome.OrderId,
            outcome.Status,
            outcome.Amount,
            CustomerNotified: true
        );
    }
}
