# How this was built

The service was designed and built with Claude Code, in stages that each end at a gate. At every gate the owner
reviews what was proposed and answers its open questions, and the next stage starts from those answers.

| Stage | Document | Gate |
| --- | --- | --- |
| What the business needs | [BRD](brd.md) | 1. The owner settles the questions the brief leaves open. |
| How to build it | [RFC-001](rfc-001-purchase-order-processing.md) | 2. The owner chooses between the options and cuts what isn't needed. |
| In what order | [Implementation plan](rfc-001-implementation-plan.md) | 3. The owner approves the plan. |
| The build | One commit per step | |
| The review | Two independent reviews, each finding checked against the code before it is reported | 4. The owner decides what gets fixed. |
| The final review | Three more reviews, and the redesign the owner asked for while they ran | 5. The owner answers four questions and confirms the plan. |

Each document ends with its gate: what was proposed, the owner's answer in their own words, and what changed because
of it. The decisions that outlast the process are the [ADRs](adr/).

## Gate log

Times are local (UTC+2), as in the git log.

| Gate | When | Decided |
| --- | --- | --- |
| [1](brd.md#gate-1) | 2026-11-04 18:45 | Temporal orchestrates the processing. Azure and Temporal Cloud, a public repository, and tracing, Terraform, validation, patterns and Postman on top. |
| [2](rfc-001-purchase-order-processing.md#gate-2) | 2026-11-04 18:53 | Strongly-typed ids in, every other addition out: patterns over features. |
| [3](rfc-001-implementation-plan.md#gate-3) | 2026-11-05 08:55 | The plan approved. At 09:10, a new working agreement: Claude commits every step under the owner's name, and the owner reviews at the end. |
| [4](rfc-001-implementation-plan.md#gate-4) | 2026-11-05 11:41 | Every review finding fixed. At 12:29, the app's own settings bound to validated classes. |
| [5](rfc-001-implementation-plan.md#gate-5) | 2026-11-05 13:38 | Three tiers under two hosts, with the worker scaling on its own; MVC controllers; a JSON converter per class hierarchy; Grafana for traces, metrics and logs; the Azure deployment kept as code. Every review finding fixed. |

These documents were written up afterwards, on 2026-11-05, from the session's record of proposals, questions and
answers. Each is dated to when it was proposed, and the owner's words are quoted as given, with typos fixed.
