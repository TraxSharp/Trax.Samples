namespace Trax.Samples.Shared.Data;

/// <summary>
/// Minimal contract every domain data context exposes to application code (trains,
/// junctions, services). A domain declares its own <c>I{Domain}DataContext</c> deriving
/// this interface and adding the owned <c>DbSet&lt;T&gt;</c> properties (plus any
/// cross-schema read accessors). Depending on the interface rather than the concrete
/// context keeps junction logic testable.
/// </summary>
public interface ISampleDataContext
{
    /// <summary>Persists pending changes. Mirrors <see cref="Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync(CancellationToken)"/>.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
