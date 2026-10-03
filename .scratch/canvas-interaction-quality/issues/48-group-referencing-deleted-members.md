# A Group left referencing deleted members

Type: grilling
Status: resolved

## Question

Decide what happens to a `Group` entity when its members are deleted, and whether `Board` tolerates a group referencing ids that no longer resolve.

Split out of [Edges in a multi-selection](28-edges-in-multi-selection.md), which found it separable: nothing about it changes with edges in the selection, and the fog had attached the two only because both live in `OnDeletePressed`.

The defect is real and its blast radius is a saved file rather than a session. `OnDeletePressed` reads through `ExpandedSelection()`, which flattens a selected group down to its member ids, so delete builds one `RemoveEntityCommand` per member and never removes the `Group`. `Board.GetBounds` filters missing members out with `Where(b => b.HasValue)`, so a partly-deleted group silently shrinks to its survivors and a fully-emptied one persists as a zero-member entity with null bounds, invisible on screen and serialized into the file by ADR 0004. Undo restores the members and the group reads correctly again, which is why this has been liveable and why nobody has hit it in a session.

Decide:

- **Whether a `Group` tolerates dead member ids or is repaired.** ADR 0006's own addendum already establishes tolerance for the *selection*: "a selection id that no longer resolves to a live entity is dropped when the selection is read". Whether the same rule extends to a persisted entity's membership is a different question, because a selection is transient view state and a `Group` is board content that round-trips through ADR 0004.
- **If repaired, by what.** A cascade inside `RemoveEntityCommand` makes deletion aware of a containing group, which is an ownership relationship ADR 0003 deliberately kept out of the flat `Board`. A prune at serialise time is cheaper and leaves the in-memory board wrong. A prune on read matches the selection's own rule. Each has a different undo story, and ADR 0007 requires the inverse to restore what was there.
- **What a one-member group means.** Deleting three of a four-member group leaves a group of one, which `GetBounds` answers correctly and which nothing forbids. Decide whether that is a valid state, is auto-ungrouped, or is the same question as the zero-member case at a different count.
- **Whether `FocusableTabStopIds` can produce a phantom stop.** `OrderedTabStops` adds a stop per visible group using `Board.GetBounds(group)!.Value`, which is a non-null assertion over a method that returns null for a group with no resolvable members. Establish whether a zero-member group can reach that line before deciding anything else, because if it can this is a crash rather than a tidiness question.
- **Whether nesting changes the answer.** `MemberBounds` recurses into nested groups and `FindContainingGroup` walks to the outermost, so an emptied inner group sits inside a live outer one. Decide whether a repair rule applies at one level or all the way up.

Its edge-shaped sibling is **already settled the other way** and is the sharpest reason these are two tickets rather than one: deleting a component leaves any edge attached to it holding an endpoint that resolves to nothing, and ADR 0032 named that the dangling-endpoint precedent and relied on it. ADR 0037's closure rule makes that precedent ordinary rather than occasional, since marqueeing one shape of a connected pair deliberately leaves the edge unselected. So the two integrity gaps that look alike in `OnDeletePressed` are not alike: one has a decision behind it and the other does not.

Touches ADR 0003 (the flat model with no ownership tree), ADR 0004 (what reaches the file) and ADR 0007 (what an undo has to restore).

**Update from ADR 0044 ([Selecting inside a group](35-selecting-inside-a-group.md) resolved):** this case is about to become common. Inside an `Entered group` the selection holds direct members, so selecting one member and pressing Delete removes it from the board while its group's `MemberIds` still lists it. That is now an ordinary user action, not an edge case. ADR 0044 fixes the two cases it creates itself (`Ctrl+G` and ungroup inside a group now edit the parent's `MemberIds`) and leaves deletion to this ticket. The scope rule also depends on the answer: ADR 0044 pops the `Entered group` when its group stops existing, so if an emptied group is kept, the scope survives on a group with nothing in it.

## Answer

Recorded as [ADR 0053](../../../docs/adr/0053-a-groups-members-always-resolve.md). Grilled with the dev, six questions.

- **Repaired, not tolerated.** Every id in a live group's `MemberIds` resolves. `OnDeletePressed` builds one `CompositeCommand` holding the removals plus every membership edit they make necessary, the same shape ADR 0044 gave `GroupCommand` and `UngroupCommand`. `RemoveEntityCommand` and `Board` stay unchanged, so ADR 0003 holds, and undo restores everything in one step (ADR 0007).
- **Zero members removes the group; one member dissolves it**, with the survivor going where the group was in its parent. No user action builds a group of one, and it does nothing visible.
- **Nesting:** the rule repeats upward, since removing an emptied inner group can leave its parent with zero or one. Dissolving a one-member group leaves the parent's count unchanged.
- **The phantom tab stop question was a fact, and the answer is no crash.** `GetVisibleGroups` drops a group with null bounds before `OrderedTabStops` reaches its `!`. The real cost was an invisible, unselectable, unremovable entity that warned on every load.
- **Load repairs on both paths.** Partial drops dead ids, drops emptied groups and dissolves one-member groups, including hand-written ones, with a warning for each. Strict repairs the same way without throwing, because the repair removes nothing visible and throwing would refuse boards the library damaged itself.
- **The canvas and load guarantee the invariant, `Board` does not.** Hosts build boards through its public mutators. Readers keep their existing tolerance; the group label and ADR 0044's lock rule count only resolving members.
- **`Ctrl+G` is unavailable when every direct member of the `Entered group` is selected.** Found during the grilling: it was the one user route to a group of one, through ADR 0044's own rule, and running it would only rename the group and pop the scope.

Edges stay on ADR 0032's dangling-endpoint precedent. Amends ADR 0044 and ADR 0004; `CONTEXT.md`'s `Group` entry updated.

## Comments

- My Question 2 claimed no user action can create a group of one, and ADR 0044's `Ctrl+G` rule contradicted that two questions later. Corrected in the session, and the case became Question 5. A second gap surfaced while writing the ADR: ADR 0004 has a strict load path that the load question had not covered, which became Question 6.
