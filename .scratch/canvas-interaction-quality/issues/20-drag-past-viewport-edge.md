# Drag past the viewport edge

Type: prototype
Status: resolved

## Question

Decide whether a drag that reaches the edge of the viewport scrolls the canvas — and if so, with what dead time, band width and ramp.

ADR 0011 made the board unbounded in extent, which quietly removed the only thing that made this optional: you currently cannot drag an instance further than one viewport, in any direction, because nothing pans while the pointer is held. Marquee selection has the same ceiling — a selection can only ever be as large as what is on screen.

Ticket 03 found tldraw ships this with published constants worth prototyping against rather than inventing from scratch: a 200 ms dead time before any scrolling begins, a 200 ms ease-in, an 8 px edge band widened by 12 px for coarse pointers, and damping on small viewports. **The 200 ms dead time is the whole design** — without it, brushing the edge of the viewport flings the camera and the gesture becomes unusable. Excalidraw does not have edge auto-scroll and carries an open feature request for it, which is its own data point.

Prototype and decide:

- **Whether it applies to every dragging gesture or only some.** Instance move, group move, marquee, resize and connector-drag all reach the edge; they do not obviously all want the same answer, and a connector drag in particular may want a wider band.
- **The constants, by feel rather than by reasoning** — dead time, band width, ramp shape, maximum velocity, and whether velocity scales with how far past the edge the pointer sits.
- **Whether the coarse-pointer band widening is worth carrying now**, given the map's non-foreclosure constraint says touch must stay cheap to add.
- **What happens at the ends of a gesture that auto-scrolled**: whether the pan is part of the same history entry as the drag (ADR 0007 holds a gesture is exactly one entry, and ADR 0003 holds pan as unpersisted view state, so this may be a non-question — confirm rather than assume).
- **How it interacts with ticket 11's snapping and alignment guides**, which are computed in board space while the viewport is moving underneath them.

Must compose with whatever ticket 01 makes the owner of an in-flight gesture — the auto-pan is driven by a gesture that is already running, so it belongs to that owner rather than to `DiagramCanvas`'s pan handling.

## Answer

**Not built.** A drag that reaches the edge of the viewport stops there. Recorded as **ADR 0029**.

The five bullets were never reached, because the question underneath them turned out to be whether the feature is affordable at all, and it is not at an acceptable price.

**Auto-scroll is a velocity, so it needs a clock, and the model has none.** ADR 0018's four `[JSInvokable]` methods are all pointer events and ADR 0020 rate-limits `OnPointerMoved` to one call per animation frame; `DiagramCanvas.razor.cs:1716-1718` says outright that the mounted window follows that cadence "never by a per-frame timer". Everything then hangs on whether the browser keeps delivering `pointermove` while the operating system clamps the cursor at the edge of the screen.

**Measured rather than assumed, by hand.** Playwright dispatches synthetic input at whatever coordinates it is given and never reaches the clamp — the wall ADR 0019 hit with the synthetic 120px notch, and an instance of the device physics ADR 0025 named as out of the suite's reach. A hand-driven probe on `prototype/edge-autoscroll` found **moves at unchanged coordinates stay at zero while silence climbs into the hundreds of milliseconds and animation frames keep counting**. No redundant events at a frozen coordinate, no `movementX` against an unchanging `clientX`. The maximised canvas is the case that fails: an embedded one keeps receiving moves while the pointer crosses the host page, so a move-driven scheme half-works there and fails completely full-window, which is the rule-that-holds-for-some-cases shape this effort exists to remove.

**There is no cheap middle, which is what makes the decision binary.** Displacement scrolling needs no clock but is not a smaller feature: it still needs the band, the ramp, the constants, a board-anchored delta and a windowing pin rule, and it still dies on a maximised canvas, where each recovery pump drags the content backwards before it goes forwards. The other clocks are dead for reasons — a C# `PeriodicTimer` is not frame-aligned and costs a wire render per tick on Blazor Server, a CSS animation moves pixels without moving `ZoomPanTracker` so hit-testing, windowing, snap candidates and the preview all disagree with the screen, a native scroll container needs the scrollable extent ADR 0011 removed, and `pointerrawupdate` is Chromium-only (left unmeasured).

**The ticket's marquee premise was already false when it was worked.** It claims a selection can only ever be as large as what is on screen; **ADR 0022 made the marquee additive** three tickets earlier, as the payoff for moving pan off the primary button, so `Shift`-marquee across pans has no ceiling. What remains is smaller than the ticket claimed: drag, resize and connector-drag over more than a viewport take drag-release-pan-drag where they could take one gesture. Zoom-out answers it with the advantage auto-scroll cannot match — source and destination visible at once — and ADR 0013's paste anchor already answers the long move.

**Declining confirms two ADRs rather than merely leaving them alone.** ADR 0018's `final delta = release − press` stays screen-anchored: auto-scroll would have forced it to be re-read in board space, soundly but unnecessarily, since nothing pans mid-gesture except `Pan`, whose board delta is zero by construction. And ADR 0020's windowing argument stays true — it rests on the viewport being changed only by a pan, and auto-scroll is precisely the case that would have made `MoveSelection` change it, with `Overscan` at 200 board units meaning a dragged instance leaves the mounted window in under a second and the user is left dragging nothing. The "no pin rule to write or forget" claim survives because of this decision, not despite it.

**Scoped to this effort, with a stated trigger.** tldraw ships it (`edgeScrollDelay: 200`, `edgeScrollEaseDuration: 200`, `edgeScrollSpeed: 25`, `edgeScrollDistance: 8`, `coarsePointerWidth: 12`); Excalidraw does not and carries an open request. Reopen on a host complaint, or if anything else needs a per-frame signal during a gesture — the heartbeat is small, and the cost is in what it then obliges.

Probe on `prototype/edge-autoscroll` (`25ed6d7`).
