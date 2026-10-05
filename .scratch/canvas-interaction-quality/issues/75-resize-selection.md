# 75 — ResizeSelection

**What to build:** Resizing a single shape and resizing a multi-selection are one gesture over the preview (ADRs 0020, 0048 resize half). `ResizeSelection` takes the `resize-handle` and `selection-handle` roles. A resize rounds only the edges it moves to the nearest grid line that respects the minimum size (50 by 50, raised proportionally for a multi-selection), and a multi-selection resize snaps only its bounding box with members scaling proportionally inside it. The release commits exactly what the preview showed, one history entry per gesture, and a release below the threshold or back at the start leaves none. The container's resize path and the canvas's group-resize fields are deleted; the single-instance case arrives on the group path, as ADR 0020 says.

**Blocked by:** 74 (MoveSelection, the gesture preview and live geometry)

**Status:** ready-for-agent

- [ ] Dragging any of the eight handles on a single shape or on the selection box resizes live, with the anchor edge fixed as today
- [ ] Under snap, only the moving edge lands on a grid line; the opposite edge stays where it was
- [ ] The minimum size holds for a single shape and the derived minimum holds for a multi-selection
- [ ] A release outside the canvas commits; a stationary click on a handle leaves no history entry and keeps the selection
- [ ] Attached edges follow the resize
- [ ] The release-reliability theory gains `ResizeSelection`
- [ ] Gesture-object tests over a fake context assert the published preview and the commit; the press-to-kind table gains the two handle rows
- [ ] Resize, multi-selection resize and group move-resize tests that dispatched mouse events are rewritten at the new seams
- [ ] A mid-resize visual baseline exists; full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
