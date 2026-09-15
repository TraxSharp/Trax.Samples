using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Types;
using Trax.Samples.GameServer.Data.Models;

namespace Trax.Samples.GameServer.Api.TypeExtensions;

/// <summary>
/// Extends the <see cref="PlayerRecord"/> GraphQL type with a computed <c>winRate</c>
/// field. Demonstrates how <c>[ExtendObjectType]</c> classes can be auto-registered
/// via <c>AddTypeExtensions(assembly)</c> on the <c>TraxGraphQLBuilder</c>.
/// </summary>
/// <remarks>
/// <c>PlayerRecord</c> is <c>[TraxAllowAnonymous]</c>, so a field bolted onto it inherits no
/// gate and has to declare one. Without the attribute below the host refuses to start: a
/// projection off a public entity is not automatically as public as the entity, and Trax makes
/// somebody say which it is. Here the answer is genuinely public, because the field is computed
/// from <c>Wins</c> and <c>Losses</c>, which the same anonymous caller can already read.
/// Note that <c>[TraxAuthorize]</c> does not compile on a resolver method; HotChocolate's
/// <c>[Authorize]</c> and <c>[AllowAnonymous]</c> are the ones that work here.
/// </remarks>
[ExtendObjectType(typeof(PlayerRecord))]
public class PlayerRecordTypeExtension
{
    [AllowAnonymous]
    public double GetWinRate([Parent] PlayerRecord player)
    {
        var total = player.Wins + player.Losses;
        return total > 0 ? (double)player.Wins / total : 0;
    }
}
