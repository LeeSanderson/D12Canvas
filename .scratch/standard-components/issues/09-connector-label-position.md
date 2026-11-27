# Connector label position

Type: grilling
Status: resolved
Blocked by:
Map: [Better standard components](../map.md)

## Question

How does a user move a connector's label, and how is its position stored?

Today the label's `Bounds.X/Y` are never read; the label sits at the route's midpoint.

Decide:

- the stored position: a 0 to 1 fraction along the route, plus a perpendicular offset or none
- how the user drags it: pressing the label drags it along the line, or freely with the offset
- what happens when the route changes (endpoint moved, routing style changed): the fraction holds
- whether the label background masks the line under it
- snapping to the midpoint

## Answer

A label's place on its connector is a fraction along the route plus a signed perpendicular offset, both stored on `Edge`. Term in `CONTEXT.md`: Label position.

**Storage.** `Edge` gains `double LabelPosition` (0 to 1, default 0.5, the fraction of the route's length from the source end) and `double LabelOffset` (board pixels, default 0, positive to the left of travel from source to target). They stay flat scalars like `LineWidth` and `LinePattern`, not one record prop. The label instance's `Bounds.X/Y` stay unused, and only its width and height count. `EdgeRoute.LabelAnchor` becomes the point at `LabelPosition` along the route, with curves flattened into 64 chords as now. The label's centre is that anchor moved by `LabelOffset` along the route's local normal. On an orthogonal route the normal changes per segment, so a label dragged past a bend moves from above the line to beside it.

**Route changes.** Both values hold. Switching routing style, moving a shape or reconnecting an end recomputes the anchor on the new route, and nothing resets.

**Dragging.** Pressing an unlocked label and moving past the drag threshold drags it freely. Each move projects the pointer onto the nearest point of the route: that point's fraction becomes `LabelPosition`, and the signed distance to it becomes `LabelOffset`. There's no along-line-only mode.
- Dragging an unselected label selects its edge in the same gesture. A click with no drag still selects the edge, and a double-click still edits.
- Only the pressed label moves, even with several edges selected.
- The fraction is clamped to the stretch between the two arrowhead insets, so a label can't sit on a head. The offset has no cap and no leader line.
- A locked edge's label doesn't drag.
- One drag is one undo step, and Escape mid-drag puts the label back.
- An unlocked label shows the `move` cursor.

**Snapping.** While dragging, the fraction snaps to 0.5 when the anchor is within 8 screen pixels of the route's midpoint, and the offset snaps to 0 when the label's centre is within 8 screen pixels of the line. Holding Ctrl turns both off, following move and resize's grid-snap bypass. Alt was rejected because Alt-press on `edge-label` already cycles the hit stack and Alt during a move makes copies. No bend or segment-midpoint snaps, and no visual guide.

**Masking.** `.edge-label` always paints the canvas surface background token with about 2px 4px padding, so the line looks broken around the text. It's always on, with no prop to turn it off. On a board with a background image the label shows as a solid patch, which is accepted.

**Creation.** Double-clicking an edge puts the new label at the click point projected onto the route, with offset 0 and the midpoint snap applied. Any other way of adding a label uses 0.5 and 0.

**Not here.** The Property bar and Property panel get no label-position fields. Keyboard movement of a label joins the map's keyboard and accessibility fog. Persistence of the two props goes to [Persistence and migration](14-persistence-and-migration.md).
