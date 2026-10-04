# A group's members always resolve: a write that would break that repairs it in the same history entry, and load repairs what it is given

`OnDeletePressed` reads through `ExpandedSelection()`, which flattens a selected group to its member ids, so a delete removes the members and never touches the `Group`. A partly deleted group shrinks to its survivors. A fully emptied one stays on the board with null bounds: `GetVisibleGroups` drops it, so it has no tab stop, nothing can select it, and nothing can remove it. It is serialized into every save from then on, and the partial load path warns `References missing member` on every load after that. ADR 0044 made this an ordinary action, since inside an `Entered group` the selection holds direct members and Delete removes them from under their group. This decides whether a group may reference ids that no longer resolve, and what repairs it when it would.

## The invariant

**Every id in a live group's `MemberIds` resolves to a component instance or a group on the same `Board`.**

The writes that could break it repair it inside their own history entry. `OnDeletePressed` builds one `CompositeCommand` holding the removals and every group membership edit they make necessary, so undo restores exactly what was there (ADR 0007). This is the shape ADR 0044 already gave `GroupCommand` and `UngroupCommand`: the canvas computes every write a gesture needs, and `Board` cascades nothing. `RemoveEntityCommand` stays a plain removal and learns nothing about groups, so ADR 0003's flat model with no ownership tree holds.

ADR 0006's addendum drops a selection id that no longer resolves when the selection is read. That rule works for the selection because the selection is transient view state. A group is board content that round-trips through ADR 0004, so pruning on read would leave the dead ids in the file and the load warnings firing. Pruning at serialize time leaves the in-memory board wrong for everything that reads it before a save.

## Zero members and one member

- **A group left with no members is removed**, and its id leaves its parent's `MemberIds`.
- **A group left with one member is dissolved.** The survivor takes the group's place in the parent's `MemberIds`, or becomes top-level if there is no parent. This is ADR 0044's ungroup splice.

A group of one does nothing a user can see. A press on the member selects the group, the group's outline is the member's outline, and the user has to enter it to edit the one shape inside. Nothing grows a group except ungrouping and regrouping, so keeping it buys nothing. tldraw does the same, removing a group at zero children and reparenting its one child at one; it is a design reference here, not a source.

**The rule applies at every level.** Removing an emptied inner group takes one member from its parent, which may leave the parent with zero or one, so the check repeats upward. Dissolving a one-member inner group puts its survivor where the group was, which leaves the parent's count unchanged. The canvas works out the board's final shape after every removal, then writes the commands that produce it, all in the one `CompositeCommand`.

`ExpandedSelection()` flattening, the selection clearing after a delete, and Cut inheriting all of this through its delete (ADR 0013) are unchanged.

## `Ctrl+G` that would only rename a group

ADR 0044 has `Ctrl+G` inside an `Entered group` G take the selected ids out of G's `MemberIds` and put the new group N in their place. With every direct member of G selected, that leaves G with one member, N. The rule above then dissolves G, N takes G's place, and the scope pops because G is gone. The user has the same shapes grouped the same way, under a new id, outside the scope they were working in.

**`Ctrl+G` is unavailable when the selection is every direct member of the `Entered group`.** `CanGroupSelection` gains this as a second condition beside "two or more", so the menu row is hidden and the chord does nothing. At the top level the case cannot arise, because selecting everything there and grouping it is meaningful.

## Load

**Both load paths enforce the invariant with the same rules.**

- The **partial** path drops each dead id from `MemberIds` and keeps the existing `References missing member` warning. A group left with zero members is dropped and a group left with one is dissolved, each with its own warning. A one-member group written that way in the file, with no dead ids involved, is dissolved and warned about too: neither the editor nor host code going through the canvas can produce one, so it was built by hand.
- The **strict** path repairs the same way and does not throw. ADR 0004's strict form exists for a host that will not accept a board loading with content missing. This repair removes nothing a user can see: a reference to an entity already gone, a group with nothing in it, and a group of one that looks the same either way. Throwing would also refuse every board saved before this decision that had a group member deleted, which is damage the library caused.

