# 13. Glyph set, D12Icon and the glyph gallery

**What to build:** the library gets its own glyph set and a public component to draw it. A host can render any of the 59 library glyphs with `<D12Icon Glyph="Glyphs.AlignLeft" />`, resize them with `--d12-icon-size`, change their weight with `--d12-icon-stroke`, and paint accent parts with `--d12-icon-accent`. A new Demo glyph gallery shows every glyph so the drawings can be judged and later changes show up as a diff. No production site uses the glyphs yet, so no existing baseline moves.

See the spec's "Glyph model and rendering" and "Glyph list and drawing rules" sections for the record shape, the full glyph table and the drawing rules. Start the drawings from the rough paths on the throwaway branches `prototype/palette-look` (`ProtoGlyphs`) and `prototype/property-bar-look` (`ProtoBarData`).

**Blocked by:** None. Can start immediately.

**Status:** resolved

- [x] `Glyph` is an immutable record of `GlyphPart(string Data, GlyphPaint Paint)`; `GlyphPaint` is `Stroke` (default), `Fill`, `AccentStroke` or `AccentFill`; every part is SVG path data
- [x] `Glyphs` is a public static catalogue holding all 59 glyphs from the spec's table, names matched to existing enum members where one exists
- [x] Every glyph follows the drawing rules: 16 grid, stroke 1.5, round caps and joins, every coordinate in the 1 to 15 box, strokes on whole pixels at 16px; text vertical align glyphs differ from the object-align glyphs
- [x] `D12Icon` renders one `<svg viewBox="0 0 16 16" class="d12-icon" fill="none" stroke="currentColor">` with round caps and joins, takes `Glyph`, `Label`, `Class` and splatted attributes, and has no `Size` parameter
- [x] Size defaults to 16px and follows `--d12-icon-size` from any ancestor; stroke defaults to 1.5 and follows `--d12-icon-stroke`
- [x] Accent parts carry class `d12-icon-accent` and take `--d12-icon-accent`, falling back to `currentColor`; no colour literal appears in any style block
- [x] Without `Label` the svg is `aria-hidden="true" focusable="false"`; with `Label` it is `role="img"` with that `aria-label`; there is never a `<title>`; the XML doc says to name the control and leave its icon decorative
- [x] A Demo glyph gallery page shows every member of `Glyphs` with its name at 16px, at 1x and 2x, in light and dark; accent glyphs show colour, hatch and dashed states; the App doesn't link to it
- [x] The user signs off a gallery screenshot before the slice commits
- [x] A unit test over `Glyphs` fails if any coordinate lies outside the 1 to 15 box or any public member is missing from the gallery
- [x] bUnit tests cover `D12Icon`'s decorative and labelled markup
- [x] The gallery has a Playwright screenshot baseline in light and dark; the full visual suite passes with no other baseline moved
- [x] `CONTEXT.md`'s **Glyph** and **Glyph set** entries still match what was built

## Comments

- The spec's table lists 60 glyphs, not 59 (22 Property bar glyphs, not 25). All 60 are drawn.
- The 1:1 zoom glyph is `Glyphs.ZoomTo100Percent`, matching the `ContextMenuCommand` member, not the table's working name `ZoomTo100`.
- Still open: `D12Icon` carries its own `<style>` block, so every icon on a page emits a copy. Revisit once the Palette, Property bar and context menu use it.
