# 108 — Mixed values

**What to build:** A property whose targets disagree shows as mixed and stays editable, with one commit writing to every target, so bulk edits work across differing shapes (ADR 0040). `Mixed` is decided per row, null counts as a value, colours compare case-insensitively; a commit writes to every target in one entry and skips those already holding the value. One display rule per `EditorKind`, shared by the panel and the later property bar: hatched swatch for colour, a "Mixed" placeholder for text and number, no active option with a disabled "Mixed" entry for dropdown, an indeterminate checkbox. `CustomEditorContext` gains `IsMixed` so an author's `Custom` editor can show the state. The panel reads the expanded selection, so a selected group is editable through its members.

**Blocked by:** 67 (Property roles replace shared tags)

**Status:** resolved

- [x] Selecting two rectangles with different fills shows a hatched swatch; picking a colour writes both in one entry
- [x] A text row over disagreeing values shows the placeholder and typing a value writes all; a checkbox row shows indeterminate
- [x] A custom editor receives `IsMixed` true when its targets disagree
- [x] Selecting a group shows the members' rows and a commit writes to every member
- [x] Two targets where one already holds the new value produce one command, not two
- [x] bUnit covers each editor kind's display rule and the commit skip
- [x] A demo page shows a mixed row for the visual suite; full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [x] `CONTEXT.md`'s `Mixed` and `Property panel` terms describe what shipped

Shipped with choices the ticket did not spell out. The rule lives in `Panel/MixedValue` (`IsMixed`, `AreEqual`, the "Mixed" label), and `Commit`'s skip uses the same comparison. A locked target still counts toward the mixed test, because the row describes what is selected, but a commit skips it as before. As a result, a partly locked group can stay mixed after an edit. Such a row is keyed on an edit count, so its field is rebuilt rather than keeping the typed text. A mixed swatch is hatched and labelled "Mixed" beside it. Its hidden value is `#010203` rather than `#000000`, because the native picker opens on that value and confirming the same value fires no change, which would make black impossible to apply. A mixed swatch is never the themed state, even when one target is `null`, so "Use theme colour" stays on offer. The checkbox's indeterminate state is set through a new `setIndeterminate` export in the canvas's JS module, which the panel imports. A checkbox that stops being mixed is cleared once. The image editor shows "Mixed" above its buttons when the selected images hold different pictures. That closes ticket 98's carry-over.

A new `/mixed-values-demo` page (two rectangles with different fills and stroke widths, two texts with different weights, and the panel) backs two baselines. Its `?locked=true` variant groups the rectangles with one locked, which backs the probes for a still-mixed row clearing its typed text and a mixed swatch not opening on black. The property bar does not exist yet; when it lands, it should read `MixedValue` and draw the hatch in its glyph.
