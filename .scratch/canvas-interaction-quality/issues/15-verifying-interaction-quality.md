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
- **How the suite deals with animation, now that ADR 0015 has introduced some.** Framing commands animate the canvas transform for ~250ms, so a screenshot taken without care races the transition — and the pre-existing ambient `transition: transform 0.1s ease-out` on `.canvas-content` means *every* baseline involving a pan or zoom has silently had this hazard all along. ADR 0015's recommendation is Playwright's `reducedMotion: 'reduce'` context option, so baselines capture the destination rather than a settle-and-hope wait; confirm that reaches the `prefers-reduced-motion` media query in the pinned Docker image, and decide whether it applies suite-wide or per-case. Note also that ADR 0015 suppresses pointer events on the container during a framing flight, which is exactly the kind of transient state a scripted gesture can trip over.
- **Two baseline-shifting changes ADR 0015 lands**, worth planning for rather than discovering: the canvas now frames all content when a `Board` is first set, which changes the opening view of every board-mounting baseline, and the minimap is new chrome needing `D12Canvas.Demo` coverage of its own.

## Note from ticket 17: a green assertion-only probe can still not describe the device

`WheelInputProbeTests` asserts a wheel notch arrives as `deltaY: 120`. It is green. **Real hardware
delivers 100** — Windows' raw `WHEEL_DELTA` is 120, but the engine converts a notch to three lines
of ~33.3px before it reaches the event, and Playwright's synthetic wheel skips that conversion.

Any feel constant tuned against the test would inherit a 20% error. The lesson for this ticket is
not "assertion-only probes are bad" — they remain valuable, and unlike the rest of that project
they have no font/AA sensitivity and run correctly outside the pinned container. It is that their
value lies in what they prove about **plumbing** (modifiers survive the trip, the pointer-anchor
invariant holds, `defaultPrevented` is set) and never about **magnitudes**, which only real input
can establish. Whatever verification approach this ticket lands on needs that line drawn explicitly.
