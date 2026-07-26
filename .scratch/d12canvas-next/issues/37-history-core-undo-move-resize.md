# 37 — History core: undo/redo move & resize

**What to build:** An end user presses Ctrl+Z after moving or resizing and the instance returns exactly where it was; Ctrl+Y (redo) reapplies it. Each completed gesture records one `ChangeBoundsCommand` in the `History` — a session-scoped, in-memory circular buffer capped at 1000 entries. One gesture is one entry, never one per intermediate frame. (ADR 0007.)

**Blocked by:** 30 (Drag-move an instance), 31 (Resize via handles)

**Status:** resolved

- [x] One history entry per completed move or resize gesture — never per frame
- [x] Undo restores the prior bounds; redo reapplies the change
- [x] Ctrl+Z / Ctrl+Shift+Z shortcuts work on the canvas
- [x] The buffer caps at 1000 entries, dropping oldest first
- [x] History is in-memory and session-scoped — never serialized, never survives a reload
- [x] A new gesture after undo clears the redo stack
- [x] xUnit coverage at the command/history seam

## Comments

New `D12Canvas/History/` folder (namespace `D12Canvas.History`, matching the `Registration`/
`Persistence` sibling-folder convention): `ICommand` (`Apply`/`Undo`), `ChangeBoundsCommand`
(covers move and resize as one command, per ADR 0007), `CompositeCommand` (ordered apply,
reverse-order undo — needed immediately by the pre-existing multi-select move/resize write sites
from ticket 33, not deferred to ticket 38), and `CommandHistory` (`Do`/`Undo`/`Redo`, a
`LinkedList`-backed drop-oldest buffer capped at `DefaultCapacity = 1000`, `Stack`-backed redo
cleared on every new `Do`). Named `CommandHistory` rather than the doc's bare `History` — a class
literally named `History` living in a namespace `D12Canvas.History`, consumed from `D12Canvas`
(the enclosing root namespace `DiagramCanvas.razor.cs` lives in), triggers `CS0118 'History' is a
namespace but is used like a type` at every call site; confirmed by hitting the error and
renaming rather than fighting it with per-file aliases.

`DiagramCanvas` gained a private `CommandHistory _history` field (session/component-scoped, same
reasoning as `_selectedInstanceIds` — lives here, not on `Board`, ADR 0007). All four existing
gesture-commit write sites (ticket 30's `MoveComponent`, ticket 31's `ResizeComponent`, ticket
33's `CommitGroupMove`/`CommitGroupResize`) now route their `instance.Bounds = ...` write through
`_history.Do(new ChangeBoundsCommand(...))` instead of a raw assignment; the two group sites build
one `ChangeBoundsCommand` per member and wrap them in a single `CompositeCommand` so one undo
reverts every member together, matching ADR 0007's own multi-select example verbatim.

New `[JSInvokable] OnUndoPressed()` / `OnRedoPressed()` on `DiagramCanvas`, calling
`_history.Undo()`/`Redo()` then `StateHasChanged()`. `DiagramCanvas.razor.js`'s keydown switch
gained a `"KeyZ"` case guarded by `ctrlKey || metaKey` (undo) plus `shiftKey` (redo) — ADR 0009's
locked shortcut table specifies **Ctrl+Z / Ctrl+Shift+Z**, not the ticket text's "Ctrl+Y", so the
ADR wins. Also guarded by the existing `isEditableTarget` check (same as `Delete`/`Backspace`,
ticket 34): Ctrl+Z is the OS/browser's own text-editing undo, so without the guard, pressing it
while focus is on an unrelated `<input>`/`<textarea>` elsewhere on a host page would hijack that
undo into reverting canvas history instead.

No new Playwright visual test, matching ticket 34's precedent — undo/redo only restores/reapplies
already-rendered states (a plain move/resize), it doesn't introduce a new visual state to capture.

xUnit coverage: `ChangeBoundsCommandTests.cs`/`CompositeCommandTests.cs`/`HistoryTests.cs` (plain
xUnit, no bUnit — the command/history seam is pure logic) cover apply/undo semantics, composite
ordering, do/undo/redo, cap-and-drop-oldest, redo-cleared-by-a-new-gesture, and
`CanUndo`/`CanRedo` state. `DiagramCanvasUndoRedoTests.cs` (bUnit) covers undo/redo through
`DiagramCanvas` end-to-end for single move, single resize, group move, and group resize (one undo
reverting every member), a new gesture clearing redo, and both stacks' no-op-when-empty behaviour
— invoking `OnUndoPressed`/`OnRedoPressed` directly via `canvas.InvokeAsync`, matching
`DiagramCanvasDeleteSelectionTests`'s existing convention for keyboard-triggered behaviour rather
than driving a real keydown event (bUnit's stubbed JS module can't simulate one).

Deletion (ticket 34) and placement (part of ticket 38's scope) are still not undo-wrapped —
`AddEntity`/`RemoveEntity` don't exist yet; only `ChangeBoundsCommand`/`CompositeCommand` were
needed for this ticket's move/resize scope.
