# 61 — Group layering bulk-write

**What to build:** An end user applies a layering command to a selected `Group` and the whole group moves through the stack as one: the command bulk-writes every member's `ZIndex`, preserving the members' relative order. A group has no z-position field of its own. One undoable history entry. (ADR 0008.)

**Blocked by:** 44 (Group/ungroup lifecycle), 60 (ZIndex commands + new-on-top)

**Status:** resolved

- [x] Layering a group rewrites all member `ZIndex` values in one operation
- [x] Members' relative order within the group is preserved
- [x] The `Group` entity itself carries no z-position field
- [x] The bulk-write is one undoable history entry
- [x] xUnit coverage including nested-group members

## Comments

No production code changed. Ticket 44's `DiagramCanvas.ExpandedSelection()` already recursively
flattens a selected `Group` (nested or not) down to its raw leaf `ComponentInstance` ids before
ticket 60's `RestackSelection`/`ApplyZIndexChange` ever see the selection - so a persisted Group's
layering command already bulk-writes every member's `ZIndex` in one `CompositeCommand`, same as any
other multi-selection, with no separate group-layering code path needed. `Group` (`Model/Group.cs`)
already carries no z-position field of its own - it never did, by ADR 0008's own design (computed
bounds only, same as its computed-bounds precedent for move/resize).

Relative order preservation holds by construction, not by any group-specific logic: for Bring to
Front/Send to Back, `RestackSelection` sorts the *entire* selection by current `ZIndex` once and
assigns consecutive values from the board's extreme (ticket 60's own fix for this exact
multi-selection concern). For Bring Forward/Send Backward, `ApplyZIndexChange` resolves each
selected instance's next distinct rank independently against one shared pre-gesture snapshot; since
`ZIndexAbove`/`ZIndexBelow` are monotonic in their input, two members starting in one relative order
can never end in the opposite order after the same rule is applied to both - only a tie (never an
inversion) is possible when they land on the same next-door value.

This ticket's remaining work was pinning that behaviour with dedicated coverage ticket 60's own
tests (ad-hoc shift-click multi-selection only) never exercised together with a *persisted* `Group`:

- `D12Canvas.Tests/DiagramCanvasGroupZIndexTests.cs` (new) - all four layering commands applied to a
  persisted two-member `Group`, each asserting the bulk write lands correctly and relative order
  survives the move, plus a nested-group case (`[A, B]` grouped into an inner `Group`, then
  `[inner, C]` grouped into an outer one) proving Bring to Front rewrites every leaf member's
  `ZIndex`, not just the outer group's immediate members.
- `D12Canvas.Tests/DiagramCanvasUndoRedoTests.cs` gains
  `UndoAfterAPersistedGroupLayeringCommandRevertsEveryMemberInOneStep` (Bring to Front, exercising
  the `RestackSelection` code path) and
  `UndoAfterAPersistedGroupBringForwardLayeringCommandRevertsEveryMemberInOneStep` (Bring Forward,
  exercising the separate `ApplyZIndexChange` code path) - alongside ticket 45's own persisted-group
  move/resize undo tests - undo/history coverage lives there by established convention, not in the
  feature-specific test file above.

All 7 new/changed test cases pass unmodified against existing production code (527 total, 1
pre-existing unrelated skip). Full solution build is clean (0 warnings, 0 errors).
