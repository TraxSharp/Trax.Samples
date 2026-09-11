---
authors: [Theauxm]
areas: [testing, samples]
status: accepted
---

# The samples adopt the shipped guards the way a consumer would

`BookwormArchitectureGuards` contains no test bodies. It subclasses the abstract fixtures
published in `Trax.Effect.Data.Testing`, `Trax.Api.GraphQL.Testing` and
`Trax.Mediator.Testing`, supplies configuration, and lets the inherited `[Test]` methods be
discovered in this assembly. That is exactly the adoption path a consumer follows.

## Status

**Accepted.**

## Why this is written down

Because it is the only place the consumer-facing half of the guard packages is exercised.
Each package has its own self-tests, which prove the checkers work when driven directly.
None of them proves the thing a consumer actually does: reference the package, subclass a
fixture, supply options, and have `dotnet test` discover the inherited tests.

If fixture discovery broke, or an abstract member changed shape, every self-test would stay
green and every consumer's build would break. Bookworm is what catches that.

## Consequences

**The flagship sample carries a test project that exists for the framework's benefit**, not
to demonstrate anything to a reader of the sample. That is a slightly odd thing for a sample
to do, and it is deliberate.

**A guard package without a Bookworm subclass is untested from the outside.** Adding a new
`*GuardFixture` to any repo means adding its subclass here, or the adoption path for that one
is unproven.

**Bookworm's own architecture is therefore constrained** by the guards it adopts: one schema
per context, companion interfaces, cross-schema edges through the manifest. That is the
point, since a sample that could not satisfy them would be a poor demonstration.

## Exemplars

**Enforced elsewhere:** the `[Test]` methods live in the packages, not here.
`DataLayerGuards` and `DomainDataLayerGuardFixture` ship from `Trax.Effect`,
`CrossSchemaGuards` and `CrossSchemaGuardFixture` from `Trax.Api`, and `TrainGuards` with
`TrainGuardFixture` from `Trax.Mediator`. `BookwormArchitectureGuards.cs` in this repo is
three subclasses and a configuration block.

Not covered: nothing checks that every shipped `*GuardFixture` has a subclass here. A new
fixture added upstream with no Bookworm adopter is untested from the consumer side, and
nothing says so.

## Changelog

- **2026-09-11**: Recorded.
