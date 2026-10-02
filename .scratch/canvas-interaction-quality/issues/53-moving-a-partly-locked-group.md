# Moving a partly-locked group

Type: grilling
Status: open
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
