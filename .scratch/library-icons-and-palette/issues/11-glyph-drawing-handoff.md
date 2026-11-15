# Glyph drawing handoff

Type: grilling
Status: open
Blocked by: 08
Map: [Library icons, Palette and Property bar](../map.md)

## Question

Who draws the final glyphs, and what does the spec hand over?

The Palette, Property bar and panel prototypes drew rough glyphs. Decide whether the spec carries final path data, or lists each glyph with the drawing rules from [How the library defines and renders an icon](03-icon-definition-and-rendering.md) and leaves the drawing to implementation. Also decide how a drawing is accepted (a review page, a visual baseline, the user's sign-off) and the final glyph list, once Context menu glyphs fixes the menu's share.

## Comments

From [Context menu glyphs](08-context-menu-glyphs.md): the menu claims the 8 strip glyphs, about 22 row glyphs and `Glyphs.Check`, and the Image editor's two buttons reuse row glyphs. The set grows to about 65. The 30 to 50 figure was an estimate, not a limit, so the handoff need not trim it.
