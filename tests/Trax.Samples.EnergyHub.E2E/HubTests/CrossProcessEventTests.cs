using Trax.Samples.EnergyHub.E2E.Fixtures;
using Trax.Samples.EnergyHub.E2E.Utilities;

namespace Trax.Samples.EnergyHub.E2E.HubTests;

/// <summary>
/// The reason the sample carries RabbitMQ: a train the worker runs reaches a subscriber on the hub.
/// The hub runs no jobs (<see cref="HubExecutesNoTrainsTests"/>), so the completion this test
/// receives can only have come from the worker, over the broker.
/// </summary>
[TestFixture]
public class CrossProcessEventTests : HubTestFixture
{
    [Test]
    public async Task A_trade_the_worker_runs_reaches_a_subscriber_on_the_hub()
    {
        await using var socket = await GraphQLWebSocketClient.ConnectAsync(
            SharedHubSetup.Factory.Server.CreateWebSocketClient(),
            apiKey: OperatorKey
        );
        await socket.SubscribeAsync(
            "completed",
            "subscription { onTrainCompleted { externalId trainName output } }"
        );

        var queued = await GetGraphQLClient()
            .SendAsync(
                """
                mutation {
                    dispatch {
                        tradeGridEnergy(input: { ratePerKwh: 0.14, maxSellPercent: 80 }) {
                            externalId
                        }
                    }
                }
                """,
                apiKey: OperatorKey
            );
        queued.HasErrors.Should().BeFalse(queued.FirstErrorMessage);

        // Other trains (and other hosts sharing the broker) may complete meanwhile: wait for ours.
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(45);
        while (true)
        {
            var remaining = deadline - DateTime.UtcNow;
            remaining.Should().BePositive("the worker's completion should reach the hub");

            var payload = await socket.ReceiveNextAsync(remaining);
            var completed = payload.GetProperty("data").GetProperty("onTrainCompleted");
            if (
                completed.GetProperty("trainName").GetString()?.EndsWith("ITradeGridEnergyTrain")
                != true
            )
                continue;

            completed.GetProperty("output").GetRawText().Should().Contain("ubossTransactionId");
            return;
        }
    }
}
