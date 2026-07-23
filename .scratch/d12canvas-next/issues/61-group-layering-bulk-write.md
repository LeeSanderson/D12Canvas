# 61 — Group layering bulk-write

**What to build:** An end user applies a layering command to a selected `Group` and the whole group moves through the stack as one: the command bulk-writes every member's `ZIndex`, preserving the members' relative order. A group has no z-position field of its own. One undoable history entry. (ADR 0008.)

**Blocked by:** 44 (Group/ungroup lifecycle), 60 (ZIndex commands + new-on-top)

**Status:** ready-for-agent

- [ ] Layering a group rewrites all member `ZIndex` values in one operation
- [ ] Members' relative order within the group is preserved
- [ ] The `Group` entity itself carries no z-position field
- [ ] The bulk-write is one undoable history entry
- [ ] xUnit coverage including nested-group members
