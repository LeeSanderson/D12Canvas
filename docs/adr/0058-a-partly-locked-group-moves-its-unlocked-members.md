# An operation on a partly-locked group acts on its unlocked members

ADR 0044 made a group locked only when every member is. So a group with some locked members takes primary presses on its unlocked ones and can be selected, dragged, resized, nudged and aligned as a unit. ADR 0017 says nothing modifies a locked entity "by any route". Together those left every operation on such a group undefined, and ADR 0044 handed the question on.

**An operation on a partly-locked group acts on its unlocked members, and its locked members stay exactly as they are.** The group's bounds are derived (ADR 0008), so they cover both parts after the operation, wherever each ended up.

## Why skip

Three answers were open. Skipping is the only one that keeps ADR 0017's "by any route" without an exception.

Moving everything, Figma's semantic, protects a member only when it is selected on its own. A user who locks a background image and then drags its group carries the image along, which is the accident locking exists to prevent.

Refusing the operation punishes the common case. Lock one background image inside a group of shapes and the shapes can no longer be dragged as a unit. The user would have to enter the group and drag them there, where the image is skipped anyway. Refusing adds a detour to the same result.

The cost is that a drag the user may read as rigid is not rigid, and the group's bounds jump. That is what the user asked for when they locked the member.

## Move and nudge

`MoveSelection` and the arrow-key nudge apply one delta to every leaf of `ExpandedSelection()` that is not locked. A locked leaf stays at its committed bounds.

The box that snaps during a move is the unlocked members' box, because it is the box that moves. A locked member left behind is stationary content and a snap candidate under ADR 0024's existing rule that locked entities are candidates. No new snapping rule is needed.

## Resize

`ResizeSelection` keeps the handles on the group's real bounds, locked members included, and scales the unlocked members proportionally inside that box. Locked members keep their size and position. The scale factor comes from the box under the pointer, so nothing distorts.

- **The minimum-size floor counts only the members that scale.** A locked member cannot be shrunk, so it cannot hold the box at its floor.
- **A centre resize (ADR 0057) uses this whole box's centre.**
- **Mid-resize, the selection box is the frame being dragged.** The handle stays under the pointer, and a locked member may stick out past the frame. At release the box re-derives and snaps out to cover the locked member again. ADR 0020's rule that the commit writes what the preview showed covers entity bounds, and those still match exactly. Only the chrome re-derives.

Showing the live derived bounds instead was rejected. On a shrink past a locked member the handle would come away from the pointer and stay at the member's edge, so the user would lose hold of the handle they are dragging.

## Align and distribute

**Align and distribute measure a partly-locked group by its unlocked members' box.** A fully locked group adds nothing and does not count toward the thresholds of 2 and 3.

Measuring the group's real bounds, as resize does, breaks ADR 0014. Say a locked image defines the group's left edge. Align-left computes a delta from the image's edge, moves the unlocked members, and leaves the image in place. The group's left edge does not change, so it is still not aligned. A second press moves the shapes again, and so does every press after it. ADR 0014 requires a satisfied align to move nothing and push nothing onto the history stack.

With the unlocked box, one press lines that box up and a second press has a zero delta. This is ADR 0037's precedent for edges applied to locked members: a member the command cannot move adds nothing to the bounds it measures.

Resize and align measure different boxes on purpose. Resize's box is the handles on screen under the pointer, so it has to be the group's real bounds. Align has no frame on screen, only a result, and it has to stay stable when pressed twice. **Chrome shows the group's bounds, and commands measure what they can move.**

## Clone drag

A clone drag of a partly-locked group copies the whole group, as `Ctrl+D` would, and every copy follows the pointer, the locked member's copy included. The source group stays where it is.

Skipping protects the locked entity. Its copy is a new entity that is not in `Board` until release, and placing it modifies nothing that exists. Leaving the copy behind would stack a duplicate image under the original, and leaving it out of the fragment would make clone drag the one duplication route that changes a group's membership.

Whether a copy of a locked entity is itself locked is not decided here. It applies just as much to duplicate and paste of a lone locked entity reached by a secondary press, and it has its own ticket.

## Delete and restyle

The same rule covers them. Deleting a partly-locked group deletes its unlocked members, and ADR 0053's repair then removes the group if nothing is left or dissolves it if one member is. A property-bar or panel change writes only to the unlocked members.

## Feedback

Nothing new is drawn. The `Gesture preview` already shows the unlocked members moving and the locked ones staying put, and the derived selection box stretches to cover both, which shows the member still belongs to the group. The preview matches what the release commits.

What is missing is why the member stayed. That is the lock badge, which waits with every other silent lock case in the map's cursor and micro-feedback patch. Deciding it here would mean inventing the library's lock visual inside a decision about moves.

## What this amends

**ADR 0017 is amended by addendum**: a partly-locked group is a selection containing locked entities that a primary press can reach, and every operation on it skips the locked ones.

**ADR 0014 is amended by addendum**: a partly-locked group is measured by its unlocked members' box, and a fully locked group is not counted.

**ADR 0042 is amended by addendum**: "Locked entities cannot take part" rested on a selection containing a locked entity being that one entity alone, which ADR 0044 made untrue. A partly-locked group clones whole.

**ADR 0044 is amended by addendum**: the handed-on move is answered.

**ADR 0008, ADR 0020, ADR 0024, ADR 0053 and ADR 0057 are reused, not amended.**

## Considered and rejected

- **Moving everything, locked members included**: a lock then protects a member only when it is selected alone, which contradicts ADR 0017.
- **Refusing the operation**: forces a detour through the `Entered group` to reach the same skipped result, and needs a visible reason or reads as a broken drag.
- **Scaling against the unlocked members' box on resize**: the handles would stop wrapping the group, so a selected group's chrome would need a second rule for where its box is drawn.
- **Measuring the group's real bounds for align**: a second press moves the members again, without end.
- **Showing the live derived bounds mid-resize**: the handle leaves the pointer.
- **Leaving the locked member's copy behind on a clone drag, or out of the copy**: a duplicate stacked on the original, or a clone that changes membership.
- **A lock indicator decided for this case alone**: belongs with every other silent lock case.
