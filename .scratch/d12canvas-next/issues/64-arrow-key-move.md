# 64 — Arrow-key move

**What to build:** A keyboard user nudges the focused/selected content with the arrow keys — a zoom-relative step, so movement stays proportionate at any magnification. Nudges are undoable gestures. Works for single instances, multi-selections, and groups. (ADR 0010.)

**Blocked by:** 37 (History core: undo/redo move & resize), 63 (Tab stops + focus-follows-selection)

**Status:** resolved

- [x] Arrow keys move the selection by a zoom-relative step in the pressed direction
- [x] Nudges record undoable history entries (a rapid burst doesn't flood history per ADR 0010's gesture intent)
- [x] Works uniformly for a single instance, a multi-selection, and a group
- [x] bUnit coverage of step size at different zoom levels and undo behaviour

## Comments

Arrow keys already had a pre-existing meaning here - `OnPanLeft`/`OnPanRight`/`OnPanUp`/`OnPanDown`
panned the canvas 50px per press, wired unconditionally in `DiagramCanvas.razor.js`'s keydown
listener. Since nothing in ADR 0010 (or this ticket) says that behaviour goes away, and a keyboard
user with no selection has nothing to nudge anyway, the four pan handlers were folded into one
`OnArrowKeyPressed(code, shiftKey)`: nudges `ExpandedSelection` when `_selectedInstanceIds` is
non-empty (a single instance, an ad-hoc multi-selection, or a persisted `Group`'s members - the
same flattening `CommitGroupMove`/`RestackSelection`/`ApplyZIndexChange` already rely on), else
falls back to the original pan. An edge-only selection (`_selectedEdgeId`, never mixed into
`_selectedInstanceIds`) has no `Bounds` to nudge either, so it takes the same pan fallback.

Fixed a real regression risk while wiring this up: the JS keydown listener called arrow-key
handling unconditionally, with no `isEditableTarget` guard (unlike Delete/Ctrl+Z/Ctrl+G, which
already had one). Ticket 43's inline WYSIWYG text editing means an arrow key can legitimately be
moving a text cursor inside a `contenteditable` host element - without the guard, nudging would
have hijacked that keystroke and moved the shape instead of the cursor. The guard was added
alongside the new handler, not deferred. It sits ahead of the nudge/pan branch, so it also now
suppresses the pan fallback while focus is on any editable host-page element, not only during
WYSIWYG editing - a small, deliberate widening of the existing guard's reach for consistency,
since a stray arrow-key pan while typing elsewhere on the host page was never a feature worth
preserving.

"A rapid burst doesn't flood history" is resolved via a held key's own press/release boundary
rather than a time-based heuristic: `NudgeCommand` (new, `D12Canvas/History/`) accumulates a
running delta and exposes `Extend`, so repeat keydown events for a still-held key mutate the same
history entry in place instead of pushing a new one each time. The matching keyup
(`OnArrowKeyReleased`, requiring a new JS keyup listener alongside the existing keydown one) clears
the reference so the next press starts a fresh undoable gesture - mirroring a mouse drag's single
commit-on-release (ADR 0007) but bounded by key-up instead of pointer-up. Extending only happens
when `CommandHistory.PeekUndo` (new) still resolves back to the exact same command instance and its
`Matches` still resolves to the exact same target set - either check failing (an intervening
undo/redo, or the selection changing mid-hold) starts a new entry rather than silently reusing a
stale one.

New coverage: `NudgeCommandTests.cs` (plain unit tests - apply/undo/extend/matches) and
`DiagramCanvasArrowKeyMoveTests.cs` (bUnit - per-direction step at default zoom, Shift's coarser
step, step size at two different zoom levels, undo/redo, burst coalescing, keyup ending a burst,
ad-hoc multi-selection, a persisted `Group`, and both no-selection and edge-only-selection falling
back to panning).
