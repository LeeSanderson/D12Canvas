# 33 — Multi-selection acts as one unit

**What to build:** An end user moves or resizes a multi-selection as a single bounding-box unit: dragging any member moves all of them preserving relative offsets; resize handles on the selection's bounding box scale all members proportionally. Each drag is one gesture. (ADR 0006.)

**Blocked by:** 30 (Drag-move an instance), 31 (Resize via handles), 32 (Marquee + shift-click multi-select)

**Status:** resolved

- [x] A bounding box is computed over the multi-selection and shown
- [x] Moving the selection preserves every member's relative offset
- [x] Resizing the bounding box scales member bounds proportionally
- [x] The complete drag (move or resize) is a single gesture
- [x] Screenshot case for a multi-selection with its bounding box
- [x] bUnit/xUnit coverage of unit move/resize maths

## Comments

**An interaction-model decision this ticket had to make, not pinned by ADR 0006 or the prior
tickets' own comments**: a group move can start two different ways - dragging one of the selected
members directly, or dragging empty space inside the combined bounding box (the gesture ticket 32
left deliberately inert, reserved for this ticket). Both had to move the *whole* selection, but a
real-browser constraint ruled out handling them identically: every `ComponentContainer` stops
`mousemove` propagation on itself, so a canvas-level `HandleMouseMove` (which is what would need to
drive a live, synchronized preview across every member) never reliably fires while the cursor
stays over the member being dragged - true in a real browser even though bUnit's synthetic events
don't reproduce it. So the two paths are handled differently:

- **Member-drag**: `ComponentContainer`'s own existing `_isMoving`/local-X-Y tracking (ticket 30,
  untouched) already handles the grabbed member smoothly and correctly for exactly this reason.
  `DiagramCanvas.MoveComponent` is the single interception point - when the moved instance is part
  of a 2+ selection, it computes the delta between that instance's own before/after `Bounds` and
  applies the same delta to every other selected member in one write (`CommitGroupMove`). The
  grabbed member tracks the cursor live; its groupmates visually update once, on release - an
  accepted trade-off (matching the codebase's own existing precedent: `_isMoving` never gives the
  parent live mid-drag visibility at all, only the final `OnMoved`).
- **Empty-space-in-bbox drag**: this one *is* driven entirely by `DiagramCanvas`'s own mousedown/
  mousemove/mouseup (no propagation problem, since it never starts on a member), so it gets full
  live preview: `EffectiveBounds(instance)` returns each selected member's `Bounds` offset by the
  in-progress delta, and both the members and the bounding-box overlay track the cursor in real
  time. Board itself is untouched until release either way - one write, matching every other
  gesture's discipline (verified explicitly, mirroring tickets 30/31's own "unchanged mid-drag"
  tests).

