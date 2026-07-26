# 38 — Undo/redo placement & delete

**What to build:** Creating and deleting become undoable: undoing a placement removes the instance, undoing a delete restores everything deleted. Deleting a multi-selection is one atomic history entry — undo brings the whole set back at once. Built on `AddEntity`/`RemoveEntity` with `CompositeCommand` wrapping multi-entity gestures. (ADR 0007.)

**Blocked by:** 28 (Click-to-add placement), 34 (Delete selection), 37 (History core: undo/redo move & resize)

**Status:** resolved

- [x] Undoing a placement (either placement path) removes the instance; redo restores it with the same ID
- [x] Undoing a delete restores all deleted entities with identity, bounds, and props intact
- [x] A multi-selection delete is a single atomic history entry
- [x] xUnit coverage of add/remove/composite command semantics

## Comments

New `D12Canvas/History/AddEntityCommand.cs` / `RemoveEntityCommand.cs` (ADR 0007's `AddEntity`/
`RemoveEntity` primitives, named with the `*Command` suffix to match `ChangeBoundsCommand`/
`CompositeCommand`'s existing convention). Both wrap a `Board` reference plus the `ComponentInstance`
being added/removed; since `ComponentInstance` is a mutable class held by reference (ADR 0003), no
snapshot is needed to reconstruct it on undo - re-adding the same instance reference restores its
identity (`Id`), `Bounds`, and `Props` exactly as they were, for free. The two commands are exact
mirrors of each other (`Apply`/`Undo` swapped).

`DiagramCanvas.PlaceComponent` (shared by `HandleDrop` and `ClickToAdd`, ticket 27/28) now routes
the newly-constructed `ComponentInstance` through `_history.Do(new AddEntityCommand(Board!, instance))`
instead of a direct `Board.AddComponent` call - undo removes the placed instance, redo restores it
under the same `Id`.

`DiagramCanvas.OnDeletePressed` (ticket 34) now builds one `RemoveEntityCommand` per currently
selected instance (skipping any id that no longer resolves, same defensive check the original loop
implicitly had via `Board?.RemoveComponent`) and, if any were found, commits them as a single
`_history.Do(new CompositeCommand(commands))` - mirroring `CommitGroupMove`/`CommitGroupResize`'s
existing "build a list, wrap once" shape from ticket 33/37. This holds for both single and multi
selection: a lone selected instance still produces a one-child `CompositeCommand`, which is just as
atomic as a bespoke non-composite path would be, so no special-casing was needed. Selection is
cleared afterwards exactly as before - undo/redo never touches selection (ADR 0006).

No new Playwright visual test, matching tickets 34's and 37's precedent - undo/redo of placement
and delete only restores/reapplies already-rendered states, it doesn't introduce a new visual state
to capture.

xUnit coverage: `AddEntityCommandTests.cs` / `RemoveEntityCommandTests.cs` (plain xUnit, mirroring
`ChangeBoundsCommandTests.cs`) cover apply/undo/redo semantics directly against a `Board`, including
that undo/redo preserves the same instance reference (identity, bounds, and props). `DiagramCanvasUndoRedoTests.cs`
gained bUnit end-to-end coverage: undo/redo of a click-to-add placement and a drag-drop placement
(both asserting the restored instance keeps the same `Id`), and undo/redo of a single-selection and
a multi-selection delete (the multi-selection case asserting both instances come back in one undo,
with their original bounds intact).
