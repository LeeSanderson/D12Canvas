# 87 — Object snapping, alignment guides and equal-spacing snap

**What to build:** An optional object-snapping toggle aligns the edges and centres of what the user drags to nearby shapes and draws a full-width guide along the match (ADR 0024). A host binds it beside snap-to-grid as a second parameter pair, off by default; the toggles are independent. Both resolve per axis: object wins where it fires and grid fills the other; Ctrl suppresses both. Nine anchor pairings per axis over the selection box as a rigid body; candidates are the on-screen entities including locked ones and LOD placeholders, excluding edges, floating endpoints and groups; tolerance 8 screen pixels; sticky to 1.75 times tolerance; skipped above 3 screen pixels per millisecond; the search runs twice per move so the guide describes where the selection now is. `Equal-spacing snap` is moves only, with candidates filtered by perpendicular overlap. Resize snaps point-wise for the moving edges. Placement and paste get grid only. `Alignment guide`s are full-bleed board-space lines in the guide `Paint layer`, at half intensity, reading a new content-role token that is not the accent, drawn with `vector-effect="non-scaling-stroke"`. The prototype on `prototype/snap-guides` is evidence, not code to merge.

**Blocked by:** 79 (Four paint layers), 86 (Live modifiers, axis lock and snap-to-grid on by default)

**Status:** resolved

- [x] With object snapping on, dragging a shape near another's edge or centre pulls it to the match within tolerance and draws a guide spanning the viewport along it
- [x] Grid snapping fills whichever axis object snapping did not fire on; Ctrl frees both
- [x] Dropping a third shape beside two equally spaced ones snaps to the same gap
- [x] A fast pointer sees no snapping; slowing down brings it back
- [x] The guide reads its own token, not the accent, in both themes
- [x] The host-facing toggle pair exists and the canvas menu row for it is added when ticket 89 lands (or here if 89 has already landed)
- [x] Constants are asserted by ordering (threshold 4 < tolerance 8 < affordance floor < edge band 20), never by value
- [x] Pure C# tests for the anchor search and equal-spacing candidates; gesture-object tests for per-axis resolution
- [x] A demo page shows a guide mid-drag for the visual suite; full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [x] `CONTEXT.md`'s `Object snapping`, `Alignment guide` and `Equal-spacing snap` terms describe what shipped

Shipped with a few choices the ADR left open. The fast-pointer cut-off stands object snapping down and drops what it held, but the grid still snaps, so a flung shape never commits off-grid. Velocity is measured in JavaScript between consecutive pointer events, in screen pixels per millisecond, and sent on every coalesced move; a move re-sent for a modifier change or re-run for a viewport change carries zero. On a move, an axis whose match leaves nothing to draw from the snapped position goes back to the grid, which happens when the other axis's snap carries the selection out of the row an equal-spacing match came from. Equal spacing offers the row's gaps after or before any row member, and centred between two members with room for the mover. A resize match that would take the box below its minimum size is skipped and the grid takes the edge. An equal-spacing guide is a segment across each matching gap with screen-sized caps, drawn in the alignment-guide token. `OnToggleObjectSnappingPressed` flips the parameter and notifies the binding, ready for ticket 89's menu row. The drag threshold now comes from C# through the listener's options, and the edge hit band through `--d12-edge-hit-band` on the canvas content, so both join the ordering test; that moved every HTML baseline by the one new custom property.

Still open: the canvas menu row waits for ticket 89. The affordance floor does not exist as a constant yet, so the ordering test covers drag threshold < tolerance < edge band only. Locked entities are not candidates because locking does not exist; the locking ticket should add them to `SnapCandidates` deliberately rather than filter them out.
