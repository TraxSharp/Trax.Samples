using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Trax.Samples.JobHunt.Providers.Llm;

namespace Trax.Samples.JobHunt.E2E.Factories;

public class JobHuntHubFactory : WebApplicationFactory<Hub.Program>
{
    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=jobhunt_e2e_tests;Username=trax;Password=trax123;Maximum Pool Size=10";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:TraxDatabase", ConnectionString);

        builder.ConfigureTestServices(services =>
        {
            // Replace the real Ollama provider with a deterministic stub.
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ILlmProvider));
            if (descriptor is not null)
                services.Remove(descriptor);
            services.AddSingleton<ILlmProvider, StubLlmProvider>();
        });
    }
}
