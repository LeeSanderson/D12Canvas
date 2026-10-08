# 102 — An edit that ends empty

**What to build:** A text or edge label left empty disappears, an abandoned new one leaves no undo entry at all, and an empty sticky note stays (ADR 0062). A component author registers an `IsEmpty` predicate to say whether an empty instance of their type should disappear; `Text` registers one, so every edge label does; `StickyNote` does not. An edit that ends empty (whitespace counts) removes the instance. If the creation is still the top history entry it is retracted through a new `CommandHistory.Retract`, which removes the top entry without leaving it on redo; otherwise the removal is one composite with the group repairs from ticket 66. Focus then goes to the quick-create source, the edge's stop for a label, or the canvas.

**Blocked by:** 66 (A group's members always resolve), 101 (The canvas starts every inline edit)

**Status:** resolved

- [x] Placing a text and pressing Escape without typing leaves the board and the history exactly as before placement
- [x] Clearing an existing text and committing removes it in one undoable entry; Ctrl+Z brings it back with its text
- [x] Clearing a sticky note leaves an empty sticky note
- [x] An edge label cleared to whitespace is removed and focus lands on the edge's stop
- [x] Removing a grouped text that leaves its group with one member dissolves the group in the same entry
- [x] `Retract` has history tests; the registration option is validated and documented in the registration surface
- [x] `CONTEXT.md`'s `History` and `Inline edit` terms describe what shipped

Shipped with three choices the ADR did not spell out. Registration refuses an `IsEmpty` predicate on a component type that does not implement `IInlineEditable`, since no edit ever ends on it. The canvas holds the creation it may retract only until history next changes, not only while it is on top by reference: placing a text, typing, committing and undoing put the creation back on top with the text change on redo, and retracting it there would have left that redo entry pointing at an instance no longer on the board. An instance already empty when its edit opens, a loaded empty `Text` opened with `F2` and closed unchanged, is removed too, as the ADR says.

Still open: focus after an abandoned `Quick create` going to its source waits for ticket 103. A blur commit that arrives after a press has locked history is dropped, as a non-empty commit already was; the real-browser probe for a click on empty canvas shows the blur arriving first.

`Quick create` shipped with ticket 103: an abandoned one is retracted with its edge, and after `Escape` focus and selection return to its source.
