# 58 — `Custom` editor escape hatch

**What to build:** A component author whose property doesn't fit any built-in `EditorKind` supplies their own editor: `EditorKind.Custom` takes an author-provided render fragment over the props, which the panel hosts like any other control — committing through the same undoable path. (ADR 0008.)

**Blocked by:** 56 (Property panel + attribute schema + first editors)

**Status:** ready-for-agent

- [ ] A property can declare `EditorKind.Custom` with an author-supplied render fragment
- [ ] The custom editor renders inside the panel alongside built-in controls
- [ ] Commits from a custom editor go through the same single-gesture undoable path
- [ ] A Demo custom component exercises the escape hatch end to end
- [ ] bUnit coverage
