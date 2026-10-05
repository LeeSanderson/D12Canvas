# 75 — ResizeSelection

**What to build:** Resizing a single shape and resizing a multi-selection are one gesture over the preview (ADRs 0020, 0048 resize half). `ResizeSelection` takes the `resize-handle` and `selection-handle` roles. A resize rounds only the edges it moves to the nearest grid line that respects the minimum size (50 by 50, raised proportionally for a multi-selection), and a multi-selection resize snaps only its bounding box with members scaling proportionally inside it. The release commits exactly what the preview showed, one history entry per gesture, and a release below the threshold or back at the start leaves none. The container's resize path and the canvas's group-resize fields are deleted; the single-instance case arrives on the group path, as ADR 0020 says.

**Blocked by:** 74 (MoveSelection, the gesture preview and live geometry)

**Status:** resolved

- [x] Dragging any of the eight handles on a single shape or on the selection box resizes live, with the anchor edge fixed as today
- [x] Under snap, only the moving edge lands on a grid line; the opposite edge stays where it was
- [x] The minimum size holds for a single shape and the derived minimum holds for a multi-selection
- [x] A release outside the canvas commits; a stationary click on a handle leaves no history entry and keeps the selection
- [x] Attached edges follow the resize
- [x] The release-reliability theory gains `ResizeSelection`
- [x] Gesture-object tests over a fake context assert the published preview and the commit; the press-to-kind table gains the two handle rows
- [x] Resize, multi-selection resize and group move-resize tests that dispatched mouse events are rewritten at the new seams
- [x] A mid-resize visual baseline exists; full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit

## Comments

Built as specified. A shape's own handle and the selection box's handle both start `ResizeSelection`, which resizes the selection's bounding box and scales members inside it, so a single shape is the one-member case. Pressing the handle of a shape outside the selection, which only edit mode can show, selects that shape first. Members are placed by each edge's fraction of the box rather than by a scale factor, so an outer member edge lands exactly on the snapped line instead of a hair off it.

The old `ResizeInProgress` baseline was taken before the resize had rendered and showed the shape at its original size. Now that moves reach C# once per animation frame, the in-progress screenshots wait for the page to show the resize, so that baseline moves to the resized shape. The group one moves by floating-point digits only. The new mid-resize baseline drags the rectangle's left handle so the attached edge stays visible.

Not done here: ADR 0048 says `Ctrl` suppresses snapping, but neither move nor resize reads `Ctrl` yet, which belongs with ticket 86's live modifiers. Under snap, a handle dragged back to its start still commits if that edge began off-grid, because the edge snaps to a line; ADR 0048 implies this and no test pins it.
