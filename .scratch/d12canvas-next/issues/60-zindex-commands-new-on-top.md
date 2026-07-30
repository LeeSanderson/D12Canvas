# 60 — ZIndex commands + new-on-top

**What to build:** An end user reorders stacking with four commands — bring to front, send to back, bring forward, send backward — each with its keyboard shortcut from the ADR 0009 table and each an undoable gesture. Newly placed instances always appear on top by default. (ADR 0008.)

**Blocked by:** 28 (Click-to-add placement), 37 (History core: undo/redo move & resize)

**Status:** resolved

- [x] All four layering commands reorder the selection correctly, including tie handling among neighbours
- [x] Each command has its baseline keyboard shortcut and is one undoable history entry
- [x] Newly placed instances receive a `ZIndex` above all existing entities
- [x] Stacking changes render immediately and persist with the instances
- [x] Screenshot case for overlapping instances before/after a layering command
- [x] xUnit coverage of the reordering semantics

## Comments

Implemented as four new `Board` query primitives (`NextZIndex`/`PreviousZIndex` = board max/min
±1; `ZIndexAbove`/`ZIndexBelow` = the next *distinct* ZIndex value strictly above/below a given
one, skipping past any siblings tied at the same value) plus one new `ChangeZIndexCommand`
(History) mirroring `ChangeBoundsCommand`'s before/after shape. "Tie handling among neighbours"
is solved by keying `ZIndexAbove`/`ZIndexBelow` off the next *distinct rank*, not the next sorted
list entry - so bringing an instance forward past a whole cluster of siblings sharing its own
ZIndex takes one step (lands it tied with the next rank up), never one swap per sibling. Bring to
Front/Send to Back always produce a genuine change (computed over the *entire* board including the
target, so the result is always strictly above/below every existing value, even the target's own
old one); Bring Forward/Backward return `null` (a real no-op, no history entry) only when already
at the topmost/bottommost rank.

`DiagramCanvas` gained four `[JSInvokable]` handlers (`OnBringToFrontPressed`/
`OnSendToBackPressed`/`OnBringForwardPressed`/`OnSendBackwardPressed`) sharing one private
`ApplyZIndexChange(Func<ComponentInstance, int?> computeNewZIndex)` helper - reads through
`ExpandedSelection()` (ticket 44), computes every selected instance's new ZIndex against a stable
before-gesture board snapshot (so a multi-selection's members never see each other's in-progress
writes), skips any that wouldn't actually change, and commits the rest as one `CompositeCommand`
of `ChangeZIndexCommand`s - mirroring `OnDeletePressed`/`OnUngroupPressed`'s own "build a list,
wrap once" shape, including always wrapping in `CompositeCommand` even for a single change (no
special-casing, per ticket 38's precedent). A selected `Group`'s members are reordered
independently, same as any other multi-selection - the Group-specific "bulk-write, preserve
relative member order" behaviour (ADR 0008) is ticket 61's job, not this one's, since ticket 61 is
explicitly blocked by this ticket.

Keyboard shortcuts added to `DiagramCanvas.razor.js`'s existing keydown switch: `BracketRight`
(Ctrl+]/Ctrl+Shift+] = forward/front) and `BracketLeft` (Ctrl+[/Ctrl+Shift+[ = backward/back),
matching ADR 0009's table exactly and following the same `isEditableTarget`-guarded shape as the
existing `KeyZ`/`KeyG` cases.

New-on-top: `DiagramCanvas.NewCenteredInstance` (shared by `PlaceComponent`'s `HandleDrop`/
`ClickToAdd` callers, ticket 27/28) now passes `Board.NextZIndex()` as the new instance's `ZIndex`
instead of leaving it at the default `0` - the one gap the research pass confirmed pre-existed.
This also incidentally applies to a new edge label (ticket 53's `AddEdgeLabel`, which shares the
same helper) - an accepted, harmless consequence of reusing the one construction path rather than
a deliberately separate feature.

**One real bug surfaced and fixed before this ticket was considered done:**
`ComponentContainer.ShouldRender` never compared `ZIndex` against its own `_lastRenderedZIndex` -
a layering command changing only `ZIndex` (Bounds/selection/Props/custom ports all unchanged)
would silently fail to reach the DOM until some unrelated parameter also happened to change,
violating this ticket's own "stacking changes render immediately" requirement. Fixed by adding a
`_lastRenderedZIndex` field alongside the existing tracked fields, following ticket 33's own
`IsMultiSelected`-addition precedent to `ShouldRender`.

xUnit coverage: `ChangeZIndexCommandTests.cs` (apply/undo, mirroring `ChangeBoundsCommandTests`);
`BoardTests.cs` gained cases for all four query primitives including two explicit tie-handling
cases (`ZIndexAbove`/`ZIndexBelow` skip past a sibling tied at the same value) and the
already-at-the-extreme null cases; `DiagramCanvasZIndexCommandsTests.cs` (bUnit) covers all four
commands end-to-end (single-select, tie handling, no-op at the extreme, no selection, no board,
multi-select applying to every member, undo/redo including a multi-selection's atomic undo, and
the rendered `z-index` style updating immediately); `ComponentContainerTests.cs` gained a direct
regression test for the `ShouldRender` fix; `DiagramCanvasClickToAddTests.cs`/
`DiagramCanvasPlacementTests.cs` each gained a case proving a newly-placed instance's `ZIndex`
lands above an existing instance's.

New Playwright visual coverage (`ZIndexLayeringVisualTests.cs`, reusing `/placement-demo`): two
overlapping instances of different types (so the stacking order is visually unambiguous) before
any layering command, and after a real `Control+Shift+]` keypress brings the originally-underneath
one to the front - both driven through a real headless Chromium session (confirmed passing
locally, screenshots visually inspected before promoting to baselines). Also ran the full
Playwright suite once as a regression check for the `ComponentContainer.ShouldRender` change: 32
pre-existing baselines failed, but this reproduces ticket 81's already-documented, unrelated
environmental drift exactly - even `PaletteVisualTests` (a control page that never renders
`DiagramCanvas`) failed identically, confirming it predates and is unrelated to this ticket's
changes. Not fixed here, per ticket 52/81's own precedent for this out-of-scope issue.

**One real gap surfaced by `/code-review`'s Standards pass, fixed before this ticket was
considered done:** Bring to Front/Send to Back's original multi-select handling called
`Board.NextZIndex()`/`PreviousZIndex()` independently per selected instance against an unchanged
snapshot, so every member of a multi-selection landed on the exact same tied value - silently
erasing their own relative stacking order relative to each other (though still correctly placing
the whole set above/below everything else). Replaced with a dedicated `RestackSelection(bool
toFront)` that sorts the selection by its current `ZIndex` once and assigns consecutive values
starting from the board's current extreme, so the member that was already on top of the other
stays on top after the move. `OnBringForwardPressed`/`OnSendBackwardPressed` were unaffected (each
selected instance's own `ZIndexAbove`/`ZIndexBelow` already differs per starting position) and
keep using the original per-instance `ApplyZIndexChange`. `DiagramCanvasZIndexCommandsTests.cs`'s
multi-select coverage was tightened to assert exact, distinct resulting values (not just "above
the untouched instance") for both Bring to Front and the newly-added Send to Back case, proving
the fix.
