# 8. Three tiers, and a worker host of its own

Status: accepted, 2026-11-05. Supersedes [ADR 1](0001-temporal-orchestrates-fulfilment.md)'s hosting: the worker no
longer runs in the API process.

## Context

The worker ran inside the API process, so that there was one thing to deploy. The two could only scale together, and a
busy worker took CPU from the API. The code was in two projects, Domain and Api, and the second held HTTP,
persistence, Temporal and telemetry side by side.

## Decision

The worker is a host of its own, [`FunBooksAndVideos.Worker`](../../src/FunBooksAndVideos.Worker/PurchaseOrderWorker.cs):
a .NET worker service that polls Temporal and runs every order's workflow and activities. The API starts workflows and
reads SQL, and runs neither. The two share only Temporal's task queue and the database, so each is deployed and scaled
on its own: `docker compose up --scale worker=3` locally, and `worker_replicas` in Azure, where the worker is a second
container app with no ingress.

Under the two hosts, the code is in three tiers, each depending only on the one below it:

- **Application**: submitting an order, the workflow and activities that fulfil it, and the reads.
- **Infrastructure**: SQL Server through EF Core, the domain's JSON, the Temporal client, telemetry and settings.
- **Domain**: the model, the rules and the commands, with no packages and no I/O.

The API adds the HTTP contract, its validation and the controllers. The worker adds the Temporal worker and registers
the rules.

## Alternatives

- **Keep the worker in the API process.** One deployable, which is enough until the two need to scale apart.
- **A project per feature (vertical slices).** With one feature, purchase orders, that is one project again.

## Consequences

- There are two images, built from one Dockerfile. Deploy rolls out the worker first, because it has to run whatever
  the new API starts. A schema change, though, reaches SQL only when the new API starts and migrates it: until then,
  any activity that needs it fails, and Temporal retries it (ADR 4).
- Workers scale without touching the API. The dashboard's *Activities a second, per worker* shows the work spreading
  across them.
- The worker image is the plain .NET runtime, so nothing the worker references may bring in ASP.NET Core.
- Each host reads the task queue from its own appsettings.json. The tests run the worker with its real settings, so the
  two can't drift apart unnoticed.
- The hosts share one managed identity, so the worker is a SQL admin too: ADR 7's shortcut covers both.
