# 43 — Inline WYSIWYG text editing

**What to build:** An end user edits a text-carrying built-in's content directly on the canvas — WYSIWYG, in place, not through a panel. Committing on blur records exactly one undoable gesture (introducing `MutateEntity`, the command for opaque props mutations); Escape cancels the edit without touching history. (ADR 0008 — `Text`-type content is inline-edited, excluded from the property panel.)

**Blocked by:** 37 (History core: undo/redo move & resize), 40 (Sticky Note built-in), 41 (Text built-in)

**Status:** ready-for-agent

- [ ] Sticky Note and Text content is editable in place on the canvas
- [ ] Committing on blur is one history entry (`MutateEntity`); undo restores the prior text
- [ ] Escape cancels the edit, reverting content with no history entry
- [ ] Edited text round-trips through persistence
- [ ] Screenshot case for the in-editing state
