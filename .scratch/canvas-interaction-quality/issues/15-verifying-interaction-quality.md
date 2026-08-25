# Verifying interaction quality

Type: grilling
Status: resolved
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

## Answer

Recorded as **ADR 0025**. Three drive points, one line drawn three times, no manual layer, and a
standing rule that is a test rather than a sentence.

### The ticket's framing was half wrong, and the correction reframes the work

This is not "add a layer on top of a working suite". **39 of 86 bUnit files dispatch pointer events
at elements, 573 call sites, and ADR 0018 deletes every binding they drive.** Those tests do not
fail, they stop being able to reach the code. The bulk of this work is roughly forty files
reattaching, and choosing the wrong attachment point pays for the rework twice.

Two of the ticket's own premises are false. **All 26 visual test files already pass
`ScreenshotAnimations.Disabled`**, so the screenshot itself has never raced the ambient
`transition: transform 0.1s ease-out`; only pre-screenshot steps (bounding-box reads, clicks taken
mid-flight) are exposed. And **no `prefers-reduced-motion` rule exists anywhere in the codebase**,
so `reducedMotion: 'reduce'` is a no-op that ADR 0015's implementation has to earn first.

### Three drive points

1. **Gesture objects, driven directly** over a fake context. No renderer. ADR 0018 already bought
   this by giving gestures an explicit context rather than a back-reference; a fake context is that
   same seam from the other side.
2. **The press-to-kind mapping, through bUnit**, as a table. Eight owners against eleven roles and
   three buttons is a space no quantity of hand-written drags covers evenly, which is how 573 call
   sites left all six leaks undetected.
3. **Interaction probes** in a real browser, asserting state rather than pixels, for everything
   JavaScript owns.

Each exists because the others are blind to something: a gesture correctly implemented and never
selected is invisible to the first, and move arithmetic asserted through markup is what made the
current tests expensive.

### Probes live in `D12Canvas.VisualTests`, and the container is not the reason

Both prior probes of this shape already lived there. A third project looked attractive because
these tests have no font sensitivity and therefore need neither the pinned container nor baselines,
and it buys nothing: the container is a convention about invocation, and **`-parallel none` is
shared-`D12Canvas.Demo`-process contention** (a locator timing out at zero elements, a click
intercepted), which an assertion suite inherits identically. Cost stated: probe failures gate
behind the slow job, which is near-free here since nearly every ticket on this map touches markup.

### `[JSInvokable]` cannot be internal, so the test seam is narrower than expected

Blazor requires `[JSInvokable]` methods to be public; all 20 existing ones are public on
`DiagramCanvas` with `DotNetObjectReference.Create(this)`. So bUnit reaches the four pointer entry
points for free, and **ADR 0018's "entry is `internal`" cannot be read literally** — worth
recording because the failure is silent: a non-public `[JSInvokable]` fails at *runtime*, shipping
a canvas where no pointer works with a clean build. The sentence means the arbitration surface, not
the interop entry.

`InternalsVisibleTo` is taken and buys exactly one thing: **ADR 0020's preview**. Without it, "does
the edge follow the shape" reverts to a question about a `style` attribute, which is the
indirection ADR 0020's data-shaped preview exists to remove. This does **not** reopen ADR 0018's
rejection of a public observation surface, which was about a *host* inspecting a live gesture.

### A leak is behavioural, so it is asserted behaviourally

The leak probe's own rule: **a response to a buttonless pointer is a leaked gesture.** No reflected
attribute (the visual suite verifies `.verified.html`, so gesture state would enter every baseline
in the project), no probe page, no observation surface. The approach is blind to a leaked
`SelectEdge` or `Native` — **the blind spot and ADR 0018's two stateless members are the same
set**, which is a property rather than a gap. `lostpointercapture` on a live gesture writes
`console.error` and the fixture fails on it, turning "should never fire" into something enforced.

### The line, drawn three times

