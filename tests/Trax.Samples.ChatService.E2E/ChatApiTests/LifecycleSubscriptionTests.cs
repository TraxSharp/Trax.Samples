using Trax.Samples.ChatService.E2E.Fixtures;
using Trax.Samples.ChatService.E2E.Utilities;

namespace Trax.Samples.ChatService.E2E.ChatApiTests;

/// <summary>
/// Trax's own lifecycle subscriptions on the same socket as <c>onChatEvent</c>: a train marked
/// <c>[TraxBroadcast]</c> emits <c>onTrainCompleted</c> to a caller its posture admits, and a train
/// without the marker emits nothing. <c>ChatEventSubscriptionTests</c> covers the chat feed itself.
/// </summary>
[TestFixture]
public class LifecycleSubscriptionTests : ChatApiTestFixture
{
    private async Task<string> CreateRoom()
    {
        var result = await GraphQL.SendAsync(
            """
            mutation {
                dispatch {
                    createChatRoom(
                        input: { name: "Sub Test Room" }
                    ) {
                        output { chatRoomId }
                    }
                }
            }
            """,
            apiKey: AliceKey
        );

        result.HasErrors.Should().BeFalse();
        return result
            .GetData("dispatch", "createChatRoom", "output")
            .GetProperty("chatRoomId")
            .GetString()!;
    }

    [Test]
    public async Task OnTrainCompleted_ReceivesEventForSendMessage()
    {
        var chatRoomId = await CreateRoom();

        var wsClient = SharedChatApiSetup.Factory.Server.CreateWebSocketClient();
        await using var sub = await GraphQLWebSocketClient.ConnectAsync(wsClient, apiKey: AliceKey);

        await sub.SubscribeAsync(
            "completed-1",
            """
            subscription {
                onTrainCompleted {
                    metadataId
                    trainName
                    trainState
                }
            }
            """
        );

        // Send a message (SendMessage has [TraxBroadcast]).
        var result = await GraphQL.SendAsync(
            $$"""
            mutation {
                dispatch {
                    sendMessage(
                        input: { chatRoomId: "{{chatRoomId}}", content: "Sub test!" }
                    ) {
                        externalId
                    }
                }
            }
            """,
            apiKey: AliceKey
        );

        result.HasErrors.Should().BeFalse();

        var payload = await sub.ReceiveNextAsync(TimeSpan.FromSeconds(10));
        var data = payload.GetProperty("data").GetProperty("onTrainCompleted");

        data.GetProperty("trainName").GetString().Should().Contain("SendMessage");
        data.GetProperty("trainState").GetString().Should().Be("COMPLETED");
    }

    [Test]
    public async Task OnTrainCompleted_ReceivesEventForCreateChatRoom()
    {
        var wsClient = SharedChatApiSetup.Factory.Server.CreateWebSocketClient();
        await using var sub = await GraphQLWebSocketClient.ConnectAsync(wsClient, apiKey: AliceKey);

        await sub.SubscribeAsync(
            "completed-2",
            """
            subscription {
                onTrainCompleted {
                    metadataId
                    trainName
                }
            }
            """
        );

        // Create a room (CreateChatRoom has [TraxBroadcast]).
        var result = await GraphQL.SendAsync(
            """
            mutation {
                dispatch {
                    createChatRoom(
                        input: { name: "Sub Room" }
                    ) {
                        externalId
                    }
                }
            }
            """,
            apiKey: AliceKey
        );

        result.HasErrors.Should().BeFalse();

        var payload = await sub.ReceiveNextAsync(TimeSpan.FromSeconds(10));
        var data = payload.GetProperty("data").GetProperty("onTrainCompleted");

        data.GetProperty("trainName").GetString().Should().Contain("CreateChatRoom");
    }

    [Test]
    public async Task A_train_without_TraxBroadcast_emits_no_lifecycle_event()
    {
        var chatRoomId = await CreateRoom();

        var wsClient = SharedChatApiSetup.Factory.Server.CreateWebSocketClient();
        await using var sub = await GraphQLWebSocketClient.ConnectAsync(wsClient, apiKey: AliceKey);
        await sub.SubscribeAsync("no-event-1", "subscription { onTrainCompleted { trainName } }");

        // Prove the subscription is live first: keep sending a broadcast message until one arrives.
        await SendUntilReceivedAsync(sub, chatRoomId);

        // GetChatHistory has no [TraxBroadcast]. Run it, then a broadcast SendMessage: the next
        // event must be SendMessage's, because an event from the query would arrive before it.
        var history = await GraphQL.SendAsync(
            $$"""
            { discover { getChatHistory(input: { chatRoomId: "{{chatRoomId}}" }) { messages { content } } } }
            """,
            apiKey: AliceKey
        );
        history.HasErrors.Should().BeFalse(history.FirstErrorMessage);
        await SendMessageAsync(chatRoomId, "after the query");

        var next = await sub.ReceiveNextAsync(TimeSpan.FromSeconds(10));
        next.GetProperty("data")
            .GetProperty("onTrainCompleted")
            .GetProperty("trainName")
            .GetString()
            .Should()
            .Contain("SendMessage", "GetChatHistory does not have [TraxBroadcast]");
    }

    private async Task SendMessageAsync(string chatRoomId, string content)
    {
        var sent = await GraphQL.SendAsync(
            $$"""
            mutation { dispatch { sendMessage(input: { chatRoomId: "{{chatRoomId}}", content: "{{content}}" }) { externalId } } }
            """,
            apiKey: AliceKey
        );
        sent.HasErrors.Should().BeFalse(sent.FirstErrorMessage);
    }

    private async Task SendUntilReceivedAsync(GraphQLWebSocketClient sub, string chatRoomId)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            await SendMessageAsync(chatRoomId, $"warm-up {attempt}");
            if (await sub.TryReceiveNextAsync(TimeSpan.FromSeconds(1)))
                return;
        }
        Assert.Fail("the subscription never delivered an event");
    }
}
