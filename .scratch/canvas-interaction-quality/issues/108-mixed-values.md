# 108 — Mixed values

**What to build:** A property whose targets disagree shows as mixed and stays editable, with one commit writing to every target, so bulk edits work across differing shapes (ADR 0040). `Mixed` is decided per row, null counts as a value, colours compare case-insensitively; a commit writes to every target in one entry and skips those already holding the value. One display rule per `EditorKind`, shared by the panel and the later property bar: hatched swatch for colour, a "Mixed" placeholder for text and number, no active option with a disabled "Mixed" entry for dropdown, an indeterminate checkbox. `CustomEditorContext` gains `IsMixed` so an author's `Custom` editor can show the state. The panel reads the expanded selection, so a selected group is editable through its members.

**Blocked by:** 67 (Property roles replace shared tags)

**Status:** ready-for-agent

- [ ] Selecting two rectangles with different fills shows a hatched swatch; picking a colour writes both in one entry
- [ ] A text row over disagreeing values shows the placeholder and typing a value writes all; a checkbox row shows indeterminate
- [ ] A custom editor receives `IsMixed` true when its targets disagree
- [ ] Selecting a group shows the members' rows and a commit writes to every member
- [ ] Two targets where one already holds the new value produce one command, not two
- [ ] bUnit covers each editor kind's display rule and the commit skip
- [ ] A demo page shows a mixed row for the visual suite; full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [ ] `CONTEXT.md`'s `Mixed` and `Property panel` terms describe what shipped
