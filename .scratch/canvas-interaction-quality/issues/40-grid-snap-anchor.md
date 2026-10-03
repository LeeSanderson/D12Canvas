# Which point of a selection grid snapping anchors

Type: grilling
Status: resolved

## Question

Decide which coordinate `Snap-to-grid` rounds when a selection is moved, and what that means for an entity whose size is not a multiple of the grid step.

Today `SnapBounds` rounds a `Bounds`' `X` and `Y`, which anchors the **top-left corner** and nothing else. So a 90-unit-wide entity on a 20-unit grid has its top-left on a grid line and its right edge at `x + 90`, permanently off every line. Two grid-snapped entities of different widths therefore still fail to line up on their facing edges, which is the thing a user turns grid snapping on to get.

Surfaced while resolving [Alignment guides and object snapping](11-alignment-guides-and-object-snapping.md), and it is only worth a ticket because of what that decision changed. While grid snapping was opt-in this was a wart a minority met; ADR 0024 makes it **on by default**, so it is now what everyone meets on their first drag.

Two things make this sharper than it looks.

**ADR 0014 already faced the same question for a different verb and answered it.** Align snaps the *target coordinate* rather than each entity's resulting position, precisely because rounding a whole `Bounds` "preserves align-left while pulling align-right and align-centre apart, since differing widths give differing X values that round to different lines". That is the identical failure, one verb over. It also recorded that `SnapBounds` is not reusable there and that a scalar coordinate snap is what is needed — so the primitive this ticket probably wants may already be owed to the codebase.

**ADR 0024's per-axis precedence gives a partial answer for free on any axis where object snapping fires**, since an object snap aligns a chosen edge exactly. The gap is only on axes governed by grid, which is every axis on a board with nothing nearby to align to.

Decide:

- **Which coordinate is rounded.** The top-left corner as today, the edge the drag is leading with, whichever of the three anchors per axis sits nearest a grid line, or the anchor that produces the smallest displacement.
- **Whether the answer differs for a resize**, where the moving edge is already the obvious candidate and the anchored edge must not move.
- **Whether it differs for a multi-entity selection.** ADR 0020 requires one rigid-body snap per tick with no branch on selection size, so whatever is chosen has to be expressible as a single offset applied to the selection's bounding box.
- **Whether placement is included.** `NewCenteredInstance` calls `SnapBounds` too, and a newly placed entity has no drag direction to lead with.
- **Whether entity *size* should ever be rounded.** Snapping width and height to the grid would make every edge land on a line permanently and would fix the problem at its root, at the cost of resizing content the user did not ask to resize. `SnapBounds` deliberately does not do this today.

Note that any change here is user-visible on a default-on feature, so it inherits ADR 0024's status as a behaviour reversal rather than an addition.

**Dependant added by ADR 0026 (ticket 16 resolved):** the keyboard nudge now steps to the next dominant grid line under `Snap-to-grid`, and it measures from whatever point this ticket settles rather than choosing its own. So this decision now has two readers instead of one — the pointer path's placement and move, and the arrow key. Worth noting that the nudge needs a **directional** ceiling and floor rather than `SnapBounds`'s rounding, which is a third snap primitive after ADR 0014 found `SnapBounds` non-reusable and needed a scalar coordinate snap; whichever anchor point this picks has to be expressible in all three.

## Answer

Recorded as ADR 0048, worked as a grilling.

**The ticket's framing was narrower than the defect.** `SnapBounds` has two callers, the single-instance move and placement, and resize does not grid-snap at all. Every built-in default size is a multiple of 20, so at about 1x top-left snapping already puts every edge of a placed built-in on a line. Off-grid widths come from a free resize, an odd host-registered size, or zooming out to the 200 step, and the first is the common one. No move anchor can fix it, because no anchor puts both edges of a 90-wide shape on a 20 grid.

Decided:

1. **Resize grid-snaps the edges it moves**, one for an edge handle and two for a corner. The anchored edge stays. This is the same anchor set ADR 0024 gave object snapping on resize, so per-axis precedence applies unchanged, and it uses ADR 0014's scalar coordinate snap.
2. **A move and a placement round the top-left of the selection's bounding box**, as today. Leading edge was rejected because it jumps when a drag reverses, and nearest-of-three because three stops per cell make a 20 grid behave like a 5 grid. Facing edges are object snapping's job.
3. **Size changes only through resize.** Placement keeps the host's `DefaultSize` exactly, and a move never rounds width or height.
4. **A multi-selection resize snaps the bounding box's moving edge only**, and members scale exactly inside it.
5. **The minimum size rules out lines.** The moving edge goes to the nearest line that respects `ResizeMath`'s floor, so it always ends on a line. The smallest snapped width is at most 70 at 1x and about 25 screen pixels at any zoom.
6. **Under snap, `Alt+Arrow` steps the moving edge to the next grid line**, with the same minimum rule. Snap off keeps ADR 0010's one-pixel step, and `Shift` keeps its anchor-flip meaning.

Accepted cost: an odd host size, or a shape smaller than a step at the current zoom, keeps its right and bottom edges off-grid on grid-governed axes until resized.

Amends ADR 0010 (keyboard resize), closes ADR 0024's deferral and extends grid snapping to resize, settles ADR 0026's nudge anchor, and confirms ADR 0020. `CONTEXT.md`'s `Snap-to-grid` entry is updated. No new tickets and no fog.
