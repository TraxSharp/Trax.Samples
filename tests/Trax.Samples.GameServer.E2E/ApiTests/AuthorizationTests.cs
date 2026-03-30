using Trax.Samples.GameServer.E2E.Fixtures;

namespace Trax.Samples.GameServer.E2E.ApiTests;

[TestFixture]
public class AuthorizationTests : ApiTestFixture
{
    [Test]
    public async Task BanPlayer_WithPlayerKey_ReturnsAuthorizationError()
    {
        var result = await GraphQL.SendAsync(
            """
            mutation {
                dispatch {
                    players {
                        banPlayer(
                            input: { playerId: "player-8", reason: "unauthorized attempt" }
                        ) {
                            externalId
                        }
                    }
                }
            }
            """,
            apiKey: PlayerKey
        );

        // BanPlayer requires Admin role — player key should be rejected.
        result.HasErrors.Should().BeTrue();
        result.FirstErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task BanPlayer_WithAdminKey_Succeeds()
    {
        var result = await GraphQL.SendAsync(
            """
            mutation {
                dispatch {
                    players {
                        banPlayer(
                            input: { playerId: "player-7", reason: "admin ban test" }
                        ) {
                            externalId
                        }
                    }
                }
            }
            """,
            apiKey: AdminKey
        );

        // Admin key has the Admin role — should succeed.
        result.HasErrors.Should().BeFalse($"GraphQL error: {result.FirstErrorMessage}");
    }
}
