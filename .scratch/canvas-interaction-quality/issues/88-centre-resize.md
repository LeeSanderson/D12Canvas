# 88 — Centre resize

**What to build:** Holding Alt while resizing keeps the selection's centre fixed and mirrors the opposite edge, so a shape grows in place (ADR 0057). `Centre resize` is `ResizeSelection` with Alt, not a ninth gesture. The centre of the instances-only box read at press stays fixed, the opposite edge mirrors the handle, a toggle mid-gesture recomputes from the start box so the opposite edge jumps while the handle stays under the pointer, the minimum size clamps symmetrically, and on each driven axis both edges are snap anchors with the smallest correction winning. Members scale inside the box as on any resize.

**Blocked by:** 86 (Live modifiers, axis lock and snap-to-grid on by default)

**Status:** resolved

- [x] Alt+drag on a right handle grows both left and right edges equally about the centre; a corner handle drives both axes
- [x] Pressing or releasing Alt mid-resize switches modes with the handle staying under the pointer
- [x] The floor clamps symmetrically so the centre never moves
- [x] Under grid snap, whichever of the two moving edges needs the smaller correction wins
- [x] A mixed selection's centre is the instances-only box's centre
- [x] Gesture-object tests over a fake context; the release-reliability theory is unchanged because this is a mode, not a member
- [x] `CONTEXT.md`'s `Centre resize` term describes what shipped

Shipped with a few choices the ADR left open. The grid tie between the two edges goes to the handle's edge. A grid correction that would take the box below its minimum is applied and then clamped about the centre, so at the floor the box may sit off the grid; an object-snap match below the minimum is skipped instead, as on a plain resize, so a guide never describes a line the box did not reach. Guides on an object-snapped axis are drawn for both edges, so a mirrored edge that lands on a neighbour gets one. `ObjectSnap.ForEdge` and `GuidesForEdge` became `ForEdges` and `GuidesForEdges`, taking the list of moving edges. A new probe holds the pointer on a right handle, presses and releases a real Alt key and shows the shape growing about its centre and the far edge going back.

Still open: ADR 0057's Windows Alt-release menu-bar probe case is not written, because the probe for clone drag and Alt+click it would join does not exist yet (ticket 82's note).
