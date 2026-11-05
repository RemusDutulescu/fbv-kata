# RFC-001 — Implementation plan

**Status:** Approved at gate 3 (2026-11-05). All twelve steps are done, and gate 4 reviewed them. The Azure steps,
10 and 11, pass `terraform validate` and actionlint but have not been applied yet.
**Author:** Claude (with Remus Dutulescu)
**Date:** 2026-11-04
**Relates to:** [RFC-001](rfc-001-purchase-order-processing.md)

## 1. Steps

One commit each. Steps 1 to 7 stand on their own; if time ran short, 10 and 11 would go first, then 9.

| # | Step | Commit |
| --- | --- | --- |
| 1 | Skeleton: the solution, four projects, build properties, editorconfig | `3135231` |
| 2 | The domain model: value objects, item lines and aggregates, with their tests | `7c392e1` |
| 3 | Rules, commands and the processor, tested on BR1, BR2, the brief's example, the Premium union and a new rule plugged in | `df27ee4` |
| 4 | Persistence: EF Core on SQL Server, converters, a rowversion and the first migration | `ce14c23` |
| 5 | Temporal: the workflow, the activity proxy, Update-with-Start and the hosted worker, with tests on a Temporal dev server and a replay test | `bbeb848` |
| 6 | The API: contracts, validation, mapping, endpoints, OpenAPI, problem details, limits and health, with integration tests | `67836ca` |
| 7 | CI | `2c8187e` |
| 8 | The Postman collection | `5449e67` |
| 9 | Tracing | `d0c79f5` |
| 10 | Terraform | `f8d7285` |
| 11 | The deploy pipeline, ending with Newman | `dab09db` |
| 12 | The README and the ADRs | `35e81ad` |

## 2. Definition of done

- **Each step:** `dotnet build` with no warnings, and the step's tests green.
- **End to end,** on `docker compose up`:
  - the brief's order is a 201, with Book Club active;
  - its slip appears shortly afterwards;
  - the Temporal UI shows AcceptOrder, then ActivateMembership, the update completing, then GenerateShippingSlip;
  - the dashboard shows one trace with a span for each rule.
- **Edge cases:** the same order again is a 200 with the same body, other contents under the same id are a 409, and
  Book Club and Video Club ordered at once end as Premium.
- **In Azure:** Newman green after the deploy, the same trace in Application Insights, and the workflow in the
  Temporal Cloud UI. Not yet run: see the status.

## Gate 3

**Proposed** at 18:58 on 2026-11-04: this plan.

**The owner's answer:** approved at 08:55 on 2026-11-05. At 09:10, before the first commit, the working agreement
changed: "You go ahead and commit (using my name, not your signing!) and do each commit up to the very end. Only at
the end I will review everything."

**What changed:** Claude committed each step under the owner's name, with no co-author trailer. Instead of a stop
after every step, there was one review at the end: gate 4.

## Gate 4

**Asked for** at 10:44: "We need to do a full-scale review of the entire thing. Anything we missed? Any code we
over-engineered without much practical and/or showcasing value? Also look for potential cleanup and code reduction
opportunities, but without sacrificing readability (it's very important code is as simple as possible where possible,
not complex AI slop)." And, of this way of working: "showcase that too, in a docs folder, and 'backdate' create these
docs based on what we discussed and built."

**Proposed** at 11:30, from two independent reviews, one for correctness and one for over-engineering. Every finding
was checked against the code, and the important ones against the running stack or the SDK's documentation. Twelve
things had been missed, the most damaging first:

1. A 202 could promise an order that Temporal never received.
2. A retry could get a 200 before its membership was active.
3. In a rare race, an identical retry could get a 409.
4. An id with a trailing newline passed validation, and failed as a 500 after the work was done.
5. On the deployed site, the API reference's "Try it" would be blocked: Azure ends TLS, so the app saw plain HTTP.
6. Readiness depended on Temporal, so a blip there would take the read endpoints offline too.
7. A new command also needs a JSON name, which nothing enforced.
8. SQL compared ids case-insensitively, and Temporal compares them case-sensitively.
9. Strict JSON accepted numbers sent as strings, and duplicate keys.
10. The documentation made claims the code didn't bear out.
11. The deploy identity had rights over the whole resource group.
12. The app could start before the SQL firewall rule existed.

Also proposed: cuts (a test suite that repeated the HTTP tests, the payload hash, tests of the compiler, null guards
outside the domain, unused members, repeated explanations), and the things kept against the audit's advice for their
showcase value, such as the pattern table and the OpenAPI description.

**The owner's answer** at 11:41: "Let's do all of these."

| Commit | |
| --- | --- |
| `5329501` | refactor: less code for the same behaviour |
| `b0b70a2` | fix(processing): the stored order decides every submission's outcome |
| `0f5f40c` | fix(api): stricter input, newline-proof ids, readiness on SQL only |
| `09ff0ec` | fix(infra): working API docs in Azure, a gentler readiness probe, a narrower deploy role |
| `e1fc286` | test: one integration suite at the HTTP boundary, and no tests of the compiler |
| `baab453` | docs: claims that match the code, each said once |

In `src` and `tests`, leaving out the regenerated migration, they removed 531 lines and added 323, the fixes and their
new tests included. Afterwards the build had no warnings, the tests passed, Newman was green against a fresh stack, and
the forwarded-headers fix was tested locally.

**Raised by the owner** at 12:01, while those were being built: "Wherever we register using the Options pattern a new
config POCO at startup, can we replace that with an extension method that adds and binds that object and raises an
exception if it's not configured, plus have required where appropriate in the POCO options fields? […] Also favor
POCO config objects rather than looking up config fields by string names."

**Proposed** at 12:21: one `AddRequiredOptions` extension, and one class per section. Settings the app can't run
without are `required` members, checked by `[Required]` or `[Range]` because binding ignores the keyword. The values
move from code into appsettings. The test fixture switches to `UseSetting`, because the app now reads its settings
while it registers services, before settings added with `ConfigureAppConfiguration` are in place. The telemetry
exporters' standard variables stay as they are, because the exporters read them themselves.

**The owner's answer** at 12:29: "Agreed, build your recommendations. And yes, it's OK to skip those 2 telemetry
vars." That became `2feac86`, checked with the compose stack stopped so that a setting that didn't apply couldn't
fall back to it, and by starting the image without its settings, which it refuses.

Last came this folder, written up from the session's record.
