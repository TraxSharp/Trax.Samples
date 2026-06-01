using Microsoft.Extensions.DependencyInjection;
using Trax.Api.Auth.ApiKey;

namespace Trax.Samples.Shared.Api.Auth;

/// <summary>
/// One-call API-key auth wiring shared by the samples, so each host stops hand-rolling the
/// <c>AddTraxApiKeyAuth</c> + <c>AddAuthorization</c> pair (and its drift) on its own.
/// </summary>
/// <remarks>
/// NO WARRANTY. Sample auth is plumbing for demonstration, not a security product. The keys passed
/// in are plaintext demo keys; see <see cref="SampleAuth.DemoKeySuffix"/>.
/// </remarks>
public static class SampleApiKeyAuthExtensions
{
    /// <summary>
    /// Registers the Trax API-key scheme with the supplied demo keys and the default authorization
    /// services in one call. Keys are configured via the standard <see cref="ApiKeyBuilder"/>.
    /// </summary>
    public static IServiceCollection AddSampleApiKeyAuth(
        this IServiceCollection services,
        Action<ApiKeyBuilder> configureKeys
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureKeys);

        services.AddTraxApiKeyAuth(configureKeys);
        services.AddAuthorization();
        return services;
    }
}
