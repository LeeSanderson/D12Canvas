# 59 — Cross-type shared-property editing

**What to build:** An end user multi-selects instances of different component types and the property panel shows only the properties explicitly tagged as shared across types — editing one applies the change to every selected instance as a single atomic, undoable gesture. Untagged properties never appear for a cross-type selection. (ADR 0008.)

**Blocked by:** 32 (Marquee + shift-click multi-select), 56 (Property panel + attribute schema + first editors)

**Status:** ready-for-agent

- [ ] Cross-type multi-selections surface only explicitly-tagged shared properties
- [ ] Committing a shared-property edit applies it to all selected instances
- [ ] The bulk edit is one atomic history entry; undo restores every instance's prior value
- [ ] A same-type multi-selection still edits that type's full declared schema
- [ ] bUnit coverage of tag filtering and bulk apply