**Resize** is exclusively driven by the selection bounding box's own overlay (`.selection-bounding-
box`, rendered by `DiagramCanvas` whenever 2+ are selected - always shown, not just mid-gesture, so
it also satisfies "a bounding box is computed and shown" on its own). Its 8 `.group-resize-handle`
children are the only new interactive elements; the main box itself is `pointer-events: none` so
clicks/drags on a member underneath (for select or member-drag-move) pass straight through
unaffected. Individual members' own resize handles are suppressed while part of a 2+ selection (new
`ComponentContainer.IsMultiSelected` parameter) so a group resize never collides with 8 more
handles per member underneath it; `ShouldRender` had to gain an `IsMultiSelected` check too, since
toggling multi-select membership alone (no X/Y/W/H/editMode/IsSelected change) wouldn't otherwise
trigger a re-render and the handles would fail to hide/show promptly.

Resize math: `ResizeMath.Apply` (new, shared) extracted the 8-direction anchor/clamp switch
statement out of `ComponentContainer.ApplyResize` (ticket 31's code, unchanged behaviour - re-
verified against its own existing tests and baselines) so `DiagramCanvas`'s group resize could
reuse the exact same anchoring logic against the selection's combined bbox instead of duplicating
it. Each member's proportional scale is computed relative to that bbox
(`ScaleWithinBoundingBox`): its start-of-gesture relative offset/size within the bbox, re-expressed
against the bbox's current (possibly live-preview) extent - the same helper backs both the live
preview (via `EffectiveBounds`) and the final commit. `ResizeMath` also grew `DefaultMinWidth`/
`DefaultMinHeight` (=50) as the one shared source for both `ComponentContainer`'s own per-instance
floor and the group-resize floor derived below - previously encoded independently in two places.

**Two real bugs surfaced by `/code-review`'s Standards/Spec pass, both fixed before this ticket was
considered done:**

1. A stationary click (mousedown+mouseup, no movement) on a `.group-resize-handle` bubbled an
   unstopped native `click` all the way up to `.diagram-canvas`'s own `HandleCanvasClick`, wiping
   the entire selection the handle belongs to - `_dragMoved` (the guard every other gesture relies
   on) is only ever set inside `HandleMouseMove`, which never runs for a click with no drag in
   between. Individual `ComponentContainer` resize handles don't have this problem because their
   *container* stops all four pointer events including click; the group handles, not nested inside
   one, needed their own `@onclick:stopPropagation` added directly. bUnit can't reproduce the
   failure mode faithfully (a target with `@onclick:stopPropagation` and no handler of its own
   throws `Bunit.MissingEventHandlerException` rather than silently no-op'ing like a real browser),
   so the regression test lives in `MultiSelectionMoveResizeVisualTests.
   StationaryClickOnGroupResizeHandle_DoesNotClearSelection` instead, against a real browser.
2. A group resize could scale an individual member below the 50x50 floor a lone instance's own
   handles have always enforced (ticket 31) - the old flat `MinGroupBoundingBoxSize` (50) only
   floored the *bbox*, so a small member inside a much larger selection could be scaled down to a
   few pixels before the bbox itself hit its own floor. Fixed by deriving the bbox's minimum
   width/height per gesture (`MinBoundingBoxSizeFor`) from whichever member's own floor is most
   restrictive, so the bbox can never shrink far enough to violate any member's minimum size.
   Covered by `GroupResizeNeverShrinksAMemberBelowItsOwnMinimumSize`.

bUnit coverage: new `DiagramCanvasMultiSelectionMoveResizeTests.cs` covers the bounding box's
visibility/geometry, both move-trigger paths (including a zoom-scaled case), both gestures'
Board-unchanged-until-release discipline, proportional group resize (including an opposite-corner-
anchored case, mirroring ticket 31's per-instance version), and individual-handle suppression/
reappearance. `DiagramCanvasMarqueeSelectTests.cs`'s `ADragStartingWithinTheSelectionBounds...`
test (ticket 32's placeholder asserting the empty-space drag was inert) is now genuinely wrong once
this ticket gives that drag a real job - removed in favour of the equivalent, corrected assertions
in the new file.

New Playwright visual coverage (`MultiSelectionMoveResizeVisualTests.cs`): the bounding box +
handles visible after a marquee-select, a mid-group-resize state showing proportional scaling, and
the stray-click regression test above.
Regenerating baselines (via the pinned `mcr.microsoft.com/playwright/dotnet:v1.61.0-noble` Docker
image, per the README process, through `podman machine start` + `docker context use default`)
surfaced the same kind of knock-on tickets 30-32 already documented: every existing baseline's HTML
picked up the new (unconditionally-emitted, currently-unused-by-them) `.selection-bounding-box`/
`.group-resize-handle` CSS block - a pure, zero-pixel-impact addition confirmed by diffing before
promoting (zero removed lines in any of them). Two of those - `MarqueeVisualTests`'s
`BothInstancesSelected_AfterMarqueeRelease` and `MarqueeInProgress` (the marquee gesture selects
live, mid-drag, so both already have 2+ selected by the time either screenshot is taken) - had a
*real* pixel change too: their 16 individual `resize-handle` dots (8 per instance) are correctly
replaced by the one shared bounding-box overlay's 8 handles. `DragMoveVisualTests.DragInProgress`
and `ResizeVisualTests.ResizeInProgress` also failed on pixels alone despite an HTML diff proven to
be CSS-only - re-running the full suite afterward passed all 16 with no further changes, confirming
that was transient rendering-timing flake, not a regression, so their `.verified.png` files were
left untouched.
