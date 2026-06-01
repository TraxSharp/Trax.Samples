using Microsoft.EntityFrameworkCore;
using Trax.Samples.Shared.Data;

namespace Trax.Samples.PersistedOperations.Models;

/// <summary>Companion interface for <see cref="UserNotesDbContext"/>.</summary>
public interface IUserNotesDbContext : ISampleDataContext
{
    DbSet<UserNote> Notes { get; }
}
