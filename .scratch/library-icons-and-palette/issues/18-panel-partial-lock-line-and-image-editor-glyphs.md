# 18. PropertyPanel partial-lock line and Image editor glyphs

**What to build:** when some of the selected shapes are locked and some aren't, the `PropertyPanel` says so above its fields: the lock glyph and "1 of 3 locked; changes skip them". The user then knows why an edit changes only some shapes. A fully locked selection keeps today's Unlock button and shows no line. The Image editor's "Choose file…" and "Remove image" buttons get the same glyphs as the context menu's image rows.

See the spec's "PropertyPanel" section.

**Blocked by:** 13 (Glyph set, D12Icon and the glyph gallery)

**Status:** resolved

- [x] A partly locked selection shows `Glyphs.Lock` and "N of M locked; changes skip them" in the muted token, above the fields in the Unlock button's slot
- [x] The line shows only for a partial lock: a fully locked selection shows today's Unlock button and no line; an unlocked selection shows neither
- [x] The line is plain text: no action, no tooltip, no live region, and no `aria-describedby` on the fields; it is the first text in the panel's reading order
- [x] The Image editor's "Choose file…" and "Remove image" buttons show `Glyphs.ChooseImage` and `Glyphs.RemoveImage` before their text, decorative
- [x] bUnit: the line's three cases (partial, full, none) and its wording; the editor buttons draw their glyphs and keep their text as accessible name
- [x] New screenshot case: the partly locked line, in light and dark; the image editor buttons case gains a dark twin
- [x] The full visual suite passes with PropertyPanel and image route baselines updated in this commit

## Comments

- The panel counts its own targets, the expanded selected instances, through `LockTally`. It leaves out selected edges, which the bar counts, because the panel never edits an edge. The bar and the panel can't disagree: a selection that holds both an instance and an edge has no bar.
- The line is a `<p>` holding a decorative `Glyphs.Lock` and the summary. It takes the Unlock button's slot, so it is the panel's first child.
- The new screenshot case is on the locking demo. It selects both rectangles and recolours them from the panel, so the picture shows the free one changed and the locked one skipped, under the line.
- The image routes demo sets a dark board theme of its own, so the new dark twin of the image editor case matches the light one pixel for pixel. Only the page's colour scheme differs.
- Baselines:
  - New: the panel's partly locked line, light and dark, and the dark twin of the image editor case.
  - Moved: the image editor case and the image object menu case, which shows the editor behind the menu. Both PNG and HTML moved, for the button glyphs.
  - Moved: the two partly locked bar cases from ticket 21, PNG and HTML, because their panel now shows the line.
  - The `MultiSelectionMoveResize` `aria-selected` noise didn't appear.
