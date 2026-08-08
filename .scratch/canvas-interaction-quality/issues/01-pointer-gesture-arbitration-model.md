# Pointer gesture arbitration model

Type: grilling
Status: open
Blocked by: 02, 03, 04

## Question

Design the model that decides, for any pointer press on the canvas, which single gesture owns it — and holds that ownership unambiguously until release.

There is no such model today. Arbitration is an emergent property of three separate mechanisms interacting: `@onmousedown:stopPropagation` / `@onmouseup:stopPropagation` directives in markup, the bubble ordering between a port or resize handle and the `ComponentContainer` beneath it, and a set of boolean flags (`_isMoving`, `_isResizing`, `_isPortDragging`, `_isDragging` on `ComponentContainer`; `_isPanning`, `_isMarqueeSelecting`, `_isGroupMoving`, `_isGroupResizing`, `_isConnectingPort` on `DiagramCanvas`) read in a specific order inside each handler. Correctness depends on reading all three together — the comments in `ComponentContainer.HandleMouseDown` exist precisely because the ordering is not self-evident.

This is the spine of the map. Most other tickets here either add a gesture to this space or change how an existing one is triggered, and each would otherwise add another flag and another ordering constraint.

Decide:

- What the unit of arbitration is, and what to call it — `CONTEXT.md` has no term for this today. Where does it live: a service, a cascaded value, a type owned by `DiagramCanvas`, something else?
- How a press is classified into exactly one gesture. A fixed priority table? Hit-region ownership? Negotiation between candidates?
- Where ownership lives once claimed, and how a gesture guarantees it is released — see ticket 04 for the concrete leak paths this must close, which is the strongest single argument for a real owner rather than distributed flags.
- How this composes with gestures the canvas does not own: HTML5 drag-and-drop placement (`@ondrop`), inline text editing, and the `isEditableTarget` guard the keyboard shortcuts already use.
- How the model stays expressible over pointer/touch input later without redesign (map Notes: non-foreclosure constraint). Note that button-number and hover concepts, which ticket 07 leans on heavily, have no touch equivalent.
- Whether this supersedes ADR 0009 outright or amends it. ADR 0009 documents the interaction *surface* (what gestures exist) but never the arbitration *mechanism*.

Ticket 02's findings constrain what the browser actually delivers; ticket 03's teardown supplies the reference bar for which gestures should exist at all; ticket 04 supplies the evidence of how the current arrangement fails.
