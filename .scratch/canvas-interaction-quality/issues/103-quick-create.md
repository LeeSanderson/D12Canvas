# 103 — Quick create

**What to build:** A plain click on a selected shape's port creates a connected duplicate beside it on that side and moves selection and focus to the new shape, and Ctrl+Arrow does the same from the keyboard, so chaining shapes is rapid (ADR 0030). `Quick create` is `DragEdgeEnd` releasing from `pointing` on a port span. It builds a true duplicate (same type, props and size as the source) through the duplication path so every id regenerates, places it at the source's border on the pressed side plus a gap of two dominant grid spacings, steps along the same axis while the slot is occupied, pins the edge at the source port and takes an `Auto endpoint` at the target, moves selection and focus to the new instance, and is one history entry. Ctrl+Arrow reaches the four standard ports. Quick create does not start a `Duplicate run`. An `IInlineEditable` type then opens for editing with its text selected, panned into view first. The port span's old double-click-to-add-port is already retired by ticket 92.

**Blocked by:** 92 (Border partition and ports on selection), 96 (Duplicate and the duplicate run), 101 (The canvas starts every inline edit)

**Status:** resolved

- [x] Clicking the right port of a selected rectangle creates an identical rectangle to its right, connected, selected and focused, in one history entry
- [x] Ctrl+Right on a focused shape does the same; Ctrl+Down places below
- [x] A second quick create from the new shape steps past the occupied slot
- [x] Quick create on a text opens the new text for typing with its label selected
- [x] A press on a port that moves past the threshold is an ordinary connector drag and creates nothing
- [x] Escape on the port press before release creates nothing at release
- [x] Gesture-object tests for the click outcome; bUnit for the chord; the press-to-kind table is unchanged
- [x] A baseline shows a chain of three; full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [x] `CONTEXT.md`'s `Quick create` term describes what shipped

Shipped with three choices the ADR did not spell out. A quick create steps out of an entered group, as paste and duplicate do, because the copy is added at the board's top level; so an abandoned copy of a grouped source hands focus and selection to the source's top-level group rather than the member. A click on a port that an edge end is pinned to quick-creates from that port rather than carrying the end, since the carry needs movement. Under snap the candidate box is snapped before the occupancy test, and a box that only touches another counts as occupied, which is how `Bounds.Intersects` already reads.

Still open: the pan into view before an edit is still a plain pan write until a framing transition exists (ticket 106).
