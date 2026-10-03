using System.Text.Json;
using Trax.Samples.ChatService.E2E.Fixtures;
using Trax.Samples.ChatService.E2E.Utilities;

namespace Trax.Samples.ChatService.E2E.ChatApiTests;

/// <summary>
/// The sample's headline feature: <c>subscription { onChatEvent(chatRoomId:) }</c> delivers a
/// room's new messages over the WebSocket to the room's participants, and to nobody else.
/// </summary>
[TestFixture]
public class ChatEventSubscriptionTests : ChatApiTestFixture
{
    private static string OnChatEvent(string roomId) =>
        $$"""
            subscription { onChatEvent(chatRoomId: "{{roomId}}") { chatRoomId eventType payload } }
            """;

    private static async Task<GraphQLWebSocketClient> ConnectAsync(string? apiKey) =>
        await GraphQLWebSocketClient.ConnectAsync(
            SharedChatApiSetup.Factory.Server.CreateWebSocketClient(),
            apiKey: apiKey
        );

    /// <summary>
    /// Sends messages as <paramref name="senderKey"/> until <paramref name="sub"/> receives one, and
    /// returns the event. Retrying covers the moment between the subscribe message and the server
    /// registering it, without a fixed delay.
    /// </summary>
    private async Task<JsonElement> SendUntilReceivedAsync(
        GraphQLWebSocketClient sub,
        string senderKey,
        string roomId,
        string content
    )
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var sent = await SendMessageAsAsync(senderKey, roomId, $"{content} {attempt}");
            sent.HasErrors.Should().BeFalse(sent.FirstErrorMessage);
            try
            {
                return (await sub.ReceiveNextAsync(TimeSpan.FromSeconds(1)))
                    .GetProperty("data")
                    .GetProperty("onChatEvent");
            }
            catch (TimeoutException)
            {
                // Not registered yet: send again.
            }
        }
        throw new AssertionException("no onChatEvent arrived");
    }

    /// <summary>What the server answered a refused subscription with: a protocol message, or a close.</summary>
    private static async Task<string> ReceiveRefusalAsync(GraphQLWebSocketClient sub)
    {
        try
        {
            var message = await sub.ReceiveAnyAsync();
            message.GetProperty("type").GetString().Should().NotBe("complete");
            return message.GetRawText();
        }
        catch (WebSocketClosedException closed)
        {
            return $"closed {(int?)closed.Status} {closed.Description}";
        }
    }

    [Test]
    public async Task OnChatEvent_is_in_the_served_schema()
    {
        var response = await GraphQL.SendAsync(
            """{ __schema { subscriptionType { name fields { name } } } }"""
        );

        var subscriptionType = response.GetData("__schema", "subscriptionType");
        subscriptionType.GetProperty("name").GetString().Should().Be("LifecycleSubscriptions");
        subscriptionType
            .GetProperty("fields")
            .EnumerateArray()
            .Select(f => f.GetProperty("name").GetString())
            .Should()
            .Contain("onChatEvent")
            .And.NotContain(
                "subscribeToChatEventAsync",
                "the subscribe resolver is not a field of its own"
            );
    }

    [Test]
    public async Task A_participant_receives_onChatEvent_for_a_new_message()
    {
        var roomId = await CreateRoomAsAsync(AliceKey, "Live");

        await using var sub = await ConnectAsync(AliceKey);
        await sub.SubscribeAsync("room-1", OnChatEvent(roomId));

        var chatEvent = await SendUntilReceivedAsync(sub, AliceKey, roomId, "hello");

        chatEvent.GetProperty("chatRoomId").GetString().Should().Be(roomId);
        chatEvent.GetProperty("eventType").GetString().Should().Be("MessageSent");
        chatEvent.GetProperty("payload").GetString().Should().Contain("hello");
    }

    [Test]
    public async Task Another_participant_receives_the_message_with_the_senders_identity()
    {
        var roomId = await CreateRoomAsAsync(AliceKey, "Pair");
        await JoinRoomAsAsync(BobKey, roomId);

        await using var bob = await ConnectAsync(BobKey);
        await bob.SubscribeAsync("bob-1", OnChatEvent(roomId));

        var chatEvent = await SendUntilReceivedAsync(bob, AliceKey, roomId, "hi bob");

        using var payload = JsonDocument.Parse(chatEvent.GetProperty("payload").GetString()!);
        payload
            .RootElement.GetProperty("senderUserId")
            .GetString()
            .Should()
            .Be("TraxApiKey:alice", "the sender is the authenticated caller");
        payload.RootElement.GetProperty("senderDisplayName").GetString().Should().Be("Alice");
    }

    [Test]
    public async Task A_subscriber_receives_nothing_from_another_room()
    {
        var watched = await CreateRoomAsAsync(AliceKey, "Watched");
        var other = await CreateRoomAsAsync(AliceKey, "Other");

        await using var sub = await ConnectAsync(AliceKey);
        await sub.SubscribeAsync("watched-1", OnChatEvent(watched));
        await SendUntilReceivedAsync(sub, AliceKey, watched, "warm-up");

        // A message in the other room, then one in the watched room: the next event must be the
        // watched room's, because the other room's would arrive first.
        (await SendMessageAsAsync(AliceKey, other, "elsewhere"))
            .HasErrors.Should()
            .BeFalse();
        (await SendMessageAsAsync(AliceKey, watched, "here")).HasErrors.Should().BeFalse();

        var next = (await sub.ReceiveNextAsync(TimeSpan.FromSeconds(10)))
            .GetProperty("data")
            .GetProperty("onChatEvent");
        next.GetProperty("chatRoomId").GetString().Should().Be(watched);
        next.GetProperty("payload").GetString().Should().Contain("here");
    }

    [Test]
    public async Task A_non_participant_is_refused_when_subscribing()
    {
        var roomId = await CreateRoomAsAsync(AliceKey, "Private");

        await using var charlie = await ConnectAsync(CharlieKey);
        await charlie.SubscribeAsync("charlie-1", OnChatEvent(roomId));

        var refusal = await ReceiveRefusalAsync(charlie);
        refusal.Should().Contain("Not authorized.");
    }

    [Test]
    public async Task An_anonymous_socket_is_refused_at_connection_init()
    {
        // With a token scheme registered, every socket must carry a credential in connection_init;
        // one without is closed with 4403 before it can subscribe to anything.
        var connect = () => ConnectAsync(apiKey: null);

        var closed = (await connect.Should().ThrowAsync<WebSocketClosedException>()).Which;
        ((int?)closed.Status).Should().Be(4403);
    }
}
