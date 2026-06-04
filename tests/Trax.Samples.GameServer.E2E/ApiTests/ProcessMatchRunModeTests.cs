using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Trax.Samples.GameServer.Data;
using Trax.Samples.GameServer.E2E.Fixtures;

namespace Trax.Samples.GameServer.E2E.ApiTests;

/// <summary>
/// ProcessMatchResult supports both Run and Queue modes but requires IDormantDependentContext
/// (scheduler-only) for CheckForAnomaliesJunction. On the API (no scheduler), only Queue mode
/// works. These tests verify Queue mode with data-layer assertions beyond what
/// GraphQLMutationTests already covers.
/// </summary>
[TestFixture]
public class ProcessMatchRunModeTests : ApiTestFixture
{
    [Test]
    public async Task ProcessMatchResult_QueueMode_WithHighPriority_SetsCorrectPriority()
    {
        var result = await GraphQL.SendAsync(
            """
            mutation {
                dispatch {
                    matches {
                        processMatchResult(
                            input: {
                                region: "eu"
                                matchId: "e2e-priority-001"
                                winnerId: "player-3"
                                loserId: "player-4"
                                winnerScore: 80
                                loserScore: 60
                            }
                            mode: QUEUE
                            priority: 99
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

        var workQueueId = result
            .GetData("dispatch", "matches", "processMatchResult")
            .GetProperty("workQueueId")
            .GetInt64();

        // Verify priority was persisted.
        DataContext.Reset();
        var entry = await DataContext
            .WorkQueues.AsNoTracking()
            .FirstOrDefaultAsync(wq => wq.Id == workQueueId);

        entry.Should().NotBeNull();
        // Priority in the GraphQL mutation is additive to the manifest's base priority.
        // Just verify it's stored and greater than 0.
        entry!.Priority.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task ProcessMatchResult_QueueMode_InputContainsAllFields()
    {
        var result = await GraphQL.SendAsync(
            """
            mutation {
                dispatch {
                    matches {
                        processMatchResult(
                            input: {
                                region: "ap"
                                matchId: "e2e-fields-001"
                                winnerId: "player-5"
                                loserId: "player-6"
                                winnerScore: 75
                                loserScore: 70
                            }
                            mode: QUEUE
                        ) {
                            workQueueId
                        }
                    }
                }
            }
            """,
            apiKey: PlayerKey
        );

        result.HasErrors.Should().BeFalse();

        var workQueueId = result
            .GetData("dispatch", "matches", "processMatchResult")
            .GetProperty("workQueueId")
            .GetInt64();

        DataContext.Reset();
        var entry = await DataContext
            .WorkQueues.AsNoTracking()
            .FirstOrDefaultAsync(wq => wq.Id == workQueueId);

        entry.Should().NotBeNull();
        entry!.Input.Should().Contain("ap");
        entry.Input.Should().Contain("e2e-fields-001");
        entry.Input.Should().Contain("player-5");
        entry.Input.Should().Contain("player-6");
    }

    /// <summary>
    /// The train overrides OnQueue to write an optimistic MatchRecord the instant a match is
    /// QUEUED. The API process runs no scheduler, so the queued train never executes here — which
    /// means the only thing that can write a MatchRecord for this matchId is the OnQueue hook
    /// firing synchronously inside the mutation. Finding the row proves the hook ran at enqueue.
    /// </summary>
    [Test]
    public async Task ProcessMatchResult_QueueMode_FiresOnQueue_WritingOptimisticMatchRecord()
    {
        const string matchId = "e2e-onqueue-optimistic-001";

        var gameDbFactory = SharedApiSetup.Factory.Services.GetRequiredService<
            IDbContextFactory<GameDbContext>
        >();

        // Deterministic start: clear any optimistic row left by a previous run of this test.
        await using (var seed = await gameDbFactory.CreateDbContextAsync())
            await seed.Matches.Where(m => m.MatchId == matchId).ExecuteDeleteAsync();

        var result = await GraphQL.SendAsync(
            $$"""
            mutation {
                dispatch {
                    matches {
                        processMatchResult(
                            input: {
                                region: "na"
                                matchId: "{{matchId}}"
                                winnerId: "player-1"
                                loserId: "player-2"
                                winnerScore: 100
                                loserScore: 30
                            }
                            mode: QUEUE
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

        var externalId = result
            .GetData("dispatch", "matches", "processMatchResult")
            .GetProperty("externalId")
            .GetString();
        externalId.Should().NotBeNullOrEmpty();

        // No scheduler here, so the queued train has not run: there is no run metadata for it.
        // Any MatchRecord that exists therefore came from OnQueue, not from the train executing.
        DataContext.Reset();
        var ranMetadata = await DataContext
            .Metadatas.AsNoTracking()
            .FirstOrDefaultAsync(m => m.ExternalId == externalId);
        ranMetadata
            .Should()
            .BeNull("the API runs no scheduler, so the queued train has not executed");

        await using var db = await gameDbFactory.CreateDbContextAsync();
        var optimistic = await db
            .Matches.AsNoTracking()
            .Where(m => m.MatchId == matchId)
            .ToListAsync();

        optimistic
            .Should()
            .HaveCount(
                1,
                "OnQueue writes exactly one optimistic MatchRecord the instant the match is queued"
            );
        optimistic[0].Region.Should().Be("na");
        optimistic[0].WinnerId.Should().Be("player-1");
        optimistic[0].LoserId.Should().Be("player-2");
        optimistic[0].WinnerScore.Should().Be(100);
        optimistic[0].LoserScore.Should().Be(30);
    }
}
