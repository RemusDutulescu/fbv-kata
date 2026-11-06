# 3. The order id is the idempotency key

Status: accepted, 2026-11-05

## Context

Mobile clients retry after a timeout, on reconnecting, or because of a double tap. A retry must not create a second
order, activate twice or ship twice. A different order that reuses an id must not be silently swallowed either.

## Decision

The order id is the idempotency key, enforced at three levels in
[`OrderSubmitter`](../../src/FunBooksAndVideos.Application/Submission/OrderSubmitter.cs) and the activities:

1. **SQL fast path.** If the order is already stored, it is compared by value with the submission, without calling
   Temporal:
   - a different order gets 409;
   - the same order, already fulfilled, is a retry and gets 200 with the order;
   - a retry of an order still in progress goes on to level 2, so it waits for the memberships as the original did.
2. **Workflow id and update id.**
   - The workflow id is `purchase-order-{id}`.
   - It is started with `IdConflictPolicy.UseExisting`, so a concurrent duplicate joins the running workflow.
   - `IdReusePolicy.RejectDuplicate` means a finished workflow never runs again.
   - The update id `submit-{id}` gives every duplicate the same answer, even after the workflow has finished.
   - Both hold while Temporal keeps the workflow's history, for the namespace's retention. After that, the SQL fast
     path still answers for a fulfilled order.
3. **Primary keys.** Orders and slips are keyed by the order id, and each activity checks before it inserts, so a
   retried activity changes nothing.

Whatever Temporal answers, the stored order decides. Each of these is a 409:

- a different order under the id;
- an acknowledged order that isn't stored, which means the id was used before a database wipe;
- a retry of a workflow that ended without acknowledging, because an operator terminated it, stored or not. Its
  memberships may never have been activated, and the workflow can't run again.

Two submissions are the same order when their customer, total and lines are equal by value, so JSON formatting and
trailing zeros don't matter. SQL compares ids case-sensitively, because Temporal's workflow ids are case-sensitive.

## Consequences

- When two different orders race with the same id, the first one wins. The second gets 409, decided after the update
  by comparing the stored order.
- If the database were wiped while Temporal kept the finished workflow, the id is spent: 409.
- An order whose workflow an operator terminated early is spent too: 409. Resubmitting it under a new id is the
  operator's call.
- Order ids are global, which suits ids issued by the shop. Ids from several shops would need namespacing.