A board saved after a repaired load is clean, so a user sees the partial path's warnings once.

## Who guarantees it

**The canvas and load guarantee the invariant. `Board` does not.** Its public mutators stay plain, and a host calling `RemoveComponent` on a grouped instance creates a dead id that nothing intercepts. Hosts build boards this way today. Making `Board` repair membership itself, or hiding its mutators behind a host-facing builder, would give `Board` the ownership relationship ADR 0003 keeps out, and would change group membership under every fixture built through it.

So the readers keep the tolerance they already have and add only what is cheap:

- `GetBounds` already skips a member that does not resolve, and `GetVisibleGroups` already drops a group with no bounds. That is why `OrderedTabStops`' `Board.GetBounds(group)!.Value` never meets an emptied group, and why there is no phantom tab stop.
- The group's accessible label counts members that resolve, not `MemberIds.Count`.
- ADR 0044's lock rule, "locked when every member is", reads over members that resolve, and a group with none resolving is not locked.

A host that breaks the invariant through `Board` gets a group that renders slightly wrong, never a crash, and the next save and load repairs it.

## Edges stay the other way

A component deleted from under an attached edge leaves the endpoint resolving to nothing, and ADR 0032 named that the dangling-endpoint precedent. That stays. An edge endpoint is a reference ADR 0005 already allows to resolve to nothing. **Corrected by ADR 0061:** this said "the edge still draws from its other end", which it does not; an edge with an unresolved end has no line, label or stop (ADR 0054). A group is a membership list whose only purpose is its members, so a dead entry in it has no meaning to keep.

## How this is verified

Per ADR 0025:

- Deleting part of a group leaves its `MemberIds` holding only live ids; undo restores the original list, and the deleted members, in one step.
- Deleting all but one member dissolves the group and places the survivor in the parent; deleting every member removes the group. Both undo in one step.
- In a nested case, emptying an inner group that is one of two members of its parent dissolves the parent too.
- Deleting members inside an `Entered group` until it dissolves pops the scope (ADR 0044 rule 4).
- `Ctrl+G` with every direct member of the `Entered group` selected leaves `Board` and `History` unchanged, and the menu has no Group row.
- Partial load of a file with a dead id, an emptied group, and a hand-written one-member group returns a repaired board and one warning per repair. Strict load of the same file returns the same board and does not throw.
- A group whose members were removed through `Board` directly renders without throwing, and its accessible label counts only live members.

## Amends, confirms

- **Amends ADR 0044** in two places. `Ctrl+G` inside an `Entered group` is unavailable when every direct member is selected. Its sentence "Whether a group tolerates dead member ids in general is not decided here" is decided here, and its lock rule reads over resolving members.
- **Amends ADR 0004** in one place: both deserialize paths repair group membership on load, and the strict path does not throw for it.
- **Confirms ADR 0003.** `Board` gains no ownership relationship and its mutators do not cascade.
- **Confirms ADR 0007.** One gesture is still one history entry, and its undo restores what was there.
- **Confirms ADR 0032's dangling-endpoint precedent** for edges.

## Considered and rejected

- **Tolerating dead ids.** Leaves an entity the user cannot see, select or remove, and a warning on every load.
- **Pruning on read**, the selection's rule. The file keeps the dead ids and the warnings keep firing.
- **Pruning at serialize time.** The in-memory board stays wrong for the label, the lock rule and the `Entered group` until a save.
- **A cascade inside `RemoveEntityCommand` or `Board.RemoveComponent`.** Gives the flat `Board` an ownership relationship ADR 0003 rejects, and changes every fixture that builds through it.
- **Keeping a group of one as a valid state.** Shows nothing, costs a step to edit the member, and nothing can grow it.
- **Letting `Ctrl+G` run when every member is selected.** Replaces the group with an identical one and throws the user out of the scope.
- **The strict load path throwing.** Refuses boards the library itself damaged, for a repair that removes nothing visible.
- **Making `Board` enforce the invariant**, by repairing in its mutators or hiding them behind a builder. Reopens ADR 0003 for a host misusing a low-level API.
