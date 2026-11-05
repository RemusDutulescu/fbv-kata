# RFC-001 — Purchase order processing

**Status:** Approved at gate 2 (2026-11-04) and implemented. The body below is the proposal as it was reviewed, kept
as a record of the reasoning; where it and the code disagree, **the code is right**. Known drift from it and from the
[implementation plan](rfc-001-implementation-plan.md), corrected here rather than rewritten below:

| Proposed | Shipped |
| --- | --- |
| Membership activation as one SQL statement, `Memberships = Memberships \| @m` | The `Customer` aggregate with a `rowversion`. Temporal retries a write that conflicts. Changed at gate 2 ([ADR 5](adr/0005-optimistic-concurrency-for-memberships.md)). |
| 202 whenever the acknowledgement takes too long | 202 if the order is stored, 503 if it isn't, so a client is never told "accepted" about an order Temporal may not have received. Changed at gate 4 ([ADR 2](adr/0002-immediate-activation-with-update-with-start.md)). |
| A payload hash tells a retry from a conflict (plan) | The stored order is compared with the submission by value. Changed at gate 4 ([ADR 3](adr/0003-order-id-is-the-idempotency-key.md)). |
| The workflow's input is the validated request (plan) | An `OrderSubmission` record, so the HTTP contract can change without touching recorded workflow histories. |
| Strict JSON rejects unknown types and fields | It also rejects missing or null required fields, enum numbers, numbers sent as strings and duplicate keys ([ADR 6](adr/0006-validation-in-layers.md)). |
| Options checked by `ValidateOnStart`, with defaults in code (plan) | `AddRequiredOptions`: a missing section or value stops the app at startup, and the values live in appsettings. Changed at gate 4. |
| Pattern names (plan): "orchestrated saga", Visitor "not used", Liskov substitution | A process manager, Visitor-style double dispatch, and polymorphism without type checks. Changed at gate 4. |
| The deploy identity is Contributor on the resource group (plan) | Contributor on the container apps only. Changed at gate 4. |
| Minimal APIs, in an Api project that also hosts the worker | MVC controllers, each action declaring its route, rate limit, validation and every status it answers. The worker is a host of its own, and the code is in three tiers. Changed at gate 5 ([ADR 8](adr/0008-three-tiers-and-a-worker-host-of-its-own.md)). |
| Traces in the Aspire dashboard locally | Traces, metrics and logs in Grafana (`grafana/otel-lgtm`), with a provisioned dashboard and a k6 load script. Changed at gate 5. |
| CodeQL, and CI on every push | CodeQL isn't set up: deploy step 1 in the README turns on its default setup after the first push. CI runs on `main` and on pull requests. |

**Author:** Claude (with Remus Dutulescu)
**Date:** 2026-11-04
**Relates to:** [brd.md](brd.md)

## 1. Context

The [BRD](brd.md) and its gate 1 answers: Temporal orchestrates the processing, it runs on Azure and Temporal Cloud,
and the repository is public. .NET 10 with Minimal APIs, EF Core and xUnit; a Domain project with no dependencies and
an Api project, plus their tests.

## 2. Processing

- **One workflow per order, keyed by the order id**, so a retried order is processed once.
- **BR1 through Update-with-Start.** One call starts the workflow and waits for an update that completes once the
  membership is active, and the API answers then. The shipping slip follows, durably. If activation takes too long,
  the API answers 202 with a link to poll.
- **The rules run in an activity, not in workflow code.** Rules are the code the brief says will change, and changed
  workflow code breaks in-flight workflows when Temporal replays them. As an activity result, the decision is also
  recorded in the history. A replay test in CI catches a non-deterministic change.
- **Rules decide, commands act.** A rule returns commands (activate a membership, generate a slip), and the workflow
  carries them out as activities.
- **No cancelling.** The whole order is validated before anything runs; after that, each step is safe to repeat and
  is retried until it succeeds. Nothing after acceptance can say no for a business reason, so nothing needs
  compensating.
