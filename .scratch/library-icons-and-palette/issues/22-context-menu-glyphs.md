# 22. Context menu glyphs

**What to build:** the context menu draws every icon from the shared glyph set. The align and distribute strip uses the library glyphs. Every row leads with a glyph in the row's text colour, so Cut, Group or Zoom to Fit are quicker to find. Toggle rows such as Snap to Grid keep their own glyph and show `Glyphs.Check` in a separate column when on, replacing the Unicode check mark.

See the spec's "Context menu" section and the glyph table for each row's glyph.

**Blocked by:** 13 (Glyph set, D12Icon and the glyph gallery)

**Status:** resolved

- [x] The 8 strip buttons draw their `Glyphs` members through `D12Icon`, decorative; the hand-built glyph markup is gone; the buttons keep their `aria-label`
- [x] Every row has a leading decorative glyph; a row reads check column, glyph, label, hint
- [x] Unlock All reuses `Glyphs.Unlock`; "Add port here" and "Add port…" share `Glyphs.AddPort`; Lock and Unlock are the same glyphs the bar and panel use
- [x] Row glyphs draw in `currentColor`, including on hover; Delete gets no danger colour
- [x] A checked toggle row draws `Glyphs.Check` in its own column beside its row glyph; the column appears only when the menu holds a toggle row; `aria-checked` still carries the state
- [x] The label stays each row's accessible name; rows gain no public route
- [x] bUnit: every row draws a glyph, a checked row draws `Glyphs.Check` in its own column, strip buttons keep their names
- [x] The full visual suite passes with every context menu baseline updated in this commit

## Comments

- `DiagramCanvas` now cascades `D12IconStyles.InScope` around the context menu, as it does around the Property bar, so the menu's icons emit no `<style>` block of their own. A canvas-level bUnit test guards this.
- The check column grows from 12px to 16px to hold `Glyphs.Check`. The menu stays 224px wide, and the longest row ("Bring to Front", Ctrl+Shift+]) still fits.
- `ContextMenuRow` gains `Icon`, and its `Glyph` flag is renamed `InStrip`.
- The strip buttons keep their native `title`, as before. The spec bans `title` only in the Property bar and the Palette.
