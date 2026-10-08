# 91 — Auto endpoint

**What to build:** Dropping a connector anywhere on a shape's body attaches it so that it keeps choosing the side facing the other end (ADR 0027). `IEdgeEndpoint` gains a fourth shape, `AutoPortEndpoint(ComponentId)`, carrying the id and nothing else. It resolves to the standard port on the side an aiming line from the component centre to the other end's reference point crosses; the one crossing-free case, the other point inside the rect, takes the nearest side. Resolution is pairwise, so `Board` and `Live geometry` both resolve an edge's two ends together. Three drop zones ordered so the easier gesture gives the more forgiving result: within a port's target pins; anywhere else over a component gives auto; nothing floats. The drop resolves in C#. The same-endpoint guard widens to same-component. Persistence uses the envelope's previously unreachable combination (component named, no port named) with no new field and no schema bump. The keyboard pick (Enter on an instance stop) defaults to auto and cycles through auto, standard and custom ports with Space. An `Auto endpoint` never chooses a custom port.

**Blocked by:** 76 (DragEdgeEnd and SelectEdge)

**Status:** resolved

- [x] Dropping a connector on a shape's body attaches it and the edge leaves from the side facing the other end; moving either shape re-chooses the side
- [x] Dropping precisely on a port pins it; dropping on empty canvas floats it
- [x] A drop on the source shape's own body creates nothing
- [x] Enter on a focused shape picks auto first; Space cycles to Top, then the rest, then any custom port, then back to auto; Enter commits
- [x] A saved edge with an auto end round-trips through both load paths and a board saved under the current schema loads unchanged
- [x] Pure C# tests for resolution over all four sides and the inside case; serializer tests for the encoding
- [x] A baseline shows an auto-attached edge; full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [x] `CONTEXT.md`'s `Auto endpoint` term describes what shipped

## Comments

Built as specified. These calls were made along the way.

The side rule is `AutoPortSide.Facing`, a pure function of the bounds and the aimed-at point. Ties go to the side listed first in `StandardPorts.All`, so a line through a corner, coincident centres and a zero-area rect all resolve without a special case. `Board.ResolveEndpoint` now takes the other end, `Board.ResolveEnd(edge, isSource)` and `LiveGeometry.ResolveEnd` resolve an edge's pair, and the pending line during a drag resolves its anchor against the pointer.

`IEdgeEndpoint` gains `Guid? ComponentId`, as ADR 0027 said it would, and `EndpointAttachment` is gone.

The drop still reads the hit stack the listener sends at release, as ticket 76 left it. The topmost port or body of a component on the board decides: a port pins, a body or author content gives auto. An edge label's content above a shape is looked through to the shape, since a label is not a component. A port strip is not a pin target, so a drop on one falls through to the body and gives auto; ticket 92 replaces strips. The same-component guard covers carrying an existing end too: dropping it on the shape the other end is attached to changes nothing, so no gesture makes an edge from a shape to itself. The keyboard commit uses the same guard.

Not asked for, and worth a look: a press on the standard port an auto end currently resolves to carries that end, unless an end is pinned there. Without it an auto end could never be moved again, since it has no handle of its own. The cost is that a new edge cannot be pulled from that one port while the auto end sits on it.

The auto stage of port picking highlights all four standard ports at once, and no custom port. Nothing specified a cue, and without one Enter showed nothing at all. The spec's open "pin-versus-auto drop cue" is still open for the pointer.

Persistence: an auto end is written as `ComponentId` with `PortId`, `CustomPortId`, `X` and `Y` all null. A point is matched before the component-only case, so a floating end saved inside a shape loads as floating. A JSON literal of a current-schema board loads unchanged through both paths, and the partial path warns about an auto end on a missing instance as it does for the other attached shapes.

Baselines: two new ones on a new `/auto-endpoint-demo` page, an edge dropped on a shape's body and the keyboard pick's auto stage. A third test on the same page moves the target and checks the edge now leaves from the side facing the source. The full visual suite ran in the pinned image under `-parallel none`: 179 of 181 passed with no existing baseline moved, and the two failures were the new baselines, which are folded in.
