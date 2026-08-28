# A drag that reaches the edge of the viewport stops there, because auto-scroll needs a clock this model does not have

D12Canvas does not scroll the canvas when a drag reaches the edge of the viewport. A pointer gesture moves content exactly as far as the pointer moves it, and reaching content further away is a matter of zooming out, cutting and pasting, or panning between drags.

This is a decision not to build something the reference bar is split on, taken after establishing the one fact that decides its cost. It is scoped to this effort rather than forever, and the section on revisiting says what would change it.

## A clamped cursor delivers nothing

Auto-scroll is a velocity: the viewport travels while the pointer is held still against the edge. That needs a time base.

The model has none. ADR 0018 gives C# four `[JSInvokable]` methods and every one of them is a pointer event; ADR 0020 rate-limits `OnPointerMoved` to one call per animation frame and `DiagramCanvas` states in a comment that the mounted window follows that cadence "never by a per-frame timer".

The question that decides everything is whether the browser keeps delivering `pointermove` when the operating system clamps the cursor at the edge of the screen. A hand-driven probe answered it: **moves at unchanged coordinates stayed at zero while silence climbed into the hundreds of milliseconds and animation frames kept counting**. Not redundant events at a frozen coordinate, not a `movementX` reported against an unchanging `clientX`. Nothing arrives.

This had to be driven by a real mouse. Playwright dispatches synthetic input at whatever coordinates it is given, so it never reaches the operating system's clamp at all — the same wall ADR 0019 hit with the synthetic 120px wheel notch, and an instance of the device physics ADR 0025 named as the one thing out of the suite's reach.

The corollary is that a canvas filling the window is the case that fails. An embedded canvas keeps receiving moves while the pointer travels over the host page, so any move-driven scheme half-works there and fails completely maximised. A rule that holds for some canvases and not others is the shape this effort exists to remove.

## There is no cheap middle

**Displacement scrolling** — panning by an amount derived from how far past the edge the pointer sits, and scrolling nothing while it is still — is the version that needs no clock. It is not a smaller feature. It still needs the edge band, the ramp, the constants, the board-anchored delta and the windowing pin rule described below, and it still stops dead on a maximised canvas. The user's only recovery is to pull back out of the band and push in again, and because the drag is live, every pump drags the content backwards before it goes forwards.

So the choice is binary. Build auto-scroll with a clock, or do not build it.

## No other clock exists

Three candidates, all dead for reasons rather than by preference:

**A C# timer.** A `PeriodicTimer` in `DiagramCanvas` is not frame-aligned, so it either fires twice inside one frame or beats under it and judders, and on a Blazor Server host every tick is a render over the wire whether or not a frame wanted one.

**A CSS animation on `.canvas-content`.** It moves pixels without moving `ZoomPanTracker`, so hit-testing, viewport windowing, ADR 0024's snap candidates and ADR 0020's gesture preview would all disagree with what is on screen. Broken rather than awkward.

**A native scroll container.** Needs a scrollable extent, and ADR 0011 made the board unbounded. Browsers also do not auto-scroll for pointer drags, only for HTML5 drag-and-drop.

`pointerrawupdate` fires at raw input rate and might survive the clamp, but it is Chromium-only and so cannot be a mechanism here. It was left unmeasured.

## The ticket overstated the problem in one place

Ticket 20 argued that marquee selection carries the same ceiling as a drag, so a selection can only ever be as large as what is on screen. That stopped being true while the ticket sat on the frontier: **ADR 0022 made the marquee additive**, unioning rather than replacing under `Shift`, as the payoff for moving pan off the primary button. A user can marquee a screenful, pan, and `Shift`-marquee the next. The ceiling was lifted three tickets before this one was worked.

What remains is real but smaller than the ticket claimed: dragging an instance, resizing one, or pulling a connector to somewhere more than a viewport away takes drag, release, pan, drag again where it could take one gesture.

## What a user does instead

**Zoom out.** This is what users of every tool actually do, and it has an advantage auto-scroll cannot match: the source and the destination are visible at once, so the drop is aimed rather than groped toward. ADR 0019's multiplicative wheel zoom about the pointer makes the round trip cheap.

**Cut and paste** for a genuinely long move. ADR 0013's paste anchor already drops content at the pointer, so this is both fewer gestures and more precise than dragging across three screens would be.

**`Shift`-marquee across pans** for a selection larger than the viewport, per the section above.

Each of these is a route the product already has. None was built for this purpose, which is part of why the gap is tolerable.

## What this confirms

**ADR 0018 is confirmed and its delta stays screen-anchored.** Auto-scroll would have forced `final delta = release − press` to be re-read in board space, because the point of the feature is that content travels further than the pointer did. That re-reading is sound and would have generalised rather than reversed the identity, but it is unnecessary now: nothing pans mid-gesture except `Pan` itself, whose board delta is zero by construction.

**ADR 0020's windowing argument is confirmed rather than merely surviving.** It reads committed bounds so "a gesture can never unmount its own participant", and rests that on the viewport being changed only by a pan. Auto-scroll is exactly the case that would have made `MoveSelection` change the viewport: `Overscan` is 200 board units, so at any usable scroll speed a dragged instance leaves the mounted window in under a second and the user is left dragging nothing. Declining the feature is what keeps that claim true, and keeps ADR 0020's "no pin rule to write or forget" honest.

**ADR 0025's cost is not compounded.** That decision ships no manual acceptance pass, which means roughly ten tuned numbers already stand defended by nothing. Auto-scroll would have added a dead time, a band width, a ramp and a maximum velocity, all of which are judged by feel and none of which any layer of the suite can assess.

## When to revisit

tldraw ships this with published constants — `edgeScrollDelay: 200`, `edgeScrollEaseDuration: 200`, `edgeScrollSpeed: 25`, `edgeScrollDistance: 8`, `coarsePointerWidth: 12` — driven from its own tick while dragging and not panning. Excalidraw does not have it and carries an open request ([excalidraw#10799](https://github.com/excalidraw/excalidraw/issues/10799)). A mature whiteboard on each side of the line is why this is a judgement rather than an omission.

Reopen it if a host reports the drag ceiling as a real complaint, or if something else in the product needs a per-frame signal during a gesture for its own reasons. The second is the cheaper trigger: the JavaScript heartbeat this decision declines is a small addition to the existing `requestAnimationFrame` loop, and most of this ADR's cost is in what the heartbeat then obliges — the board-anchored delta and the windowing pin — rather than in the heartbeat itself.

## Considered and rejected

- **A JavaScript heartbeat**, where the pointer listener's existing per-frame loop keeps emitting `OnPointerMoved` with the last-seen coordinates while the pointer sits in the edge band. The correct mechanism if the feature were built: no new invokable method, no new event kind, and JS owning the band test and the dead time exactly as it already owns the movement threshold. Declined with the feature, not on its own merits.
- **A C# `PeriodicTimer`**, **a CSS animation**, **a native scroll container**, and **`pointerrawupdate`** — see the clock section.
- **Displacement scrolling** — pays the whole cost of the feature and still fails on a maximised canvas, where every recovery pump moves the dragged content backwards first.
- **Auto-scroll on some gestures only**, with a wider band for connector drags as ticket 20 suggested. Had the feature been built, the rule would have been one predicate over ADR 0018's closed set — every gesture with an active phase that does not itself move the viewport, which admits `MarqueeSelect`, `MoveSelection`, `ResizeSelection` and `DragEdgeEnd` and excludes the rest without curation. A band that varies by gesture kind would have been the first number on this effort to do so, against ADR 0018's refusal of a per-kind threshold and ADR 0028's collapse of the port numbers to one.
