# Verification and baselines

Type: grilling
Status: open
Blocked by: 08, 11
Map: [Library icons, Palette and Property bar](../map.md)

## Question

How is the work verified, and how do the visual-test baselines move?

Decide:

- which visual tests change (Palette, Property bar, `PropertyPanel`, context menu, LOD placeholder, image placeholder) in light and dark, and whether new ones are needed (flyouts, tiles, the fully locked bar)
- whether baselines move in one commit at the end or with each slice the spec cuts
- what bUnit and Playwright behaviour tests cover: the radiogroup keyboard rules, the menu-button flyouts, the lone Unlock bar, `OptionGlyphs` on a consumer property

## Comments

From [Context menu glyphs](08-context-menu-glyphs.md): every context-menu baseline changes, both the strip and the rows, since each row gains a glyph and the check mark becomes a glyph. The Image editor's "Choose file…" and "Remove image" buttons gain glyphs too, so the `PropertyPanel` baselines for an image selection change.

From [Panel partial-lock count](10-panel-partial-lock-count.md): a partly locked selection now shows a line above the panel's fields, the lock glyph and "1 of 3 locked; changes skip them". Any `PropertyPanel` baseline with a partly locked selection changes, and the line may need a baseline of its own and a bUnit test that it shows only for a partial lock.

From [Glyph drawing handoff](11-glyph-drawing-handoff.md): a new glyph gallery page in `D12Canvas.Demo` needs a Playwright baseline in light and dark, and a unit test over `Glyphs` checks the padding box and that every member is in the gallery. The user signs off a gallery screenshot before the drawing slice commits. Decide where that slice sits in the baseline order, since every site baseline changes once the final paths land.
