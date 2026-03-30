using Trax.Samples.GameServer.E2E.Fixtures;

namespace Trax.Samples.GameServer.E2E.ApiTests;

[TestFixture]
public class GraphQLMutationTests : ApiTestFixture
{
    [Test]
    public async Task BanPlayer_RunMutation_ExecutesInline()
    {
        var result = await GraphQL.SendAsync(
            """
            mutation {
                dispatch {
                    players {
                        banPlayer(
                            input: { playerId: "player-8", reason: "E2E test ban" }
                        ) {
                            externalId
                        }
                    }
                }
            }
            """,
            apiKey: AdminKey
        );

        result
            .HasErrors.Should()
            .BeFalse($"GraphQL error: {result.FirstErrorMessage} (HTTP {result.StatusCode})");

        var externalId = result
            .GetData("dispatch", "players", "banPlayer")
            .GetProperty("externalId")
            .GetString();
        externalId.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task ProcessMatchResult_QueueMutation_EnqueuesWork()
    {
        var result = await GraphQL.SendAsync(
            """
            mutation {
                dispatch {
                    matches {
                        processMatchResult(
                            input: {
                                region: "na"
                                matchId: "e2e-queue-001"
                                winnerId: "player-1"
                                loserId: "player-2"
                                winnerScore: 80
                                loserScore: 60
                            }
                            mode: QUEUE
                            priority: 10
                        ) {
                            externalId
                            workQueueId
                        }
                    }
                }
            }
            """,
            apiKey: PlayerKey
        );

        result
            .HasErrors.Should()
            .BeFalse($"GraphQL error: {result.FirstErrorMessage} (HTTP {result.StatusCode})");

        var dispatch = result.GetData("dispatch", "matches", "processMatchResult");
        dispatch.GetProperty("externalId").GetString().Should().NotBeNullOrEmpty();
        dispatch.GetProperty("workQueueId").GetInt64().Should().BeGreaterThan(0);
    }
}
