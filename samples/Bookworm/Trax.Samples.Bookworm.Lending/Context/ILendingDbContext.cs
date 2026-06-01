using Microsoft.EntityFrameworkCore;
using Trax.Samples.Bookworm.Lending.Models.Loans;
using Trax.Samples.Bookworm.Lending.Models.Members;
using Trax.Samples.Shared.Data;

namespace Trax.Samples.Bookworm.Lending.Context;

/// <summary>Companion interface for <see cref="LendingDbContext"/>.</summary>
public interface ILendingDbContext : ISampleDataContext
{
    DbSet<Member> Members { get; }
    DbSet<Loan> Loans { get; }
}
