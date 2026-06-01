using Microsoft.EntityFrameworkCore;
using Trax.Samples.ChatService.Data.Entities;
using Trax.Samples.Shared.Data;

namespace Trax.Samples.ChatService.Data;

/// <summary>Companion interface for <see cref="ChatDbContext"/>.</summary>
public interface IChatDbContext : ISampleDataContext
{
    DbSet<ChatRoom> ChatRooms { get; }
    DbSet<ChatParticipant> ChatParticipants { get; }
    DbSet<ChatMessage> ChatMessages { get; }
}
