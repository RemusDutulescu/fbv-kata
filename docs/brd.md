# BRD — FunBooksAndVideos purchase order processing

**Status:** Approved at gate 1, 2026-11-04
**Owner:** Remus Dutulescu
**Author:** Claude (with Remus Dutulescu)
**Date:** 2026-11-04

## 1. Background

The brief adapts Dave Thomas's Code Kata 16 (Business Rules) to FunBooksAndVideos, an online shop that sells books,
videos, and memberships of its Book Club and Video Club. It asks for an object model exposed through a REST API, and
a flexible purchase order processor that applies two of the shop's rules:

- **BR1:** if an order contains a membership, the membership is activated in the customer's account immediately.
- **BR2:** if an order contains a physical product, a shipping slip is generated.

Its example is order 3344656 for customer 4567890, with a total of 48.50: the video *Comprehensive First Aid
Training*, the book *The Girl on the train* and a Book Club membership.

## 2. Reading the brief

- **"Some of the rules", "flexible".** The brief shows two rules, not all of them, so the design is judged by how
  cheaply and safely the next one can be added.
- **"Immediately".** The membership must be active by the time the API answers. Activating it later, from a queue,
  breaks BR1.
- **"Physical product", not "book".** BR2 asks whether a product ships, so an e-book or a DVD added later doesn't
  touch it. Assumed: books ship, and videos are watched online.

## 3. Goals

1. BR1 and BR2, as worded.
2. A new rule is one new class, with no change to the processor.
3. A retried order has the effect of one order. Clients, mobile apps among them, retry on flaky networks.
4. A crash or a retry loses nothing that an accepted order needs.
5. Every decision earns its place. In the owner's words: "Any decision we take has to have a point and to either make
   the architecture more sturdy or to showcase some concept we master."

## 4. Non-goals

- Pricing and payment: orders arrive paid.
- Customer accounts and authentication.
- Printing slips. A slip is a record of what to ship.
- Anything else the brief doesn't ask for, a line drawn firmly at [gate 2](rfc-001-purchase-order-processing.md#gate-2).

## 5. Constraints

- **Two days**, so every addition has to be worth its hours.
- **Public.** The code is there to be read and the API to be tried, so no secret goes into git.
- **Licences.** No library that went commercial in 2025: MediatR, AutoMapper, FluentAssertions 8.
- **Fresh code.** Nothing is copied from other codebases, even where the patterns match.

## 6. The owner's starting points

From the first message, with what each became:

- "The purchase order item lines make me think of a polymorphic list of IItem, where we can have two other
  interfaces deriving from it, IMembership and ISingleItem (or something, I can't think of the best name for it), and
  then concrete implementations like BookClubMembership, VideoClubMembership, VideoItem, BookItem."

  The split between products and memberships stays. The memberships differ only in data, so they become one flags
  type, in which Premium is both clubs. The names follow the brief: `ItemLine`, then `ProductLine` with `IsPhysical`,
  `BookLine` and `VideoLine`, and `MembershipLine`.
- "We probably have to think of transactions, and what we do in case, for a mixed order, the membership activation
  fails (we probably cancel the rest, I guess?) or if it succeeds, but the individual items fail subsequently. Also I
  think we can use a Temporal workflow here."

  All or nothing happens at acceptance: an invalid order is refused before anything runs. After that, every step is
  safe to repeat and is retried until it succeeds, so nothing needs cancelling
  ([ADR 4](adr/0004-forward-recovery-no-compensation.md)).
- "I don't know if we should deploy this too. I'm thinking yes. But where, to make it professional and usable too?
  Also maybe we should include something like Swagger to showcase and define the API."

  OpenAPI with an interactive reference. Where it runs is a gate 1 question.
- "Maybe when there are processes that can be offloaded, we can use Temporal." And: "I'm thinking [of delivering it]
  as a GitHub repo, but I don't know if it's the best idea to make it public."

  Both are gate 1 questions.

## Gate 1

**Proposed** at 18:30: the reading above, and four questions.

| Question | Options | The owner's answer |
| --- | --- | --- |
| How deep should Temporal go? | Orchestrate the processing · an optional adapter beside an in-process path · only in the write-up | Orchestrate the processing |
| Where should it run? | Azure Container Apps with a Temporal dev server · Azure and Temporal Cloud · a homelab behind Cloudflare · nowhere | Azure and Temporal Cloud |
| Where does the code live? | A private repository · a public one · an archive | A public repository |
| Which extras? | A pull request that adds a third rule · end-to-end tracing · an MCP adapter · infrastructure as code | End-to-end tracing, and: "Terraform. Also let's think of others, like input validation, some interesting design patterns too, a Postman collection." |

**What changed**, in [RFC-001](rfc-001-purchase-order-processing.md):

- Temporal Cloud is durable, so the database has to be as well. SQL Server everywhere (locally, in the tests and as
  Azure SQL) replaces the SQLite first proposed.
- A public repository means no secret in git. GitHub signs in to Azure with OpenID Connect, the app signs in to SQL
  with a managed identity, and the one secret, the Temporal Cloud API key, goes from Terraform straight into Azure.
- Validation in layers, a map of the patterns used, and a Postman collection that the deploy pipeline runs.
- The pull request adding a third rule wasn't picked. A test that plugs a new rule into the processor shows the same
  thing.
