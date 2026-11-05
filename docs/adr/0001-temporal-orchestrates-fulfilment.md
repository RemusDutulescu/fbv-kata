# 1. Temporal orchestrates fulfilment

Status: accepted, 2026-11-05

## Context

Fulfilling an order means several side effects: store the order, activate memberships, generate a shipping slip.
More rules will bring more of them. Each one must happen exactly once, even when the process crashes halfway, the
database blips, or the client retries.

## Decision

Each order runs as a Temporal workflow, [`PurchaseOrderWorkflow`](../../src/FunBooksAndVideos.Api/Processing/PurchaseOrderWorkflow.cs).
Every side effect is an activity: retried with backoff, idempotent, and recorded in the workflow's history.

The rules are evaluated inside the first activity, `AcceptOrder`, and their commands come back as its result.
Workflow code must be deterministic when Temporal replays it. If the workflow evaluated the rules itself, deploying a
changed rule would change what an in-flight order decides on replay, and Temporal would refuse to continue it. As an
activity result, the decision is recorded once and replays read it back. A test replays a committed history, so a
non-deterministic change fails CI rather than production.

The worker is hosted in the API process, so there is one thing to deploy.

## Alternatives

- **In-process, in a database transaction.** Simplest, but a crash between two steps loses work. Retries then need
  hand-written idempotency and a sweeper job to find the stranded orders.
- **Outbox plus a message broker.** Durable, but the orchestration is left to build: the state machine, retries,
  timeouts and visibility.

## Consequences

- Durable state, retries and an operator's view (the Temporal UI) come with it.
- Temporal becomes a runtime dependency: Temporal Cloud in Azure, the dev server locally and in tests.
- Workflow code follows determinism rules, and a change to the workflow's shape needs Temporal's versioning once
  orders are in flight.
- The worker scales with the API. Split it out when the two need to scale separately.
