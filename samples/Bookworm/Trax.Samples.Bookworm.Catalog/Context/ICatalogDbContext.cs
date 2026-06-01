using Microsoft.EntityFrameworkCore;
using Trax.Samples.Bookworm.Catalog.Models.Authors;
using Trax.Samples.Bookworm.Catalog.Models.Books;
using Trax.Samples.Shared.Data;

namespace Trax.Samples.Bookworm.Catalog.Context;

/// <summary>Companion interface for <see cref="CatalogDbContext"/>.</summary>
public interface ICatalogDbContext : ISampleDataContext
{
    DbSet<Author> Authors { get; }
    DbSet<Book> Books { get; }
}
