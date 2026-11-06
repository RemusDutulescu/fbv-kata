# How this was built

The service was designed and built with Claude Code, in stages. Each stage ends at a gate, where the owner reviews
the proposal and answers its open questions, and the next stage starts from those answers. Each document ends with
its gate: what was proposed, the owner's answer in their own words, and what changed. The decisions that outlast the
process are the [ADRs](adr/).

| Gate | Stage | When | Decided |
| --- | --- | --- | --- |
| [1](brd.md#gate-1) | What the business needs: the [BRD](brd.md) | 2026-11-04 18:45 | Temporal orchestrates the processing, on Azure and Temporal Cloud, in a public repository. Tracing, Terraform, validation, patterns and Postman on top. |
| [2](rfc-001-purchase-order-processing.md#gate-2) | How to build it: [RFC-001](rfc-001-purchase-order-processing.md) | 2026-11-04 18:53 | Strongly-typed ids in, every other addition out: patterns over features. |
| [3](rfc-001-implementation-plan.md#gate-3) | In what order: the [implementation plan](rfc-001-implementation-plan.md), then the build, one commit per step | 2026-11-05 08:55 | The plan is approved. At 09:10, Claude is to commit every step under the owner's name, for one review at the end. |
| [4](rfc-001-implementation-plan.md#gate-4) | Two independent reviews of the build, each finding checked against the code | 2026-11-05 11:41 | Every finding fixed. At 12:29, the app's settings bound to validated classes. |
| [5](rfc-001-implementation-plan.md#gate-5) | Three more reviews, and a redesign the owner asked for while they ran | 2026-11-05 13:38 | Three tiers under two hosts, with the worker scaling on its own. MVC controllers, a JSON converter per class hierarchy, and Grafana for traces, metrics and logs. The live demo on the owner's homelab, with the Azure deployment kept as code. Every finding fixed. |

On 2026-11-06, the owner removed the Azure deployment and the Postman collection
([after gate 5](rfc-001-implementation-plan.md#after-gate-5)).

Times are local (UTC+2), as in the git log. The documents were written up on 2026-11-05 from the session's record.
Each is dated to its proposal, and quotes the owner as given, with typos fixed.
