# 4. Forward recovery, no compensation

Status: accepted, 2026-11-05

## Context

A saga usually pairs each step with a compensation, which undoes it when a later step fails. That only pays off when
a step can fail for a business reason.

## Decision

Recovery is forward-only. Every activity is idempotent and retried with exponential backoff and no attempt limit,
until it succeeds. Only `DomainException` is non-retryable, and validation in the API makes it unreachable for an
order that was accepted.

Once an order has passed validation, nothing in this domain can say no. Activating a membership is a union of flags,
and generating a slip is an insert. The failures left are transient, such as the database or the network, so
retrying is the right response. Taking back a membership the customer has paid for would be wrong.

## When compensation becomes necessary

When a step can legitimately refuse: reserving stock, charging a card, calling a partner that can reject. That step
gets a compensating activity (release the stock, refund the card), and on a non-retryable failure the workflow runs
the compensations in reverse order. That is Temporal's saga pattern, and it fits into this workflow without
reshaping it.

## Consequences

- An order is never left half-done: every step eventually completes.
- A step that keeps failing, during a database outage say, keeps its workflow running and visible in the Temporal UI
  instead of failing a paid order. Alerting on retry counts is the operational counterpart.
