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
