using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Trax.Samples.JobHunt.E2E.Factories;

public class JobHuntHubFactory : WebApplicationFactory<Hub.Program>
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=jobhunt_e2e_tests;Username=trax;Password=trax123;Maximum Pool Size=10";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:TraxDatabase", ConnectionString);
    }
}
