# Whether a copy of a locked entity is locked

Type: grilling
Status: open
Blocked by:

## Question

Decide whether duplicate, paste and clone drag carry `Locked` onto the copy of a locked entity.

Surfaced while resolving [Moving a partly-locked group](53-moving-a-partly-locked-group.md). ADR 0058 has a clone drag of a partly-locked group copy the locked member and carry the copy with the pointer, and left open whether that copy lands locked. No ADR decides it: ADR 0013's duplication path predates locking, and ADR 0017 added `Locked` without saying what a copy inherits.

- **It applies to more than clone drag.** A secondary press selects a lone locked entity (ADR 0022), and `Ctrl+C`, `Ctrl+D` and the menu rows all read that selection. Check ADR 0017 and ADR 0023 to see whether any of them is meant to be available on a locked selection at all before deciding what the copy carries.
- **Carrying it** makes a copy a faithful copy, and a locked template copied into place stays protected. The copy then cannot be primary-pressed or moved straight after it lands, so a duplicate run or a paste the user means to position next needs an unlock first.
- **Dropping it** makes every copy immediately editable, and loses the lock silently on something the user locked on purpose.
- **Clipboard round-trips.** A paste into another board goes through the serializer, which persists `Locked` as an optional field. Decide whether the clipboard payload keeps it.

Touches ADR 0013 (clipboard and duplication), ADR 0017 (what locked protects), ADR 0039 (`Duplicate run`) and ADR 0042 (clone drag).
