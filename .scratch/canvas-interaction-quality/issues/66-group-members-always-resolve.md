# 66 — A group's members always resolve

**What to build:** A group is never left referencing deleted members (ADR 0053). When an end user deletes shapes that belong to groups, every affected group's membership is edited in the same history entry as the delete, so one Ctrl+Z restores both the shapes and the memberships. A group left with one member dissolves, with the survivor taking the group's place in any parent group; a group left with no members is removed. This holds at every nesting level. Both load paths repair a saved board the same way, and the strict load no longer throws on a dangling member id.

This is pure model, command and serializer work and can land ahead of the pointer spine. It is the foundation ticket 81 (entered group) and ticket 102 (an edit that ends empty) build their group repairs on.

**Blocked by:** None — can start immediately

**Status:** resolved

- [x] Deleting a member of a two-member group dissolves the group in the same history entry; undo restores the group, its member and the parent membership together
- [x] Deleting every member of a group removes the group in the same entry
- [x] A nested group dissolving promotes its survivor into the parent group's member list
- [x] `Board`'s plain mutators are unchanged; the repair lives in the delete command composition, not on `Board`
- [x] Strict deserialise and partial deserialise both repair a saved board with dangling members identically and strict load does not throw
- [x] A board saved under the current schema loads unchanged
- [x] `CONTEXT.md`'s `Group` term already describes this behaviour; no vocabulary change
- [x] Pure C# tests at the command and serializer seams; no rendering involved

## Comments

Two new pieces, both pure. `GroupRepair.Plan` (in `Model`) takes a set of groups and a predicate saying which instance ids still resolve, and returns a `GroupRepairPlan`: the groups that leave the board, the groups replaced by a copy with fewer members, and the reasons (missing member, emptied, dissolved with its survivor). It repeats upward until every surviving group has two or more members, and a dissolved group's survivor takes its exact index in the parent. `InstanceRemoval.Compose` (in `History`) turns a set of instance ids into the removals plus the repair, composed from the existing `UngroupCommand` and `GroupCommand` primitives (a replaced group is remove-old-then-add-new under the same id), so no new command type was needed and one `CompositeCommand` undoes everything together.

`OnDeletePressed` now calls `InstanceRemoval.Compose`. The delete repairs only the groups the removal touches (those holding a removed id, and their ancestors), so a group a host broke through `Board`'s mutators is left for the next load, as the decision says. Both serializer paths run the same plan over every group after they are added; the strict path applies it silently and the partial path emits one warning per repair (the existing missing-member warning, plus one for an emptied group and one for a dissolved group), each naming the group by the id text as written in the file. A parent settles after its children, so a chained dissolve reports the survivor that is still on the board at the end. A repaired board saves clean and reloads with no warnings.

The group's accessible label now counts members that resolve rather than `MemberIds.Count`, the one cheap reader-side tolerance the decision asked for.

Four existing serializer tests built fixtures with one-member or empty groups that the old code tolerated; they were rewritten with two-member groups so they test what they meant to. A shared `GroupRepairFixtures` file holds the one saved board that needs every kind of repair at once, and both load paths are asserted to produce the same board from it. Beyond the pure tests the checklist asks for, one bUnit test drives `OnDeletePressed` on a selected group and one asserts the accessible label, because nothing else proves the canvas is wired to the new pieces.

Not done here, by design: the `Ctrl+G` guard for "every direct member of the entered group is selected", the scope-pop on dissolve and the lock rule reading over resolving members all need features from later tickets (81 and 100). The visual suite was run because the aria-label code path changed, even though its value is unchanged for any board that passes load.
