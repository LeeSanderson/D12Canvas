# A move snaps the selection's top-left to the grid, a resize snaps the edge it moves, and size changes only through resize

Under `Snap-to-grid`, three gestures round to the dominant grid line, and each rounds one thing:

| Gesture | What is rounded | What is never rounded |
|---|---|---|
| Move, placement | the top-left corner of the selection's bounding box | width and height |
| Pointer resize | each edge the handle moves: one for an edge handle, two for a corner | the anchored opposite edge |
| Keyboard resize (`Alt+Arrow`) | the moving edge, stepped to the next line in its direction | the anchored opposite edge |

ADR 0024 left this open. It made grid snapping on by default, so the off-grid far edge that `SnapBounds` leaves on any entity whose size is not a grid multiple is now what everyone meets on their first drag.

## Most of the defect came from resize, not from the anchor

The ticket asked which point a move should round. Reading the code changed the question. `SnapBounds` has two callers, the single-instance move and placement, and **resize does not grid-snap at all**. Every built-in default size is a multiple of 20 (160×100, 200×200, 200×40, 240×180), so at about 1x a freshly placed built-in already has all four edges on lines under top-left snapping.

An off-grid width comes from three places: a free resize, a host-registered size that is not a grid multiple, or zooming out until the dominant step is 200. The first is by far the most common, and it is also the one the anchor cannot fix, because no choice of anchor puts both edges of a 90-wide shape on a 20 grid. Snapping the edge a resize moves fixes it where it starts. A shape resized at a given zoom then has a width that is a whole number of steps, and top-left snapping on the next move puts all four edges on lines.

## Resize snaps the moving edge

This is the same anchor set ADR 0024 already gave object snapping on resize, "the edges actually moving", for the same reason: the moving edge is the only coordinate the user is aiming. Grid and object snapping then share one notion of what a resize aims, and ADR 0024's per-axis precedence applies unchanged, object snapping taking an axis where it fires and grid filling the rest. `Ctrl` suppresses both, as on a move.

It rounds one coordinate, so it uses the scalar coordinate snap ADR 0014 already needs for align. That is the same function, not a new one.

**A multi-selection resize snaps the bounding box's moving edge only.** Members scale exactly inside it, as ADR 0020 has them do, with no per-member rounding. That is the rigid-body rule ADR 0020 set for moves, and ADR 0014's rule that you snap the target rather than each result. The outermost members touch the box, so their outer edges land on the line. Inner member edges land wherever proportional scaling puts them.

**The minimum size rules out lines rather than overriding the snap.** `ResizeMath` sets a 50×50 floor, raised for a multi-selection by `MinBoundingBoxSizeFor`. The moving edge goes to the nearest grid line that leaves the shape at least that size, so it always ends on a line. At 1x the smallest snapped width is at most 70. At the 200 step it is at most 250 board units, about 25 screen pixels, which stays bounded because the step follows the dominant layer. A smaller size is one zoom or one held `Ctrl` away.

## A move keeps the top-left anchor

The top-left of the selection's bounding box is one offset applied to the whole selection, which is what ADR 0020's rigid-body snap needs. Both open-source reference tools anchor here too. The alternatives each fail on something concrete:

- **The leading edge** flips anchor when a drag reverses, so the shape jumps by the width's remainder under a pointer that barely moved. Placement has no direction to lead with, so it would need a fallback anyway.
- **Whichever of left, centre or right sits nearest a line** gives three stop positions per grid cell. A 90-wide shape on a 20 grid stops every 5 units, which feels like a slightly sticky free drag rather than a grid.
- **The smallest displacement** is the same as nearest-of-three on a single axis.

Lining up the facing edges of two different-width shapes is object snapping's job, and ADR 0024 gives it the axis whenever it fires.

## Size changes only through resize

Placement keeps a registered `DefaultSize` exactly. A host that chose 90×50 may have chosen it for its content, and overriding the host's own registration is worse than an off-grid edge. A move never changes width or height: that would resize content the user did not ask to resize, by an amount that depended on the zoom level at the time. `SnapBounds` already refused to round size, and this keeps the refusal.

**The accepted cost:** a shape with an odd host size, or one smaller than a step at the current zoom, keeps its right and bottom edges off-grid on axes the grid governs. Object snapping or a resize at that zoom repairs it.

## Keyboard resize follows the grid

ADR 0026 moved the arrow-key nudge to the next grid line under snap, because one-pixel steps took grid-aligned content off the grid. It left `Alt+Arrow` resize alone, which was coherent while pointer resize did not snap either. Once pointer resize leaves widths that are grid multiples, a single `Alt+→` moving the edge one screen pixel is the same defect ADR 0026 fixed for the nudge.

So, under `Snap-to-grid`, **each `Alt+Arrow` press moves the moving edge to the next grid line in its direction**, using the directional ceiling and floor the nudge already needs, and the minimum-size rule above. With snap off, ADR 0010's `1 / zoomScale` step is untouched. `Shift` keeps its anchor-flip meaning from ADR 0010, so there is still no larger-step variant. At the 200 step a press moves about 20 screen pixels, the same bounded range ADR 0026 accepted for the nudge.

## What this amends

**ADR 0024's deferral is closed**, and grid snapping now covers resize as well as placement and move.

**ADR 0026's nudge measures from the top-left of the selection's bounding box**, which it explicitly left to this decision.

**ADR 0010 is amended in a fifth place**: keyboard resize steps to grid lines under `Snap-to-grid`.

**ADR 0020 is confirmed.** It left the anchor to the alignment-guides decision, which passed it here, and the rigid-body snap per tick is used as written.

**ADR 0014 gains a second reader of its scalar coordinate snap.** Nothing in it changes.

## Considered and rejected

- **Leaving resize unsnapped**: keeps the main source of off-grid widths, which no move anchor can repair.
- **The leading edge as the move anchor**: jumps when a drag reverses, and placement has no direction.
- **The nearest of left, centre or right per axis**: three stops per cell, so a 20 grid behaves like a 5 grid.
- **The smallest displacement**: the same as nearest-of-three on one axis.
- **Rounding a placed instance's size to the grid**: overrides the host's registered size.
- **Rounding size during a move**: resizes content the user only moved, by a zoom-dependent amount.
- **Snapping each member's edges in a multi-selection resize**: breaks the members' proportions and needs a branch on selection size, which ADR 0020 rules out.
- **Snap, then clamp to the minimum**: leaves the moving edge off-grid at the floor.
- **Leaving `Alt+Arrow` at one pixel under snap**: the first keyboard press undoes what the pointer resize just aligned.
