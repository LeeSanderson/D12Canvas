# Drag past the viewport edge

Type: prototype
Status: claimed

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
