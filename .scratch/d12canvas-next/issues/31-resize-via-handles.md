# 31 — Resize via handles

**What to build:** A selected component instance shows resize handles; the end user drags a handle to resize, with the opposite edge/corner staying anchored. Resizing is zoom-relative correct and the whole handle-drag is one gesture.

**Blocked by:** 29 (Click select, escape, and `aria-selected`)

**Status:** resolved

- [x] Resize handles appear on the selected instance
- [x] Dragging a handle resizes with the opposite edge/corner anchored
- [x] Resizing is correct at any zoom level; bounds can never invert or go negative
- [x] The complete handle-drag is a single gesture
- [x] Screenshot cases: handles visible, mid-resize state
- [x] bUnit/xUnit coverage of bounds updates and anchoring

## Comments

The pre-existing `ApplyResize`/`ResizeDirection` anchor math and the 8 `resize-handle` divs already
existed (prototype-era, per spec.md's "container drag/resize mechanics ... proven starting
material") but had two gaps: the handles rendered unconditionally on *every* instance regardless of
selection state, and `HandleMouseMove`'s resize branch was nested inside an `if (!_editMode) return;`
guard, so a selected-but-not-editing instance's handle drag never actually resized anything (the
handle's own `mousedown` still armed `_isResizing`, but the following `mousemove`s were silently
dropped). Fixed both: the 8 handle divs now render behind `@if (IsSelected || _editMode)` -
selected for the new Board-backed flow, `_editMode` preserved so the legacy standalone
`ComponentContainerDemo.razor` path (no Board/selection wiring) keeps working unchanged - and the
`_isResizing` branch in `HandleMouseMove` moved above the `_editMode` gate so it now applies
whenever armed, exactly mirroring the reasoning ticket 30 used for `_isMoving`.

`ComponentContainer` gains `EventCallback<Bounds> OnResized`, fired exactly once - on release, with
the instance's final `Bounds` - matching `OnMoved`'s contract from ticket 30 down to the
no-op-on-no-movement guard. `DiagramCanvas.ResizeComponent(instanceId, bounds)` is the single write
point, mirroring `MoveComponent`.

No changes were needed to the anchor/clamp math itself (`ApplyResize`'s `Math.Max(_, minWidth/
minHeight)` per direction already keeps bounds from inverting or going negative at any zoom level,
since `ScaledDelta` - reused unchanged - already divides by `ZoomPanTracker.Scale`) - only the
gating around it.

bUnit coverage: `ComponentContainerTests.cs` gained handle-visibility-gating tests (selected /
edit-mode / neither) and per-handle-direction tests (`bottom-right` grows with the top-left corner
anchored, `top-left` grows with the bottom-right corner anchored, a huge inward drag clamps to the
50x50 minimum without inverting, a press-release with no movement is a no-op). New
`DiagramCanvasResizeTests.cs` (mirroring `DiagramCanvasDragMoveTests.cs`) covers the Board-level
seam: zoom-relative scaling, Board unchanged mid-resize with the DOM reflecting the live size, and
Board updated only on release.

New Playwright visual coverage (`ResizeVisualTests.cs`, reusing `/placement-demo`): a "handles
visible" baseline for a selected-but-not-resizing instance, and a mid-resize baseline via a real
`Mouse.Down`/`Move` sequence on the bottom-right handle (same plain-mouse-drag technique as ticket
30's `DragMoveVisualTests`, not the synthetic-dragover workaround ticket 27 needed). Regenerating
baselines (via the pinned `mcr.microsoft.com/playwright/dotnet:v1.61.0-noble` Docker image, per the
README process, run through `podman machine start` + `docker context use default` since Docker
Desktop's own service can't start here) surfaced the expected knock-on: `BoardRenderingVisualTests`,
`ClickToAddPlacementVisualTests`, and `DragAndDropPlacementVisualTests.DroppedInstance` all place
*unselected* instances, so their baselines previously (incorrectly) included the 8 always-on
resize-handle dots - those were regenerated to reflect the dots now correctly disappearing when
unselected. `SelectionVisualTests` and both `DragMoveVisualTests` cases (their instance is selected)
only picked up harmless whitespace/comment-marker diffs from the new `@if` wrapping - zero pixel
change, confirmed via diffing the `.verified.html` before promoting.
