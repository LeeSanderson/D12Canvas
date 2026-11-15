# Context menu glyphs

Type: grilling
Status: resolved
Map: [Library icons, Palette and Property bar](../map.md)

## Question

What does the context menu draw after this work?

Decide:

- whether the align/distribute strip's 8 hand-built glyphs (`ContextMenu.razor.cs` `GlyphMarkup`) move onto the shared `Glyphs` set and `D12Icon`
- whether the text menu rows (Cut, Copy, Paste, Group, Lock, Zoom to Fit and the rest, listed in [Icon inventory](01-icon-inventory.md)) take a leading glyph
- whether the `✓` toggle mark becomes a glyph

Each "yes" adds glyphs. The answer fixes how many of the set's 30 to 50 glyphs the menu claims.

## Answer

The context menu draws every icon from the shared set: the strip, a glyph on every row, and the check mark.

- **Strip.** The 8 align/distribute glyphs move onto `Glyphs` and render through `D12Icon`. `GlyphMarkup` in `ContextMenu.razor.cs` goes. The buttons keep their `aria-label`, and the glyph is decorative.
- **Rows.** Every row gets a leading glyph, about 22 new ones (the row list is in [Icon inventory](01-icon-inventory.md)). Unlock All reuses Unlock, and Lock and Unlock are the same glyphs the Property bar uses. A row reads: check column, glyph, label, hint. The glyph draws in the row's text colour through `currentColor`, including on hover; Delete gets no danger colour. The glyph is decorative and the label stays the accessible name.
- **Check mark.** The Unicode `✓` becomes `Glyphs.Check`, kept in its own column. A toggle row (Snap to Grid, Object Snapping) keeps its own glyph, and the check shows that it's on, as in OS menus. `aria-checked` still carries the state. The column appears only when the menu holds a toggle row, as today (`HasToggles`).
- **Image editor.** The "Choose file…" and "Remove image" buttons in `ImagePictureEditor.razor` take the same glyphs as the menu's "Choose image…" and "Remove image" rows, placed before the text and decorative. One action has one glyph wherever it shows.
- **Budget.** The 30 to 50 figure from How the library defines and renders an icon was an estimate, not a limit. With the rows the set grows to about 65 glyphs, and that's accepted.

The menu stays internal: `ContextMenuRow` has no public route, so none of this reaches consumers.
