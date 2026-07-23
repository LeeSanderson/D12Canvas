# 44 — Group/ungroup lifecycle

**What to build:** An end user selects two or more entities and invokes the group action (Ctrl+G): the selection is promoted into a persistent `Group` entity holding `MemberIds`. Afterwards, clicking any member selects the whole group. Ungroup (Ctrl+Shift+G) dissolves it back into independent entities. Both actions are single undoable history entries. A group's bounds are computed from its members on demand — never stored. Grouping a selection that already contains a group nests it. (ADR 0006, ADR 0007.)

**Blocked by:** 32 (Marquee + shift-click multi-select), 37 (History core: undo/redo move & resize)

**Status:** ready-for-agent

- [ ] The group action promotes a 2+ selection into a `Group` entity with `MemberIds`; the group becomes the selection
- [ ] Clicking any member selects the whole group
- [ ] Ungroup dissolves the group; members become independently selectable again
- [ ] Group and ungroup are each one undoable history entry (`GroupCommand`/`UngroupCommand`)
- [ ] Group bounds are computed from members, not stored
- [ ] A selection containing a group can itself be grouped (nesting)
- [ ] xUnit/bUnit coverage of the lifecycle
