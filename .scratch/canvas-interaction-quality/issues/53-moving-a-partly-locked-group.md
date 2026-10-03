# Moving a partly-locked group

Type: grilling
Status: resolved
Blocked by:

## Question

Decide what a move, resize, nudge or align does to a `Group` at the top level when some of its members are locked and some are not.

Surfaced while resolving [Selecting inside a group](35-selecting-inside-a-group.md). ADR 0044 decided that a group is locked only when **every** member is, so a partly-locked group takes primary presses on its unlocked members and can be selected and dragged as a unit. ADR 0017 says nothing modifies a locked entity. Those two together leave the move undefined.

- **Skip the locked members.** ADR 0017's align precedent ("a selection containing one locked entity aligns the others rather than refusing") points this way, and it is what `ExpandedSelection()` plus the existing per-entity lock check would do by default. The group's shape then changes under what the user thinks is a rigid-body move, and its computed bounds jump.
- **Refuse the move.** The group stays put and the drag does nothing, which needs a visible reason or it reads as a broken drag.
- **Move everything, locked members included.** Treats a lock as protecting the member at its own level only, which is Figma's semantic and contradicts ADR 0017's "nothing modifies this".

Decide also:

- **Whether resize follows the same answer as move.** Resize scales members within the group's bounds (ADR 0018), so a skipped locked member also distorts the scale the others get.
- **What the user sees during the drag.** ADR 0020's `Gesture preview` would show the skipped member staying behind, or nothing moving. Either has to be legible before release.
- **Whether the keyboard nudge and the menu's align strip match the pointer**, since all of them read the same selection.

Touches ADR 0017 (what locked protects) and ADR 0044 (what a partly-locked group is).

## Answer

Recorded as [ADR 0058](../../../docs/adr/0058-a-partly-locked-group-moves-its-unlocked-members.md). Grilled with the dev, six questions, each agreed as recommended.

1. **Skip.** An operation on a partly-locked group acts on its unlocked members and the locked ones stay put. It is the only answer that keeps ADR 0017's "by any route" with no exception. Moving everything carries a locked background image along with its group, and refusing forces a detour through the `Entered group` to the same result.
2. **Resize scales against the group's real bounds.** The handles stay where they are, the unlocked members scale inside that box, and the locked ones keep their size and position. The floor counts only the members that scale. A centre resize uses this box's centre.
3. **No new feedback.** The `Gesture preview` shows the member left behind and the derived box stretches to cover it. Why it stayed is the lock badge, added to the cursor and micro-feedback fog patch.
4. **Nudge matches move. Align and distribute measure the unlocked members' box.** With the real bounds, a locked member defining the aligned edge makes every press move the members again. A fully locked group is not counted. Chrome shows the group's bounds, and commands measure what they can move.
5. **A clone drag copies the whole group and every copy follows the pointer.** ADR 0042's "locked entities cannot take part" rested on a premise ADR 0044 broke. Whether a copy of a locked entity is itself locked is split out.
6. **Mid-resize, the box is the frame being dragged**, so the handle stays under the pointer. It re-derives at release.

Checking the answers against each other turned up question 6, the conflict between resize against the real bounds and the derived box mid-drag. Grepping showed that no `.cs` file implements `Locked` yet, so the ticket's "existing per-entity lock check" does not exist and nothing has to be undone. ADR 0024 already makes locked entities snap candidates, so a member left behind is one with no new rule. Delete and restyle follow the same rule by derivation, with ADR 0053's repair after a delete.

Amends ADRs 0014, 0017, 0042 and 0044 by addendum. ADR 0017's addendum also notes that its "no mixed locked-and-unlocked state" claim is out of date, though its Lock/Unlock row rule still works. `CONTEXT.md`'s `Locked` entry gains one sentence. Split out [Whether a copy of a locked entity is locked](61-copy-of-a-locked-entity.md).
