# 2. Immediate activation with Update-with-Start

Status: accepted, 2026-11-05

## Context

BR1 says a membership in an order must be activated immediately. Once processing is asynchronous and durable,
"immediately" can slide into "soon". For a customer who has just bought a membership in the app, it means the
membership works by the time the app says the order went through.

## Decision

The API starts the order's workflow and sends it a `Submit` update in a single call (Update-with-Start). The update
completes once the workflow has run the commands marked `Immediate`, which is BR1's activation, and before the
`Deferred` ones, such as BR2's slip. A 201 therefore means "accepted, and the memberships are active".

The rule that emits a command decides its timing, so the workflow never needs to know which rule wants what.

If the acknowledgement doesn't come within `Submission:AcknowledgementTimeout` (10 seconds), or Temporal can't be
reached, the stored order decides:

- **Stored:** 202 with a Location to poll. The workflow carries on.
- **Not stored yet:** 503. The API can't tell whether Temporal received the order at all, so it doesn't claim to have
  accepted it, and resubmitting is safe.

A retry of an order that is still in progress joins its workflow and waits in the same way. So a 200 for a retry also
means the memberships are active. A retry whose workflow an operator ended before then can't run it again, and gets a
409 (ADR 3).

## Alternatives

- **Start the workflow and always answer 202.** Simplest, but then BR1 isn't immediate for the client.
- **Activate in the API, then start a workflow for the rest.** That makes two write paths, and a crash between them
  leaves a membership without its order, or the other way round.
- **Start the workflow, then poll until the membership is active.** It works, but costs more round trips than one
  call.

## Consequences

- A submission's latency includes storing the order and activating the membership: tens of milliseconds locally.
- Which commands are immediate is a business decision made in one place, the rule.
- The 202 and 503 answers are tested end to end, against an API whose task queue no worker polls. Each timeout and
  outage case is also tested at the workflow level.
