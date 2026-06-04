using LanguageExt;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Trax.Effect.Attributes;
using Trax.Effect.Models.Metadata;
using Trax.Effect.Services.ServiceTrain;
using Trax.Samples.GameServer.Data;
using Trax.Samples.GameServer.Data.Models;
using Trax.Samples.GameServer.Trains.Matches.ProcessMatchResult.Junctions;

namespace Trax.Samples.GameServer.Trains.Matches.ProcessMatchResult;

/// <summary>
/// Multi-step match result processor. Can be queued from the API via GraphQL
/// or run on a recurring schedule by the scheduler for batch reprocessing.
///
/// Junctions: ValidateMatch → UpdatePlayerStats → CheckForAnomalies
///
/// Returns a typed ProcessMatchResultOutput with match processing summary.
/// When anomalies are detected, CheckForAnomaliesJunction activates the dormant
/// DetectCheatPattern dependent train via IDormantDependentContext.
///
/// Demonstrates the queue-time <see cref="OnQueue"/> hook: when a match is QUEUED
/// (not run synchronously), it writes an optimistic <see cref="MatchRecord"/> the
/// instant the mutation is accepted, so the match shows up immediately instead of
/// only after the scheduler runs the train seconds later.
/// </summary>
[TraxAllowAnonymous]
[TraxMutation(
    Namespace = GraphQLNamespaces.Matches,
    Description = "Processes a completed match result"
)]
[TraxBroadcast]
public class ProcessMatchResultTrain
    : ServiceTrain<ProcessMatchResultInput, ProcessMatchResultOutput>,
        IProcessMatchResultTrain
{
    /// <summary>
    /// Factory for the game data context. Injected via <see cref="InjectAttribute"/> so the
    /// queue-time hook can write without going through the railway. A factory (rather than a
    /// scoped context) gives the hook its own connection, the same way a real optimistic-write
    /// consumer keeps its side-effect independent of Trax's own data context.
    /// </summary>
    [Inject]
    public IDbContextFactory<GameDbContext>? GameDbFactory { get; set; }

    protected override Task<Either<Exception, ProcessMatchResultOutput>> Junctions() =>
        Chain<ValidateMatchJunction>()
            .Chain<UpdatePlayerStatsJunction>()
            .Chain<CheckForAnomaliesJunction>()
            .Resolve();

    /// <summary>
    /// Optimistic shadow write. Fires synchronously inside the API's QUEUE mutation, before the
    /// work queue row is inserted and long before the scheduler runs the train. The record is
    /// stamped with the run's ExternalId (the same id the eventual run executes under) so a real
    /// consumer could reconcile the optimistic row against the authoritative one.
    /// </summary>
    protected override async Task OnQueue(Metadata metadata, CancellationToken ct)
    {
        var input = metadata.GetInput<ProcessMatchResultInput>();
        if (input is null || GameDbFactory is null)
            return;

        await using var db = await GameDbFactory.CreateDbContextAsync(ct);

        // Idempotent: OnQueue can fire again if the mutation is retried/re-queued, and match_id
        // is unique, so only write the optimistic record when one does not already exist. The
        // hook MUST be idempotent — the deferred run re-executes the chain, and a real consumer
        // would reconcile this row rather than blindly insert.
        if (await db.Matches.AnyAsync(m => m.MatchId == input.MatchId, ct))
            return;

        db.Matches.Add(
            new MatchRecord
            {
                MatchId = input.MatchId,
                Region = input.Region,
                WinnerId = input.WinnerId,
                LoserId = input.LoserId,
                WinnerScore = input.WinnerScore,
                LoserScore = input.LoserScore,
            }
        );
        await db.SaveChangesAsync(ct);

        Logger?.LogInformation(
            "OnQueue optimistic match write: matchId={MatchId} region={Region} externalId={ExternalId}",
            input.MatchId,
            input.Region,
            metadata.ExternalId
        );
    }
}
