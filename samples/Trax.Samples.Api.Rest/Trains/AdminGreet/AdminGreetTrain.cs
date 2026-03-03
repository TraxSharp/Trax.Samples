using LanguageExt;
using Trax.Effect.Attributes;
using Trax.Effect.Services.ServiceTrain;

namespace Trax.Samples.Api.Rest.Trains.AdminGreet;

/// <summary>
/// Same logic as GreetTrain, but requires the "Admin" authorization policy.
/// Calling this train through the API without a valid Admin user returns 403.
/// </summary>
[TraxAuthorize("Admin")]
public class AdminGreetTrain : ServiceTrain<AdminGreetInput, Unit>, IAdminGreetTrain
{
    protected override async Task<Either<Exception, Unit>> RunInternal(AdminGreetInput input)
    {
        Console.WriteLine($"[AdminGreetTrain] Hello, {input.Name}! (admin-only)");
        return Unit.Default;
    }
}
