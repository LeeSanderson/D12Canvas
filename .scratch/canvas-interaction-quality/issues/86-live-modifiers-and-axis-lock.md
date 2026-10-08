# 86 — Live modifiers, axis lock and snap-to-grid on by default

**What to build:** Holding Shift during a move locks the motion to one axis, holding Ctrl suppresses all snapping, and both take effect without the pointer moving (ADRs 0043, 0024 modifier half, 0011 amendment). One rule: a modifier that chooses the gesture or the selection is read once when it acts (Shift's append and toggle); a modifier that changes what the running gesture does is read live on every move. Ctrl is never read at press because Ctrl+click is the macOS secondary click, and a Ctrl+primary press on macOS is a secondary press behind a platform check. A modifier change with the pointer still re-sends the last position as a move from a capture-phase window key listener, with zero velocity, through the frame coalescer. `Axis lock` follows the press-anchored delta and is re-read every move; the locked axis is exempt from all snapping; Shift is unbound on resize. `SnapToGrid` defaults to on.

**Blocked by:** 75 (ResizeSelection)

**Status:** resolved

- [x] Shift pressed mid-move straightens the motion to the dominant axis from the press point; releasing it frees the motion; the locked axis never snaps
- [x] Ctrl pressed mid-move frees the shape from the grid at once, with no pointer movement; releasing it snaps again
- [x] Shift during a resize does nothing
- [x] On an Apple platform a Ctrl+primary press pans and opens the menu like a secondary press
- [x] `SnapToGrid` defaults to true; the demo pages that assumed off are updated deliberately
- [x] Gesture-object tests cover the live reads with a fake context; a probe proves a modifier keydown with the pointer still produces exactly one re-sent move
- [x] Full visual suite run in the pinned image with `-parallel none` (the snap default moves placements); baselines folded into the commit
- [x] `CONTEXT.md`'s `Axis lock` and `Snap-to-grid` terms describe what shipped

Shipped with a few choices the ADRs left open. A tie between the two axes keeps the horizontal motion. The re-send fires only once the press has crossed the drag threshold, so a modifier change on a press that is still a click cannot turn it into a drag. A Meta change re-sends as well, since the comparison is over all four modifiers. Pointer velocity does not exist yet, so the re-sent move carries no velocity field at all. Every demo page now takes the new default rather than pinning it off: no page is about free placement, and the baselines that moved are click-to-add and palette placements and drags landing on the 20px grid. The default also exposed `Math.Round(-0.5)` producing -0 and rendering as `-0px`, so grid rounding now goes through one `GridSnap.NearestLine` that normalises it. `IGestureContext.SnapToGrid` is gone, since the move gesture now rounds each axis on its own from `GridSpacing`.

Still open: `PointerGesture.TrySelectNextInHitStack` reads `Ctrl` at press to filter AltGr (ADR 0046), which is harmless on Apple platforms, where a Ctrl+primary press is now a secondary press, but is the one press-time Ctrl read left.
