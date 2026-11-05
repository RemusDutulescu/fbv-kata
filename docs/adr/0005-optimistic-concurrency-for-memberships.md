# 5. Optimistic concurrency for memberships

Status: accepted, 2026-11-05

## Context

Two orders for the same customer can be processed at the same moment, one with Book Club and one with Video Club.
Both read the customer, add their club and write it back. Without care, the last write wins and a club is lost.

## Decision

[`Customer`](../../src/FunBooksAndVideos.Domain/Customer.cs) is an aggregate, and its `Activate` method unions the
membership flags. The table carries a `rowversion` as an EF shadow property, which the domain never sees.

When two writes overlap, the second `SaveChanges` throws a concurrency exception, its activity fails, and Temporal
retries it against fresh data, so the union includes both clubs. Two orders that both create the same customer
collide on the primary key and recover the same way. A test races two such orders and asserts that the customer ends
up Premium.

## Alternatives

- **One SQL statement, `SET Memberships = Memberships | @club`.** Atomic and a single round trip, but the business
  logic (the union and its validity) moves out of the domain into SQL.
- **Pessimistic locks or serializable transactions.** They block concurrent work and risk deadlocks, for contention
  that is rare.

## Consequences

- A conflict costs one retry, after a backoff of at least a second. That is acceptable for contention within a single
  customer.
- Temporal's retry is the recovery, so no retry loop is written by hand.
