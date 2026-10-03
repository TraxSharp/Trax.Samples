using Trax.Samples.Bookworm.Auth;
using Trax.Samples.Bookworm.E2E.Fixtures;
using Trax.Samples.Bookworm.E2E.Utilities;

namespace Trax.Samples.Bookworm.E2E.ApiTests;

/// <summary>
/// Verifies that the <c>[TraxAuthorize(Roles = Member)]</c> gate on the borrow mutation is enforced
/// through the real HTTP + auth pipeline.
/// </summary>
[TestFixture]
public class AuthTests : ApiTestFixture
{
    [Test]
    public async Task BorrowBook_Anonymous_IsRejected()
    {
        var doc = await GraphQL.PostAsync(Borrow(await NewBookAsync()));

        GraphQLClient
            .HasErrors(doc)
            .Should()
            .BeTrue("an unauthenticated caller must not be able to borrow a book");
    }

    [Test]
    public async Task BorrowBook_Librarian_IsRejected()
    {
        // Borrowing is for members: the librarian key holds only the Librarian role.
        var doc = await GraphQL.PostAsync(
            Borrow(await NewBookAsync()),
            ApiKeyDefaults.LibrarianKey
        );

        GraphQLClient.HasErrors(doc).Should().BeTrue();
    }

    [Test]
    public async Task BorrowBook_AuthenticatedMember_Succeeds()
    {
        var loanId = await BorrowAsync(await NewBookAsync(), ApiKeyDefaults.MemberKey);

        loanId.Should().BePositive("a member is authorized to borrow a book");
    }
}
