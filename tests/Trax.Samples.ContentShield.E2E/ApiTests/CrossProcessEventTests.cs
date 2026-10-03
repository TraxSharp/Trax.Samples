using Trax.Samples.ContentShield.E2E.Fixtures;
using Trax.Samples.ContentShield.E2E.Utilities;

namespace Trax.Samples.ContentShield.E2E.ApiTests;

/// <summary>
/// The runner publishes lifecycle events to RabbitMQ, so a subscriber on the API sees a review the
/// runner finished.
/// </summary>
[TestFixture]
public class CrossProcessEventTests : ApiTestFixture
{
    [Test]
    public async Task A_review_the_runner_finishes_reaches_a_subscriber_on_the_api()
    {
        await using var socket = await GraphQLWebSocketClient.ConnectAsync(
            SharedApiSetup.Factory.Server.CreateWebSocketClient(),
            // With API-key auth registered every socket must carry a credential, even one that
            // only wants anonymous trains' events.
            apiKey: ModeratorKey
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
                        moderation {
                            reviewContent(
                                input: { contentId: "e2e-event", contentType: "text", contentBody: "hello" }
                            ) {
                                externalId
                            }
                        }
                    }
                }
                """
            );
        queued.HasErrors.Should().BeFalse(queued.FirstErrorMessage);
        var externalId = queued
            .GetData("dispatch", "moderation", "reviewContent")
            .GetProperty("externalId")
            .GetString();

        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(45);
        while (true)
        {
            var remaining = deadline - DateTime.UtcNow;
            remaining.Should().BePositive("the runner's completion should reach the API");

            var payload = await socket.ReceiveNextAsync(remaining);
            var completed = payload.GetProperty("data").GetProperty("onTrainCompleted");
            if (completed.GetProperty("externalId").GetString() != externalId)
                continue;

            completed.GetProperty("output").GetRawText().Should().Contain("e2e-event");
            return;
        }
    }
}
