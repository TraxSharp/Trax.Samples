# Trax Chat Service Sample

A chat server whose messages arrive over GraphQL subscriptions. Chat mutations are Trax trains;
when one completes, a lifecycle hook publishes its output to a room-scoped topic, and every
participant subscribed with `onChatEvent(chatRoomId:)` receives it over a WebSocket.

## What it proves

- A custom subscription field on Trax's subscription root, `LifecycleSubscriptions`, fed by an
  `ITrainLifecycleHook` that sends to a HotChocolate topic when a chat train completes.
- Subscription authentication: the API key travels in the `connection_init` payload, and a socket
  without one is closed with `4403`.
- Per-subscriber authorization: `[TraxAuthorize]` on the field, plus a subscribe resolver that
  admits only the room's participants.
- Caller identity: every train is `[TraxAuthorize(Roles = "User")]` and reads the caller from
  `TraxPrincipal`. No input names a user, so nobody can act as somebody else.

## Run

No Docker: Trax metadata and chat data are both SQLite files next to the API.

```bash
# The API, in Development, on http://localhost:5210
dotnet run --project samples/ChatService/Trax.Samples.ChatService.Api

# Optional: the React client on http://localhost:5173
cd samples/ChatService/Trax.Samples.ChatService.Client
npm ci
npm run dev
```

Open two browser tabs on the client, pick Alice in one and Bob in the other, and chat.

## Try it

The demo keys exist only in Development: `alice-key-do-not-use-in-production`,
`bob-key-do-not-use-in-production`, `charlie-key-do-not-use-in-production`.

```bash
G=http://localhost:5210/trax/graphql

# 1. Alice creates a room (note the chatRoomId)
curl -s $G -H 'Content-Type: application/json' -H 'X-Api-Key: alice-key-do-not-use-in-production' \
  -d '{"query":"mutation { dispatch { createChatRoom(input: { name: \"General\" }) { output { chatRoomId name } } } }"}'

ROOM=<chatRoomId>

# 2. Bob joins it
curl -s $G -H 'Content-Type: application/json' -H 'X-Api-Key: bob-key-do-not-use-in-production' \
  -d "{\"query\":\"mutation { dispatch { joinChatRoom(input: { chatRoomId: \\\"$ROOM\\\" }) { output { userId displayName } } } }\"}"
# {"data":{"dispatch":{"joinChatRoom":{"output":{"userId":"TraxApiKey:bob","displayName":"Bob"}}}}}
```

3. Subscribe as Bob with any `graphql-ws` client, sending the key in the `connection_init`
   payload as `apiKey`. From Node, with the `graphql-ws` package the React client installs, save
   this as `subscribe.mjs` in the client folder and run `node subscribe.mjs $ROOM`:

   ```js
   import { createClient } from "graphql-ws";
   const client = createClient({
     url: "ws://localhost:5210/trax/graphql",
     connectionParams: { apiKey: "bob-key-do-not-use-in-production" },
   });
   client.subscribe(
     { query: `subscription { onChatEvent(chatRoomId: "${process.argv[2]}") { eventType payload } }` },
     { next: (m) => console.log(JSON.stringify(m)), error: console.error, complete: () => {} },
   );
   ```

```bash
# 4. Alice sends a message: Bob's subscription receives a MessageSent event
curl -s $G -H 'Content-Type: application/json' -H 'X-Api-Key: alice-key-do-not-use-in-production' \
  -d "{\"query\":\"mutation { dispatch { sendMessage(input: { chatRoomId: \\\"$ROOM\\\", content: \\\"Hello!\\\" }) { output { messageId senderUserId content } } } }\"}"

# 5. Charlie never joined: his history read is refused, and so is his subscription
curl -s $G -H 'Content-Type: application/json' -H 'X-Api-Key: charlie-key-do-not-use-in-production' \
  -d "{\"query\":\"{ discover { getChatHistory(input: { chatRoomId: \\\"$ROOM\\\" }) { messages { content } } } }\"}"
# "You are not a participant in room ..."

# 6. Bob's rooms
curl -s $G -H 'Content-Type: application/json' -H 'X-Api-Key: bob-key-do-not-use-in-production' \
  -d '{"query":"{ discover { getChatRooms { rooms { id name participantCount lastMessageAt } } } }"}'
```

## Tests

```bash
dotnet test tests/Trax.Samples.ChatService.Tests   # junctions and the hook, in-memory
dotnet test tests/Trax.Samples.ChatService.E2E     # the real host over HTTP and WebSocket, SQLite
```

## Docs

[Chat Service sample](https://traxsharp.net/docs/samples/chat-service) and
[Subscriptions](https://traxsharp.net/docs/sdk-reference/graphql-api/subscriptions).

> NO WARRANTY. The demo keys are plaintext constants for demonstration only; they are registered
> only in Development, and Trax refuses to start with them anywhere else.
