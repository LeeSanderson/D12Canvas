# 57 — Full built-in `EditorKind` set

**What to build:** The property panel covers the whole closed set of built-in `EditorKind`s — Color, Checkbox, Dropdown, and the rest of the set per ADR 0008 — so the built-in component types' visual props (fill, stroke, note colour, font settings, image fit, …) are all editable. Every control commits through the same single-gesture undoable path.

**Blocked by:** 56 (Property panel + attribute schema + first editors)

**Status:** ready-for-agent

- [ ] Each built-in `EditorKind` renders its control and commits edits correctly
- [ ] All four built-in component types' declared props are fully editable in the panel
- [ ] Every commit is one undoable history entry
- [ ] bUnit coverage per control; screenshot cases for the new controls
