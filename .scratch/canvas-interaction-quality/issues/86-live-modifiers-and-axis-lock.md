# 86 — Live modifiers, axis lock and snap-to-grid on by default

**What to build:** Holding Shift during a move locks the motion to one axis, holding Ctrl suppresses all snapping, and both take effect without the pointer moving (ADRs 0043, 0024 modifier half, 0011 amendment). One rule: a modifier that chooses the gesture or the selection is read once when it acts (Shift's append and toggle); a modifier that changes what the running gesture does is read live on every move. Ctrl is never read at press because Ctrl+click is the macOS secondary click, and a Ctrl+primary press on macOS is a secondary press behind a platform check. A modifier change with the pointer still re-sends the last position as a move from a capture-phase window key listener, with zero velocity, through the frame coalescer. `Axis lock` follows the press-anchored delta and is re-read every move; the locked axis is exempt from all snapping; Shift is unbound on resize. `SnapToGrid` defaults to on.

**Blocked by:** 75 (ResizeSelection)

**Status:** ready-for-agent

- [ ] Shift pressed mid-move straightens the motion to the dominant axis from the press point; releasing it frees the motion; the locked axis never snaps
- [ ] Ctrl pressed mid-move frees the shape from the grid at once, with no pointer movement; releasing it snaps again
- [ ] Shift during a resize does nothing
- [ ] On an Apple platform a Ctrl+primary press pans and opens the menu like a secondary press
- [ ] `SnapToGrid` defaults to true; the demo pages that assumed off are updated deliberately
- [ ] Gesture-object tests cover the live reads with a fake context; a probe proves a modifier keydown with the pointer still produces exactly one re-sent move
- [ ] Full visual suite run in the pinned image with `-parallel none` (the snap default moves placements); baselines folded into the commit
- [ ] `CONTEXT.md`'s `Axis lock` and `Snap-to-grid` terms describe what shipped
