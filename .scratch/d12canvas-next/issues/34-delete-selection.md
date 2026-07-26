# 34 — Delete selection

**What to build:** An end user presses Delete and every selected entity is removed from the `Board`. Works for a single instance or a whole multi-selection; the selection clears afterwards. (Baseline shortcut from ADR 0009.)

**Blocked by:** 29 (Click select, escape, and `aria-selected`)

**Status:** resolved

- [x] Delete removes all currently selected instances from the `Board`
- [x] Works for single and multi selections
- [x] The selection is empty after deletion
- [x] bUnit coverage of the delete behaviour

## Comments

New `[JSInvokable] OnDeletePressed()` on `DiagramCanvas`, following `OnEscapePressed`'s exact
shape: iterates `_selectedInstanceIds`, calls `Board.RemoveComponent(id)` for each, then clears
`_selectedInstanceIds` and re-renders. Single vs. multi selection needs no separate branch here -
unlike move/resize (tickets 30/31/33), deletion has no "move/scale as one unit" delta to compute,
just N independent removals, so the same loop handles both.

`DiagramCanvas.razor.js`'s existing keydown switch (ticket 29's `"Escape"` case) gained `"Delete"`
and `"Backspace"`, both invoking `OnDeletePressed` - matching ADR 0009's baseline shortcut table
("Delete / Backspace | Delete selected instance(s)/group"), even though the ticket text only
named Delete.

No history/command wiring: ticket 37 (undo/redo core) and ticket 38 (undo for placement/delete
specifically) aren't implemented yet, and ticket 34 is blocked only by 29, not 37 - per the
undo/redo design doc's own sequencing, `Board.RemoveComponent` is called directly today, with
undo-wrapping as a future `CompositeCommand` of N `RemoveEntity`s deferred entirely to ticket 38.

No persisted `Group` entity exists yet either (ticket 44 not implemented) - today's "selection"
is only the ad-hoc, transient multi-select `HashSet<Guid>`, so there's no cascade-to-members
semantics to design; deleting a multi-selection is just deleting each of its member instances.

No new visual/screenshot test: unlike ticket 29 (which introduced a new rendered selection
affordance) or ticket 33 (a new bounding-box overlay), deletion doesn't add a new visual state to
capture - it only removes existing elements, already covered by every prior placement/selection
baseline.

**One real risk surfaced by `/code-review`'s Spec pass, fixed before this ticket was considered
done:** the keydown listener is global (`window`, not scoped to canvas focus - a pre-existing gap
predating this ticket, shared by every other key it handles). Backspace is uniquely dangerous to
add to that listener unguarded, since it's the browser's own text-deletion key - without a check,
pressing Backspace while editing an unrelated `<input>`/`<textarea>` anywhere else on a host page
embedding `DiagramCanvas` would silently wipe the current selection instead of deleting a
character. Fixed with a narrow `isEditableTarget` guard on the `Delete`/`Backspace` case only
(`DiagramCanvas.razor.js`) - not a fix of the broader unscoped-listener/unconditional-
`preventDefault` design, which predates this ticket and belongs to ADR 0010's full keyboard-
accessible interaction model (ticket 16), not this one.

bUnit coverage: new `DiagramCanvasDeleteSelectionTests.cs` - single-selection delete (removed from
`Board`, no `.component-container` left), multi-selection delete (only the selected members
removed, an untouched third instance survives, selection cleared), and delete-with-nothing-
selected as a no-op.
