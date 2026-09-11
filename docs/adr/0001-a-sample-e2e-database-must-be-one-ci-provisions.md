---
authors: [Theauxm]
areas: [testing, ci]
status: accepted
---

# A sample's E2E database must be one CI actually provisions

Every sample E2E factory boots a host against PostgreSQL, and CI provisions those databases
up front: a service container on a fixed host port, plus an explicit create-databases list.
A factory's default connection string must name a port and a database from that list.

## Status

**Accepted.**

## Why this is written down

Because getting it wrong does not fail. It reports success.

A factory pointing at a database CI does not provision either fails to connect, which is
loud and fine, or it skips, which is not. The Bookworm suite once hid a zero-coverage gap
behind a local-only port 5433: the tests were green, the build was green, and nothing ran.
A suite that silently tests nothing is worse than no suite, because it is counted.

## Considered options

**Let each sample provision its own database in a fixture.** More self-contained, and it
moves the failure from "cannot connect" to "created a database nobody expected", which is
just as silent. It also multiplies the setup cost across a dozen samples.

**Point everything at one database.** Simplest, and rejected because the samples are
deliberately different topologies (`LocalWorkers`, `DistributedWorkers`, `EphemeralWorkers`)
and sharing one schema between them couples samples that are meant to demonstrate
independence.

## Consequences

**The CI workflow is the source of truth, and the guard reads it.** It parses the host ports
mapped onto the service container and the databases created in the setup loop, then checks
every factory against that. Changing the workflow and forgetting a sample fails, and so does
the reverse.

**A new sample needs a line in the workflow before its suite means anything.** That is the
intended friction: the alternative is a suite that looks like coverage and is not.

## Exemplars

- `E2EDatabaseProvisioningTests` parses `.github/workflows/pull_request.yml` for the
  provisioned ports and databases and pins every sample factory's default connection string
  to them, so the two cannot drift apart unnoticed.

Not covered: the guard checks the connection string a factory *defaults* to. A suite that
overrides it at run time, or reads it from configuration, is outside what the regex sees.

## Changelog

- **2026-09-11**: Recorded.
