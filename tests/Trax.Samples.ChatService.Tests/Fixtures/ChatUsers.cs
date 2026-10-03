using Microsoft.EntityFrameworkCore;
using Trax.Api.Auth;
using Trax.Samples.ChatService.Data;
using Trax.Samples.ChatService.Data.Entities;

namespace Trax.Samples.ChatService.Tests.Fixtures;

/// <summary>
/// The callers a junction sees, as Trax hands them over: <see cref="TraxPrincipal.Id"/> is
/// qualified by the scheme that authenticated the caller (<c>TraxApiKey:alice</c>).
/// </summary>
public static class ChatUsers
{
    public static readonly TraxPrincipal Alice = new("TraxApiKey:alice", "Alice", ["User"]);
    public static readonly TraxPrincipal Bob = new("TraxApiKey:bob", "Bob", ["User"]);

    /// <summary>Seeds a room whose participants are <paramref name="participants"/>.</summary>
    public static async Task<Guid> SeedRoomAsync(
        ChatDbContext db,
        params TraxPrincipal[] participants
    )
    {
        var room = new ChatRoom
        {
            Id = Guid.NewGuid(),
            Name = "Test Room",
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = participants.Length > 0 ? participants[0].Id : Alice.Id,
        };
        db.ChatRooms.Add(room);
        foreach (var p in participants)
            db.ChatParticipants.Add(
                new ChatParticipant
                {
                    Id = Guid.NewGuid(),
                    ChatRoomId = room.Id,
                    UserId = p.Id,
                    DisplayName = p.DisplayName,
                    JoinedAt = DateTime.UtcNow,
                }
            );
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return room.Id;
    }
}
