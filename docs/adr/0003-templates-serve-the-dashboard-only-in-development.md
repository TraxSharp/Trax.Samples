---
authors: [Theauxm]
areas: [samples]
status: accepted
---

# The templates serve the dashboard and the demo key only in Development

The `trax-hub` and `trax-scheduler` templates mount the dashboard, and the `trax-hub` and
`trax-api` templates register the plaintext demo API key, only when
`IHostEnvironment.IsDevelopment()`. `dotnet run` starts in Development through each
template's `launchSettings.json`, so the out-of-the-box experience is unchanged; a scaffold
started any other way has no dashboard and no credential until its author adds real ones.

## Status

**Accepted.**

## Considered options

**Gate the template's dashboard with the demo key.** It would keep the dashboard in every
environment, but the key is published in the template itself, so a gate that accepts it is no
gate. A scaffold is started as-is more often than it is read.

**Ship real authentication in the template.** A user store, a login page and a scheme are the
host's choice, and any one the template picked would be the wrong one for most scaffolds. The
templates stay minimal and say in `Program.cs` where the gate goes.

**Leave it, and rely on `UseTraxDashboard()` refusing to start without a posture**
(dashboard/0002). That covers the dashboard once the templates move to that release, not the
demo key, and not the templates already published against earlier Trax.Dashboard versions.
The environment check works against every Trax.Dashboard version, so it does not wait for a
release.

## Consequences

A scaffold run with `ASPNETCORE_ENVIRONMENT` unset (a container, `dotnet Hub.dll`) starts in
Production, and `/trax` is a 404 there. That is the intended surprise: the fix is a posture in
`Program.cs`, not a different environment variable.

`trax generate` scaffolds from `trax-hub`, so a generated project inherits this.

## Exemplars

- `TemplateEnvironmentTests` starts each template host in Production and in Development, and
  pins that `/trax` is served only in Development and that the demo key's scheme is registered
  only there.

Not covered: the tests build the templates from this repository against its central package
pins, not a project scaffolded from the published `Trax.Samples.Templates` package.

## Changelog

- **2026-09-27**: Recorded.
