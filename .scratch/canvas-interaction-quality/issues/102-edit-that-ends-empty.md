# 102 — An edit that ends empty

**What to build:** A text or edge label left empty disappears, an abandoned new one leaves no undo entry at all, and an empty sticky note stays (ADR 0062). A component author registers an `IsEmpty` predicate to say whether an empty instance of their type should disappear; `Text` registers one, so every edge label does; `StickyNote` does not. An edit that ends empty (whitespace counts) removes the instance. If the creation is still the top history entry it is retracted through a new `CommandHistory.Retract`, which removes the top entry without leaving it on redo; otherwise the removal is one composite with the group repairs from ticket 66. Focus then goes to the quick-create source, the edge's stop for a label, or the canvas.

**Blocked by:** 66 (A group's members always resolve), 101 (The canvas starts every inline edit)

**Status:** ready-for-agent

- [ ] Placing a text and pressing Escape without typing leaves the board and the history exactly as before placement
- [ ] Clearing an existing text and committing removes it in one undoable entry; Ctrl+Z brings it back with its text
- [ ] Clearing a sticky note leaves an empty sticky note
- [ ] An edge label cleared to whitespace is removed and focus lands on the edge's stop
- [ ] Removing a grouped text that leaves its group with one member dissolves the group in the same entry
- [ ] `Retract` has history tests; the registration option is validated and documented in the registration surface
- [ ] `CONTEXT.md`'s `History` and `Inline edit` terms describe what shipped
