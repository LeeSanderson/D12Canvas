# Verification and baselines

Type: grilling
Status: resolved
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

## Answer

Baselines move with each slice, never in one commit at the end. The commit-gating rule in `docs/agents/testing.md` requires it: any commit that changes rendered markup runs the full visual suite and includes its baseline updates.

### Slice order

The drawing slice goes first. It adds `Glyph`, `Glyphs` and `D12Icon`, draws all 59 glyphs, and builds the Demo glyph gallery with its light and dark baselines and the padding-box unit test. The user signs off a gallery screenshot before it commits. No production site uses the glyphs yet, so no existing baseline moves. Each site slice after it (Palette, Property bar, panel tiles, context menu, LOD and image placeholders) switches to the final glyphs and moves its own baselines once. Drawing last was rejected because the swap would move every site baseline a second time.

### New screenshot cases

Nine cases, each in light and dark (18 baselines):

1. Palette "…" flyout open, with columns by category
2. Property bar option flyout open, glyph-and-name rows
3. Property bar colour flyout open, preset grid
4. Property bar with a partly locked selection, lock count at the end
5. Property bar fully locked, the lone Unlock button
6. PropertyPanel tiles, single value, one tile pressed
7. PropertyPanel tiles, mixed value, no tile pressed and the label tagged
8. PropertyPanel partly locked line, "1 of 3 locked; changes skip them"
9. Demo glyph gallery

The rail tooltip gets no screenshot because hover states flake. A probe covers it instead. An existing case without a dark twin gets one only when its slice changes that site anyway. That adds dark twins for the hatched-fill bar and the image editor buttons, and leaves cases such as the empty panel alone.

Every existing baseline at the changed sites moves: Palette, Property bar, PropertyPanel, context menu, LOD placeholder, image routes, the locked-shape case and the connector palette entry.

### Plain Dropdown baseline

`PropertyPanelWithDropdownControl` (light and dark) moves to a consumer property with no `OptionGlyphs`, so it keeps covering the plain Dropdown path. If the demo has no such property, the slice adds one to the demo's custom component type.

### Behaviour tests

bUnit covers markup and component state:

- tiles carry `role="radiogroup"` and `role="radio"` with `aria-checked`; a mixed value checks none and tags the label; a locked selection disables them
- Space or Enter on a tile commits one edit; arrow keys move the roving `tabindex` and do not commit
- `OptionGlyphs` on a consumer-registered property gives tiles in the panel and a chip with a flyout in the bar; without it the property stays a plain Dropdown
- `Icon` null draws `Glyphs.Component`, and a host type's glyph shows on the rail and in the flyout
- the panel's partly locked line shows only for a partial lock; the bar's lock count follows the same rule, and a fully locked selection gives the lone Unlock button
- `ColourPresets` throws on a bad `#RRGGBB` or a null list, an empty list removes the grid, and each swatch's accessible name is its `Name`
- a checked context-menu row draws `Glyphs.Check` in its own column
- the `Glyphs` unit test checks the padding box and that every member is in the gallery

Playwright probes cover what needs a real browser:

- a Property bar chip opens its flyout and moves focus into it; Escape and an outside click close it and send focus back to the chip; choosing an option commits and closes
- the Palette "…" flyout follows the same rules, and an entry in it still places on the board
- arrow keys move real focus across the tiles, including onto a wrapped second row
- a rail button has an accessible name and shows its tooltip on hover and on focus
