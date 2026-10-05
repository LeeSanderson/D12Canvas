# 66 — A group's members always resolve

**What to build:** A group is never left referencing deleted members (ADR 0053). When an end user deletes shapes that belong to groups, every affected group's membership is edited in the same history entry as the delete, so one Ctrl+Z restores both the shapes and the memberships. A group left with one member dissolves, with the survivor taking the group's place in any parent group; a group left with no members is removed. This holds at every nesting level. Both load paths repair a saved board the same way, and the strict load no longer throws on a dangling member id.

This is pure model, command and serializer work and can land ahead of the pointer spine. It is the foundation ticket 81 (entered group) and ticket 102 (an edit that ends empty) build their group repairs on.

**Blocked by:** None — can start immediately

**Status:** ready-for-agent

- [ ] Deleting a member of a two-member group dissolves the group in the same history entry; undo restores the group, its member and the parent membership together
- [ ] Deleting every member of a group removes the group in the same entry
- [ ] A nested group dissolving promotes its survivor into the parent group's member list
- [ ] `Board`'s plain mutators are unchanged; the repair lives in the delete command composition, not on `Board`
- [ ] Strict deserialise and partial deserialise both repair a saved board with dangling members identically and strict load does not throw
- [ ] A board saved under the current schema loads unchanged
- [ ] `CONTEXT.md`'s `Group` term already describes this behaviour; no vocabulary change
- [ ] Pure C# tests at the command and serializer seams; no rendering involved