- **The trade-off, stated up front.** For two writes in one service, a transaction would do. Temporal earns its
  place because the real rules, such as the original kata's emails, royalty slips and commissions, reach other
  systems.

## 3. Storage

- Temporal Cloud is durable, so the database must be too: SQL Server in Docker Compose, in the tests (Testcontainers)
  and in Azure. The tests run on the production engine.
- Membership activation is one atomic statement, `Memberships = Memberships | @m`. Reading the row, changing it and
  writing it back would let two simultaneous orders for one customer, Book Club and Video Club, lose a club.

## 4. API and validation

- Minimal APIs under `/v1`. The contract is separate from the domain and mapped by hand, because old app versions
  stay in use.
- One polymorphic list of item lines, `"type": "book" | "video" | "membership"`, with enums as strings and problem
  details carrying a trace id. The OpenAPI document comes from the endpoints' typed results.
- Four layers, each with one job:
  1. **JSON.** An unknown type, an unknown field or malformed JSON is a 400.
  2. **Contract.** FluentValidation, per kind of line, with errors at paths like `items[2].membership`. .NET 10's
     built-in validation couldn't be confirmed to handle polymorphic types.
  3. **Domain.** Constructors enforce the invariants, so an invalid order can't exist.
  4. **State.** An order id reused with a different order is a 409.
- Limits on body size, item count and string length, and a rate limit: the endpoint is public, and every submission
  spends Temporal Cloud actions.

## 5. Delivery

- Azure Container Apps, Azure SQL with Entra-only sign-in, Temporal Cloud with an API key and Application Insights,
  all built by Terraform.
- No secret in git. GitHub signs in to Azure with OpenID Connect, and the app signs in to SQL with a managed
  identity. The one secret, the Temporal Cloud API key, goes from Terraform straight into Azure. Terraform state is
  gitignored. The repository gets secret scanning with push protection and CodeQL.
- CI builds and tests every push. After each deployment, the pipeline runs the Postman collection with Newman, so the
  collection is also the smoke test and can't drift from the API.
- One trace per order, from the request through the workflow and its activities into SQL, with a span per rule: in
  the Aspire dashboard locally, and in Application Insights in Azure.

## 6. Patterns

Only where they solve a problem here:

- **Used:** Strategy (a class per rule), Command (rules return actions), Process Manager (the workflow), Idempotent
  Receiver (the order id), Ports and Adapters (the domain depends on nothing), and CQRS-lite (writes go through
  Temporal, reads come from SQL).
- **Not used:** Chain of Responsibility (the rules are independent, and a chain stops at the first handler),
  Specification (each condition is one line), a generic repository (the DbContext is one), and a transactional outbox
  (Update-with-Start is the only way in, so there is no double write).

## Gate 2

**Proposed** at 18:51: the design above, and two questions.

| Question | Options | The owner's answer |
| --- | --- | --- |
| Which additions? | A rule audit trail · a dry-run endpoint · strongly-typed ids · a rule kill-switch | Strongly-typed ids, and: "All these things you're proposing are just additional slop nobody asked for. I'm more interested in showcasing patterns." |
| How are commits made? | Claude commits with a co-author trailer · Claude commits, and the README notes the AI use · Claude prepares each step, and the owner commits it | Claude prepares each step, and the owner commits it |

**What changed**, in the [implementation plan](rfc-001-implementation-plan.md):

- Every feature beyond the brief is out: the audit trail, the dry-run endpoint and the kill-switch. What the public
  deployment or correctness needs stays, listed for a veto: rate and body limits, health checks, the 202 and the
  payload hash.
- The patterns stay inside the requested scope, each with one job, in a map that says where each one lives. It is
  now the README's [Patterns](../README.md#patterns) table.
- Orders and customers get strongly-typed ids.
- Membership activation moves into the domain: the `Customer` aggregate with optimistic concurrency, and Temporal
  retrying a write that conflicts. The same guarantee against lost updates, with the business logic in the domain.
- Claude stops after each step with the change ready and a commit message, and the owner commits it. Revised at
  [gate 3](rfc-001-implementation-plan.md#gate-3).
