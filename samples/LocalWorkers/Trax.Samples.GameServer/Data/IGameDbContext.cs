using Microsoft.EntityFrameworkCore;
using Trax.Samples.GameServer.Data.Models;
using Trax.Samples.Shared.Data;

namespace Trax.Samples.GameServer.Data;

/// <summary>Companion interface for <see cref="GameDbContext"/>.</summary>
public interface IGameDbContext : ISampleDataContext
{
    DbSet<PlayerRecord> Players { get; }
    DbSet<MatchRecord> Matches { get; }
    DbSet<PublicAnnouncement> Announcements { get; }
}
