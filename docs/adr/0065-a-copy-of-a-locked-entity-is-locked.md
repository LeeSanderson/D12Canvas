# A copy of a locked entity is locked, and Cut carries only what it deletes

ADR 0058 has a clone drag of a partly-locked group copy the locked member and left open whether that copy lands locked. The question is wider than clone drag. A secondary press selects a lone locked entity (ADR 0022), and `Ctrl+C`, `Ctrl+D` and the menu's clipboard rows all read that selection. No ADR said which of them a locked selection allows, or what a copy inherits. ADR 0013's duplication path predates locking, and ADR 0017 added `Locked` without saying.

**Copy and Duplicate work on a selection holding locked entities, and every copy carries `Locked`. Cut is eligible only when its delete would remove something, and its payload is exactly what that delete removes.**

## Copying is not modifying

ADR 0017 defines a lock as "nothing modifies this". Copy and Duplicate leave the source untouched and only create something new, which is the argument ADR 0058 already made for clone drag: "placing it modifies nothing that exists". Making them ineligible would widen the lock from "nothing modifies this" to "nothing reads this", which is further than ADR 0017's reasoning goes. Copying a locked template into place is also an ordinary thing to want.

Cut deletes its source, so on a lone locked entity, or a fully locked group, it has nothing it may remove and is ineligible. Clone drag on a lone locked entity cannot start at all, because a primary press never reaches one. It arises only through a partly-locked group, which ADR 0058 covers.

## The copy keeps the lock

Every route that copies carries `Locked` onto the copy: `Ctrl+C` and paste, `Ctrl+D` and a `Duplicate run`, clone drag, and the menu rows. It applies to instances and edges alike, since ADR 0017 puts the flag on both and ADR 0045 makes a selected edge always travel.

The two failures are not the same size. Carrying the lock costs one visible step. Duplicate and paste select what they produce, so a locked copy is already the selection, and `Ctrl+Shift+L` or the Unlock row frees it. The friction shows up on the first attempt to drag, with the fix in reach.

Dropping the lock costs a silent one. A user who locks a card's background image and clones the card with `Alt` gets a new card whose background is unlocked. Nothing shows it, and the next drag across that card takes the image along, which is the accident ADR 0017 exists to prevent. The user finds out afterwards and has to re-lock every copy by hand.

The accepted cost falls on `Ctrl+D` of a lone locked entity: the copy lands at `+20, +20` over the original and cannot be dragged off until it is unlocked. A `Duplicate run` still cascades, because ADR 0039 replays committed offsets and needs no move between steps.

## The clipboard keeps it too

The payload goes through the serializer, which already writes `Locked` as an optional field, so keeping it costs nothing and a paste into another board stays locked. Stripping it would need a special case and would make copy-paste differ from `Ctrl+D` for no reason a user could see. Foreign content, text or a bitmap, arrives unlocked because it never had the field.

Through this library's own selection rules a payload holds at most a lone locked entity or groups with locked members. A hand-edited or foreign payload can hold several locked entities at the top level, and pasting it produces a selection ADR 0023 calls unreachable. Its row rule would then offer Lock on things already locked. **The Lock row reads Unlock when every top-level selected entity is locked**, and Lock otherwise. That reads the same as before for every state the library produces: a partly-locked group is not locked, so the row still reads Lock on it, as ADR 0058's addendum to ADR 0017 says. Such a paste is accepted, not rejected.

## Cut is a move through the clipboard

Cut is copy plus delete, and ADR 0058 makes delete on a partly-locked group remove only its unlocked members. If Cut's copy half carried the whole group, cutting a card with a locked background would leave the background in place and paste a second, locked copy of it. A move would leave two backgrounds.

So Cut's payload is exactly the set its delete removes. The unlocked members travel and the locked ones stay, which is what a drag does to them under ADR 0058. In the source, ADR 0053's repair removes the group if nothing is left, dissolves it if one member is left, and otherwise leaves it holding the locked members. The pasted fragment is a group of the cut members, or a lone entity when only one was cut.

That makes Cut the one route whose payload differs from Copy's. ADR 0058 rejected leaving a locked member out of a clone drag because the clone would change a group's membership. That reason is about a copy. A move that leaves a locked member behind has already split the members.

## Not decided here

Nothing marks a copy as locked when it lands. That is the lock badge, which waits with every other silent lock case in the map's cursor and micro-feedback patch.

## Consequences

**ADR 0013 is amended by addendum**: the payload and every duplication route carry `Locked`, and Cut carries only what its delete removes.

**ADR 0017 is amended by addendum**: copying is not modifying, so a lock does not stop a copy, and the copy is locked.

**ADR 0023 is amended by addendum**: Copy and Duplicate are eligible on a locked selection, Cut only when it would remove something, and the Lock row reads Unlock when every top-level selected entity is locked.

**ADR 0042 and ADR 0058 are amended by addendum**: the copy of a locked member lands locked.

**ADR 0039, ADR 0045 and ADR 0053 are reused, not amended.**

## Considered and rejected

- **Copying refused on a locked selection**: widens the lock from "nothing modifies this" to "nothing reads this", and blocks copying a locked template.
- **Dropping `Locked` on the copy**: a cloned card's locked background comes out unlocked with nothing to show it, and the next drag carries it off.
- **Carrying the lock on some routes and not others**: copy-paste and `Ctrl+D` would differ for no reason a user can see.
- **Stripping `Locked` from the clipboard payload**: a special case in the serializer path for the same inconsistency.
- **Rejecting a paste that yields several top-level locked entities**: the row rule can express it, so there is nothing to protect by refusing.
- **Cut carrying the whole partly-locked group**: a cut-then-paste leaves the locked original and adds a locked copy.
- **Cut ineligible on any selection holding a locked entity**: blocks cutting the unlocked members of the common card case for no gain.
