# Verifying interaction quality

Type: grilling
Status: open
Blocked by: 01, 05

## Question

Decide how this effort proves its own deliverable, given that the existing test layers structurally cannot.

The repo has two layers, both settled by ticket 04 of `d12canvas-next`: bUnit for logic and markup, and Playwright for .NET for rendered visual states, with a standing rule that any ticket rendering a new visual state adds a screenshot case. `AGENTS.md` hardens that into a pre-commit requirement — the full suite, in the pinned Docker image, with `-parallel none`.

Neither layer can observe what this effort is actually about. A screenshot diff cannot see that connectors lag a drag by one commit, that a pan has no momentum, that a gesture leaked and the canvas is still dragging with no button held, or that a snap felt sticky. Ticket 04 should confirm the sharpest version of this: a scripted Playwright drag always releases cleanly inside the element, so the reported release-reliability bug is very likely invisible to the entire existing suite.

Decide:

- Whether a new automated layer is warranted, and what it asserts. Candidates: state assertions at intermediate points within a scripted gesture (does the edge endpoint track the container mid-drag?), event-sequence assertions, deliberately adversarial gestures that release outside the element or interleave buttons, and frame-timing or render-count budgets.
- Whether Playwright can express the adversarial cases at all — releasing outside the viewport, losing window focus, synthesising trackpad `wheel` events with `ctrlKey` — or whether some are only reachable manually. Ticket 02's findings bear directly on the trackpad half.
- Whether a manual acceptance checklist is a legitimate part of the deliverable for the parts automation cannot reach, and if so where it lives and who runs it.
- Whether the arbitration model from ticket 01 should be designed to be *testable in isolation* — a pure classifier over synthetic input that bUnit can drive without a browser — and what that constrains about where it lives. This is the highest-leverage question here: it could move most of the coverage down into the fast layer.
- Whether render-count or frame-budget assertions belong in CI or would be too flaky. `d12canvas-next` already recorded visual-test parallelism flakiness and SDK-skew baseline drift as real problems (tickets 78, 79, 81), so the bar for adding a timing-sensitive layer is high.
- What the standing rule becomes. The existing one — new visual state means a screenshot case — needs an interaction-shaped counterpart, or it will be applied to gestures it cannot actually verify.
