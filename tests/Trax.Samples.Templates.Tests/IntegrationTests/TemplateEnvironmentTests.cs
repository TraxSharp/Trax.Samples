using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Trax.Samples.Templates.Tests.IntegrationTests;

/// <summary>
/// What a project scaffolded from the <c>trax-hub</c>, <c>trax-scheduler</c> and
/// <c>trax-api</c> templates serves outside Development. The templates put no authorization in
/// front of the dashboard and ship a plaintext demo API key, so both exist only in
/// Development, where <c>dotnet run</c> starts through launchSettings.json.
///
/// <para>Enforces <c>docs/adr/0003-templates-serve-the-dashboard-only-in-development.md</c>.</para>
/// </summary>
[TestFixture]
[Property("adr", "docs/adr/0003-templates-serve-the-dashboard-only-in-development.md")]
public class TemplateEnvironmentTests
{
    private const string Adr =
        "see docs/adr/0003-templates-serve-the-dashboard-only-in-development.md";

    // Any public type in the template's assembly locates its entry point.
    private static WebApplicationFactory<T> Start<T>(string environment)
        where T : class =>
        new WebApplicationFactory<T>().WithWebHostBuilder(b => b.UseEnvironment(environment));

    [Test]
    public async Task The_hub_template_serves_no_dashboard_outside_Development()
    {
        await using var factory = Start<Hub.Data.AppDbContext>("Production");

        var response = await factory.CreateClient().GetAsync("/trax");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, Adr);
    }

    [Test]
    public async Task The_hub_template_serves_the_dashboard_in_Development()
    {
        await using var factory = Start<Hub.Data.AppDbContext>("Development");

        var response = await factory.CreateClient().GetAsync("/trax");

        response.StatusCode.Should().Be(HttpStatusCode.OK, "the premise of the Production test");
    }

    [Test]
    public async Task The_scheduler_template_serves_no_dashboard_outside_Development()
    {
        await using var factory = Start<Scheduler.Trains.HelloWorld.HelloWorldInput>("Production");

        var response = await factory.CreateClient().GetAsync("/trax");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, Adr);
    }

    [Test]
    public async Task The_scheduler_template_serves_the_dashboard_in_Development()
    {
        await using var factory = Start<Scheduler.Trains.HelloWorld.HelloWorldInput>("Development");

        var response = await factory.CreateClient().GetAsync("/trax");

        response.StatusCode.Should().Be(HttpStatusCode.OK, "the premise of the Production test");
    }

    [Test]
    public async Task The_hub_template_registers_the_demo_key_only_in_Development()
    {
        (await SchemeNames<Hub.Data.AppDbContext>("Production")).Should().BeEmpty(Adr);
        (await SchemeNames<Hub.Data.AppDbContext>("Development")).Should().NotBeEmpty();
    }

    [Test]
    public async Task The_api_template_registers_the_demo_key_only_in_Development()
    {
        (await SchemeNames<Api.Data.AppDbContext>("Production")).Should().BeEmpty(Adr);
        (await SchemeNames<Api.Data.AppDbContext>("Development")).Should().NotBeEmpty();
    }

    private static async Task<List<string>> SchemeNames<T>(string environment)
        where T : class
    {
        await using var factory = Start<T>(environment);
        var schemes = await factory
            .Services.GetRequiredService<IAuthenticationSchemeProvider>()
            .GetAllSchemesAsync();
        return schemes.Select(s => s.Name).ToList();
    }
}
