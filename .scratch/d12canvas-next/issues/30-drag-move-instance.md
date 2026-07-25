# 30 — Drag-move an instance

**What to build:** An end user drags a selected component instance to move it. Movement is correct at any zoom level (screen delta converted to board delta), the instance's bounds update on the `Board`, and the whole press-to-release drag is one gesture — the unit undo/redo will later operate on.

**Blocked by:** 29 (Click select, escape, and `aria-selected`)

**Status:** resolved

- [x] Dragging moves the instance with correct zoom-relative coordinates
- [x] Bounds on the `Board` reflect the final position after release
- [x] The complete press-to-release drag is treated as a single gesture
- [x] Screenshot case for a mid-drag state
- [x] bUnit/xUnit coverage of bounds updates

## Comments

`ComponentContainer` gains a new `EventCallback<Bounds> OnMoved`, fired exactly once - on
release - with the instance's final `Bounds`. This deliberately does **not** reuse the
pre-existing `_editMode`-gated `_isDragging`/`ApplyResize` fields (legacy code predating the
Board-backed canvas, still used standalone by `ComponentContainerDemo.razor`): a new, independent
`_isMoving`/`_moveStart`/`_moveStartX`/`_moveStartY` set of fields arms on mousedown whenever
`IsSelected && !_editMode && !_isResizing` (the `!_isResizing` guard stops it engaging on top of
a resize-handle's own mousedown, which still bubbles up from the handle first). While armed,
`HandleMouseMove` mutates the container's own `X`/`Y` parameters directly for live visual
feedback (reusing the existing `deltaX /= ParentCanvas.ZoomPanTracker.Scale` conversion - pan
cancels out of a delta, only scale matters) and lets Blazor's automatic post-event render pick it
up; `Board` itself only hears about it once, via `OnMoved`, on `HandleMouseUp` - and only if the
position actually changed, so a plain click on an already-selected instance is a no-op rather than
a wasted mutation/render. `DiagramCanvas.MoveComponent(instanceId, bounds)` is the single write
point: `board.GetComponent(id).Bounds = bounds`, then `StateHasChanged()`.

One real bug surfaced and got fixed along the way: `ComponentContainer`'s root div had
`@onclick:stopPropagation` but no equivalent for mousedown/mousemove/mouseup, so those events
bubbled up to `DiagramCanvas`'s own pan handlers underneath. In a live browser this meant dragging
a selected instance would simultaneously pan the canvas (mousedown on the instance also armed
`DiagramCanvas._isPanning`); in bUnit it was worse - the pan handler's own `StateHasChanged()`
re-render re-pushed `X="instance.Bounds.X"` down as a parameter, silently reverting the
in-progress drag on every tick. Added `@onmousedown:stopPropagation`, `@onmousemove:stopPropagation`,
and `@onmouseup:stopPropagation` alongside the existing click one - this also incidentally fixes
the same latent conflict for the legacy edit-mode drag/resize path.

bUnit coverage: `DiagramCanvasDragMoveTests.cs` (zoom-relative delta move, Board unchanged
mid-drag with the DOM style reflecting the live position, Board updated only on release, dragging
an unselected instance is a no-op, a no-movement press/release is a no-op) plus two cases in
`ComponentContainerTests.cs` exercising `OnMoved` directly.

New Playwright visual coverage (`DragMoveVisualTests.cs`, reusing `/placement-demo`): a real
`Mouse.Down`/`Move` sequence (not the synthetic-dragover workaround ticket 27 needed - this is a
plain mouse gesture, not native HTML5 drag-and-drop, so Chromium repaints normally while it's in
flight) captures a mid-drag screenshot, and a second test captures the board after release.
Baselines generated via the pinned `mcr.microsoft.com/playwright/dotnet:v1.61.0-noble` Docker
image per the README process.

Also hit and fixed a pre-existing environment issue while generating baselines: inside that pinned
image (.NET SDK 10.0.301 - newer than this machine's local 10.0.101), `Verify.XunitV3`'s
conditional `global using static VerifyXunit.Verifier;` (injected via the package's own
`buildTransitive` props, conditioned on `$(ImplicitUsings)`) silently failed to apply, breaking
compilation of every existing visual test file with `CS0103: The name 'Verify' does not exist`,
not just this ticket's new one. Added an explicit `D12Canvas.VisualTests/GlobalUsings.cs` with the
same usings the package would otherwise inject, which is SDK-version-independent and harmless to
have alongside the package's own (identical duplicate `global using`s are a no-op in C#).
