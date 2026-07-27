# 44 — Group/ungroup lifecycle

**What to build:** An end user selects two or more entities and invokes the group action (Ctrl+G): the selection is promoted into a persistent `Group` entity holding `MemberIds`. Afterwards, clicking any member selects the whole group. Ungroup (Ctrl+Shift+G) dissolves it back into independent entities. Both actions are single undoable history entries. A group's bounds are computed from its members on demand — never stored. Grouping a selection that already contains a group nests it. (ADR 0006, ADR 0007.)

**Blocked by:** 32 (Marquee + shift-click multi-select), 37 (History core: undo/redo move & resize)

**Status:** resolved

- [x] The group action promotes a 2+ selection into a `Group` entity with `MemberIds`; the group becomes the selection
- [x] Clicking any member selects the whole group
- [x] Ungroup dissolves the group; members become independently selectable again
- [x] Group and ungroup are each one undoable history entry (`GroupCommand`/`UngroupCommand`)
- [x] Group bounds are computed from members, not stored
- [x] A selection containing a group can itself be grouped (nesting)
- [x] xUnit/bUnit coverage of the lifecycle

## Comments

`Model/Group.cs` adds the entity (`Id` + `MemberIds`, no stored `Bounds`). `Board` gains
`AddGroup`/`RemoveGroup`/`GetGroup`, `GetBounds(Group)` (recursively resolves member/nested-group
bounds via a shared `Bounds.Union` helper, skipping any member id that no longer resolves), and
`FindContainingGroup(id)` (walks up through nesting to the outermost group containing an entity).
`History/GroupCommand.cs`/`UngroupCommand.cs` are thin `AddGroup`/`RemoveGroup` wrappers per ADR
0007, each a single `_history.Do(...)` call - one undo entry apiece (ungrouping multiple selected
groups at once still wraps in one `CompositeCommand`, matching `OnDeletePressed`'s style).

`DiagramCanvas` keeps `_selectedInstanceIds` as a set of *top-level* entity ids - a component
instance id or a Group id. A new `ExpandedSelection()` recursively flattens that down to raw
component ids, so the existing ad-hoc multi-select machinery (bounds/move/resize/delete) treats a
selected Group exactly like any other 2+ multi-selection with no separate code path. `SelectComponent`
and `UpdateMarqueeSelection` both resolve a clicked/marquee-intersected instance to its outermost
containing group id (`EffectiveSelectionId`) before adding it to the selection, so selection always
converges onto a Group once one exists (marquee included, to avoid a marquee ever re-grouping
already-grouped members into a second, overlapping `Group`). Ctrl+G/Ctrl+Shift+G wired via
`DiagramCanvas.razor.js`'s existing keyboard listener, guarded against firing while focus is on an
editable target the same way Ctrl+Z already is.

**Left open, by design:** there's no way to select an individual member out of an existing group
short of ungrouping first (shift-clicking a grouped member toggles the *whole* group, per ADR
0006's "left open... to be decided at implementation time" on group-entry interaction). Deleting a
selected group's members leaves the (now member-less) `Group` entity behind on `Board.Groups` -
cascading that cleanup, and group move/resize polish, are ticket 45/46's job, not this one's.
