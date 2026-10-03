# State Machine sample

A GraphQL host over two portable snapshot state machines, authored with the fluent API and driven through
the four generic `stateMachine` mutations. It shows the whole feature end to end: fluent authoring,
one-line discovery, server-side authority (the server, not the browser, decides what a valid draft is,
down to the checkout total), and an exactly-once effect.

| Machine | Shape | Notes |
|---|---|---|
| `turnstile` | `Locked ⇄ Unlocked` | No effect, no committed state. The structure proof. |
| `checkout` | `Cart → Review → Paid` (v2) | `Paid` is committed; `Pay` runs one irreversible charge exactly once. v2 adds a `total` that every state requires to equal 999 cents per item, backfilled from v1 drafts by a forward migration (see below). |

Both live in `Trax.Samples.StateMachine` as `Machine<TState, TTrigger>` subclasses. The host
(`Trax.Samples.StateMachine.Api`) wires them with one line:

```csharp
builder.Services.AddTrax(trax =>
    trax.AddEffects(effects => effects.UsePostgres(connectionString).AddJson())
        .AddStateMachines(typeof(TurnstileMachine).Assembly)   // before AddMediator
        .AddMediator(typeof(TurnstileMachine).Assembly));

builder.Services.AddScoped<ISnapshotPrincipal, TraxCallerSnapshotPrincipal>();
builder.Services.AddScoped<ICharge, LoggingCharge>();
```

`AddStateMachines` discovers the machines and wires the store, the effect-claim ledger, the
exactly-once runner, the registry and the four mutations. It must come before `AddMediator`. The host
supplies only the two things a machine can't know: how to map its auth to a user key
(`ISnapshotPrincipal`) and the charge implementation.

## Run it

From `Trax.Samples/`:

```bash
docker compose up -d                             # Postgres
dotnet run --project samples/StateMachine/Trax.Samples.StateMachine.Api
```

Open http://localhost:5220/trax/graphql and send `X-Api-Key: alice-key-do-not-use-in-production`.

```graphql
# What machines are available?
{ discover { stateMachine { listMachines { machines { name hasEffect } } } } }

# Save a checkout draft (use one UUID id throughout), then charge it exactly once.
mutation {
  dispatch { stateMachine { saveSnapshot(input: {
    machine: "checkout",
    id: "11111111-1111-1111-1111-111111111111",
    snapshot: "{\"machine\":\"checkout\",\"version\":2,\"state\":\"Review\",\"context\":{\"items\":[\"book\"],\"receipt\":null,\"total\":999}}"
  }) { output { snapshot problem { code } } } } }
}

mutation {
  dispatch { stateMachine { sendSnapshot(input: {
    machine: "checkout", id: "11111111-1111-1111-1111-111111111111", requestId: "pay-1"
  }) { output { snapshot problem { code } } } } }
}
```

The second `sendSnapshot` returns the same `Paid` snapshot and does not charge again. Save the same draft
with `"items":["book","pen"]` and `"total":1` and the answer is `problem { code: "invalid-context" }`:
the total is the amount a real `ICharge` would take, so the machine refuses a total that is not 999 cents
per item, and the charge reads the amount from the server's stored copy (`CheckoutMachine.AmountCents`).

## Schema evolution (v1 → v2)

`checkout` is version 2: its context is `{ items, receipt, total }`, and every state requires `total` to be a
number. A draft written by the old v1 host has no `total`, so it would fail v2 validation. The machine
declares a forward migration inline, so a v1 draft is upgraded on rehydrate:

```csharp
m.Id("checkout").Version(2).StartsAt(CheckoutState.Cart, Fresh)
    .MigrateFrom(1, (state, ctx) =>
    {
        var next = (JsonObject)ctx.DeepClone();
        next["total"] = ItemsCount(ctx) * UnitPriceCents; // backfill from the item count
        return new MigrationResult(state, next);
    });
```

Load a stored v1 draft and the server returns it at v2 with `total` filled in; there is no manual step. That
migration is guarded by tests in `Trax.Samples.StateMachine.Tests` (a v1 draft loads as v2, and a v2 context
missing `total` is rejected, which is what makes the migration load-bearing).

## Web frontend

A React app in [web/](web/) drives both machines through these mutations with a machine-agnostic transport
and a `useMachine` hook. Start this host, then `cd web && npm install && npm run dev` and open
http://localhost:5173.

## Notes

- Anonymous requests get a `TRAX_AUTHORIZATION` error at HTTP 200, not a crash. The four mutations carry
  `[TraxAuthorize]`; `listMachines` is anonymous so you can inspect the machines without a key.
- The `snapshot_draft` and `effect_claim` tables are created by the Trax Postgres provider's own
  migrations when the host starts; the sample writes no schema of its own.

Docs: <https://traxsharp.net/docs/samples/state-machine>
