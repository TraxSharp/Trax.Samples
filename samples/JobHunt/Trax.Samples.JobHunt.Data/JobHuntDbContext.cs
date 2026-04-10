using Microsoft.EntityFrameworkCore;
using Trax.Samples.JobHunt.Data.Entities;

namespace Trax.Samples.JobHunt.Data;

public class JobHuntDbContext(DbContextOptions<JobHuntDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ApiKey).IsRequired();
            entity.Property(e => e.DisplayName).IsRequired();
            entity.HasIndex(e => e.ApiKey).IsUnique();
        });
    }
}
