# Trax Recovery Sample

A live demo of a train recovering from a crash without asking its model again. A train asks a
decider (a stand-in model) which track to take, a later step crashes, and the manifest's automatic
retry takes the same tracks by replaying the recorded answers. Every junction, question and track
reaches the page as it happens, through junction events.

It proves three Trax features working together: [train decisions](https://traxsharp.net/docs/core/decisions),
[retries that replay decisions](https://traxsharp.net/docs/scheduler/dead-letters-and-cleanup#retries-replay-decisions)
and [junction events](https://traxsharp.net/docs/effect/junction-events). The full walkthrough is on
[traxsharp.net/docs/samples/recovery](https://traxsharp.net/docs/samples/recovery).

```
Recovery/
├── Trax.Samples.Recovery/          Trains, junctions, the demo decider, the fault injector
├── Trax.Samples.Recovery.Api/      One host: GraphQL + scheduler + local workers + dashboard
└── Trax.Samples.Recovery.Client/   React 19 + Vite + Apollo + graphql-ws page
```

## Run it

Needs the .NET 10 SDK, Node.js 20 or later, and Docker. From the `Trax.Samples` folder:

```bash
docker compose up -d database
dotnet run --project samples/Recovery/Trax.Samples.Recovery.Api
```

In a second terminal:

```bash
cd samples/Recovery/Trax.Samples.Recovery.Client
npm ci
npm run dev
```

Open http://localhost:5173. The host listens on http://localhost:5260; the dashboard is at
http://localhost:5260/trax and the GraphQL IDE at http://localhost:5260/trax/graphql, both in
Development only. When your Postgres is not on 5432, start the host with
`ConnectionStrings__TraxDatabase="Host=localhost;Port=<port>;Database=trax;Username=trax;Password=trax123"`.

## Try it

1. Leave **Research agent** and **Crash once** selected and press **Run**. Attempt 1 plans, asks the
   model where to look (`Source`), searches, asks how deep to go (`Depth`), and crashes in the second
   tool call. A few seconds later the manifest retries: attempt 2 shows
   **replayed: model not asked** on both questions, takes the same tracks and completes.
2. Press **Run** again and, while attempt 1's failure waits out its backoff, press **Ask afresh**.
   The retry asks the model both questions again (**asked afresh: on purpose**).
3. Pick **Refund approval** (order A-1001), press **Run**, and during the backoff press
   **Change the data during the backoff**. The order now shows an earlier refund, so the retry's
   state no longer hashes the same: the replay is refused, the model is asked afresh
   (**asked afresh: state changed**) and the refund goes to a person (the `Unsure` track) instead of
   being paid.
4. After a run completes, **Ask afresh** re-queues its last execution with
   `requeueExecution(id, askAfresh: true)`, as a run of its own.

The same over plain GraphQL, with header `X-Api-Key: recovery-operator-key-do-not-use-in-production`:

```graphql
mutation { dispatch { startRun(input: { scenario: REFUND, orderId: "A-1001", crashOnce: true }) {
  output { runId manifestId manifestExternalId } } } }

query { operations { executions(manifestId: 1, order: OLDEST) { items { id trainState failureJunction } } } }

query { operations { junctionRuns(metadataId: 42) { position kind name state answer replayed attempt } } }
```

## Keys

Two demo keys, registered only in Development (a key carrying `do-not-use-in-production` refuses to
start anywhere else):

| Key | Role | Sees |
|---|---|---|
| `recovery-operator-key-do-not-use-in-production` | `Operator` | The operations view: every answer and every step name. The page uses it. |
| `recovery-viewer-key-do-not-use-in-production` | `Viewer` | The broadcast view of the two scenario trains: the run's shape, with answers and the steps on a track withheld. |

## Demo-only settings

The scheduler polls every second and retries after four seconds with no backoff growth, so a retry
is visible within seconds. `appsettings.Development.json` holds a fixed state hash key. Neither
belongs in production: keep the scheduler defaults, and keep the key with your other secrets.

## Using Nimble

Set `Recovery:Model` to `Nimble` and `Recovery:Nimble:Endpoint` to the full URL of a Nimble server's
`POST /v1/systemone` (one you run; Trax has no default). The demo decider is then not registered.

## Tests

`tests/Trax.Samples.Recovery.E2E` runs the real host against Postgres (database
`recovery_e2e_tests`) with a counting decider:

```bash
TRAX_TEST_PG_PORT=5432 dotnet test tests/Trax.Samples.Recovery.E2E
```
