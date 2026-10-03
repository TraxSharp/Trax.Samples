using LanguageExt;
using Trax.Effect.Attributes;
using Trax.Effect.Services.ServiceTrain;
using Trax.Samples.Bookworm.Auth;
using Trax.Samples.Bookworm.Trains.Lending.BorrowBook.Junctions;

namespace Trax.Samples.Bookworm.Trains.Lending.BorrowBook;

/// <summary>Lends a book to the calling member. A write operation, gated to members.</summary>
[TraxAuthorize(Roles = BookwormRoles.Member)]
[TraxMutation(
    Namespace = GraphQLNamespaces.Lending,
    Description = "Borrows a book for the calling member"
)]
public class BorrowBookTrain : ServiceTrain<BorrowBookInput, BorrowBookOutput>, IBorrowBookTrain
{
    protected override Task<Either<Exception, BorrowBookOutput>> Junctions() =>
        Chain<BorrowBookJunction>().Resolve();
}