- **Plumbing, not magnitudes** (ticket 17's, generalised): probes prove a modifier survived the
  hop, never a number.
- **Relationships, not values**: assert `drag threshold (4) < snap radius (8) < edge hit band
  (20)`, all screen pixels. Pinning a value catches only drift, and ticket 17's constant was wrong
  the day it was written. A threshold above the snap radius silently stops short drags snapping.
- **Counts, not clocks**: render counts prove ADR 0020's budget tracks participants rather than
  board size; coalescing counts prove ten `pointermove` events in one frame yield one call. A
  wall-clock canary is rejected — loose enough to survive a loaded runner is loose enough to miss
  anything worth catching.

**The relationship test finds a live defect on contact.** `PortHitRadius = 10` is **board space**,
documented in its own comment as unaffected by zoom, so it cannot join the ordering. ADR 0017 made
hit regions screen-constant precisely because such a number describes how precisely a hand can aim;
the port tolerance is the family's one holdout, covering 2.5 screen pixels at 0.25x zoom, at
exactly the zoom where aiming is hardest. Handed to ticket 06, which owns those numbers.

### No manual acceptance pass, and what that costs

Decided against a checklist. So roughly ten tuned numbers ship judged by nothing: zoom sensitivity,
ambient smoothing, the idle boundary, the framing flight, the drag threshold, snap radius, velocity
cut-off, edge band, press margin, LOD threshold. Relationship assertions keep them consistent with
each other and say nothing about whether any feels right. The same gap covers rate: dropping from
60fps to 30fps with identical render counts is invisible to every layer here. Recorded as a known
consequence rather than left to be discovered.

### The standing rule is a `[Theory]`, not a sentence

"A new rendered visual state" is something a person recognises having made. "A new interaction" is
not, which is the trap this ticket named. So the obligation is **a parameterised test over ADR
0018's closed set of eight** — one `Release-reliability case` per member, and a ninth gesture fails
the suite until its case exists. That is the second load-bearing use of ADR 0018 closing the set.
`docs/agents/testing.md` gains one line pointing at the test rather than restating it in prose. The
visual-state rule is unchanged.

### What Playwright reaches

Thirteen probes settled it empirically, and both shapes assumed hardest were expressible: the right
button released mid-pan, and a press and release in one JavaScript turn. Under ADR 0018 most stop
being cases at all, since **capture makes the release location meaningless** and both cancel
channels are script-reachable (remove the captured element for `lostpointercapture`, dispatch
`pointercancel`). The boundary is device physics: Playwright's synthetic wheel is coarse, so
**ADR 0019's `Auto` profile classifies it as a mouse every time** and the trackpad branch needs the
profile forced or raw fine-delta dispatch. Recorded so a green suite is not mistaken for trackpad
coverage.

### Animation

Reduced motion **suite-wide**, not per case. Two constraints follow: a reduced-motion rule **may
only zero durations** (suite-wide means every baseline documents the reduced rendering and none
documents the default, honest only if they agree at rest), and **ADR 0015's pointer-event
suppression must key off `transitionend` rather than a 250ms timer**, or reduced motion leaves the
canvas dead to the pointer for 250ms with nothing animating. That is a defect prevented rather than
found, and it is why **one case deliberately opts back in**: it asserts suppression applies during
a flight and clears after, never the duration, with a reduced-motion counterpart asserting it has
already cleared. The only place both paths run.

### Two findings handed to implementation

- **CI runs the visual job without `-parallel none`**, the flag every other document calls
  mandatory and without which failures are documented as indistinguishable from real regressions.
  A gate that flakes proves nothing, so it is in scope here.
- **ADR 0015 shifts baselines twice** — framing all content when a `Board` is first set changes
  every board-mounting baseline's opening view, and the minimap is new chrome needing demo
  coverage. Planned for rather than discovered mid-run.

### Recorded

**ADR 0025**, amending ADR 0015 in one place (suppression keyed to the transition's lifecycle),
confirming ADR 0018 with one sentence clarified and ADR 0020 throughout, and adding a caveat to
ADR 0019. `Interaction probe` and `Release-reliability case` added to `CONTEXT.md`.
