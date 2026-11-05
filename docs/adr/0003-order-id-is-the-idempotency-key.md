# 3. The order id is the idempotency key

Status: accepted, 2026-11-05

## Context

Mobile clients retry after a timeout, on reconnecting, or because of a double tap. A retry must not create a second
order, activate twice or ship twice. A different order that reuses an id must not be silently swallowed either.

## Decision

The order id is the idempotency key, enforced at three levels in
[`OrderSubmitter`](../../src/FunBooksAndVideos.Api/Processing/OrderSubmitter.cs) and the activities:

1. **SQL fast path.** If the order is already stored, the submission's SHA-256 is compared with the stored one. The
   same hash is a retry and gets 200 with the order; a different hash gets 409. Temporal is not called.
2. **Workflow id.** The id is `purchase-order-{id}`, started with `IdConflictPolicy.UseExisting` (a concurrent
   duplicate joins the running workflow) and `IdReusePolicy.RejectDuplicate` (a finished one never runs again). The
   update id `submit-{id}` gives the duplicate the same answer.
3. **Primary keys.** Orders and slips are keyed by the order id, and each activity checks before it inserts, so a
   retried activity changes nothing.

The hash covers a canonical form of the order (ids, total to two decimals, lines), so JSON formatting doesn't affect
it.

## Consequences

- When two different orders race with the same id, the first one wins. The second gets 409, decided after the update
  by comparing the stored hash.
- If the database were wiped while Temporal kept the finished workflow, the id is spent: 409.
- Order ids are global, which suits ids issued by the shop. Ids from several shops would need namespacing.
