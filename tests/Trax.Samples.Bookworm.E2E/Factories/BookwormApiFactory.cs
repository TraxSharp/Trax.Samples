using Trax.Samples.Shared.Testing;

namespace Trax.Samples.Bookworm.E2E.Factories;

/// <summary>
/// Boots the Bookworm API against a dedicated test PostgreSQL instance. The connection string can be
/// overridden via the <c>BOOKWORM_TEST_DB</c> environment variable; it defaults to the throwaway
/// instance the test docs spin up on port 5433, which keeps E2E runs off any shared database.
/// </summary>
public sealed class BookwormApiFactory : SampleApiFactory<Api.Program>
{
    public const string DefaultConnectionString =
        "Host=localhost;Port=5433;Database=trax;Username=trax;Password=trax123;"
        + "Maximum Pool Size=8;Minimum Pool Size=0";

    protected override string ConnectionString =>
        Environment.GetEnvironmentVariable("BOOKWORM_TEST_DB") ?? DefaultConnectionString;
}
