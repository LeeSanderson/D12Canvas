# 38 — Undo/redo placement & delete

**What to build:** Creating and deleting become undoable: undoing a placement removes the instance, undoing a delete restores everything deleted. Deleting a multi-selection is one atomic history entry — undo brings the whole set back at once. Built on `AddEntity`/`RemoveEntity` with `CompositeCommand` wrapping multi-entity gestures. (ADR 0007.)

**Blocked by:** 28 (Click-to-add placement), 34 (Delete selection), 37 (History core: undo/redo move & resize)

**Status:** ready-for-agent

- [ ] Undoing a placement (either placement path) removes the instance; redo restores it with the same ID
- [ ] Undoing a delete restores all deleted entities with identity, bounds, and props intact
- [ ] A multi-selection delete is a single atomic history entry
- [ ] xUnit coverage of add/remove/composite command semantics
