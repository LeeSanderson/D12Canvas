# How the library defines and renders an icon

Type: grilling
Status: resolved
Blocked by: 02
Map: [Library icons, Palette and Property bar](../map.md)

## Question

What shape does an icon have in the library, and how does it render?

Decide:

- the data shape: a path-data record (as the App's `IconPaths.cs` does), a `RenderFragment` per icon, an SVG `<symbol>` sprite, or something else
- whether there is a public icon component, and what it is called in `CONTEXT.md`
- sizing: one design grid, and how a host changes the rendered size
- theming: `currentColor`, stroke width, and how this works with the `--d12-*` tokens and the colour-literal guard test in light and dark
- accessibility: decorative by default, and how a meaningful icon gets its accessible name
- the default stroke width at 16px: Lucide's 24-grid stroke 2 renders 1.33px wide, so compare 2 against 1.5 at 1x and 2x
- how the Lucide path data and the in-house gap icons (stroke width, orthogonal routing, possibly text vertical align) sit side by side, and how a Lucide version bump is done
- packaging: Lucide's LICENSE shipped verbatim as `THIRD-PARTY-NOTICES.txt` in the NuGet package, and a licence header on the path-data file

## Comments

From [Icon inventory](01-icon-inventory.md): the Property bar's Fill, Stroke, Text colour and Edge colour glyphs paint the selection's colour into the icon, with a hatch pattern for mixed and a dashed outline for theme colour. The definition has to allow a glyph whose paint is set per render, or those four stay outside the icon set as swatches.

## Answer

The library draws its own glyphs on a 16 grid and renders them through one public component. This reverses [Icon source and style](02-icon-source-and-style.md): no Lucide paths are copied in.

- **Data.** `Glyph` is an immutable record holding a list of `GlyphPart(string Data, GlyphPaint Paint)`. Every part is SVG path data, with no circle, rect or line elements. `GlyphPaint` is `Stroke` (the default), `Fill`, `AccentStroke` or `AccentFill`. The built-in catalogue is the static `Glyphs` class (`Glyphs.Rectangle`, `Glyphs.AlignLeft`, ...), maintained by hand in `Glyphs.cs` with no generator.
- **Component.** `<D12Icon Glyph="..." />` is public. It renders one `<svg viewBox="0 0 16 16" class="d12-icon" fill="none" stroke="currentColor">` with round caps and joins, and takes a `Class` parameter plus splatted attributes. There is no `Size` parameter.
- **Glossary.** **Glyph** and **Glyph set** are in `CONTEXT.md`. "Icon" stays the word for the slot a glyph fills, such as a registration's icon. `D12Icon` is a code name, not a domain term.
- **Size.** 16px by default. `--d12-icon-size`, set on any ancestor, overrides it, so a consumer can resize the library's own Palette or bar icons from outside.
- **Stroke.** `--d12-icon-stroke`, default 1.5, which is 1.5px at 16px. I rendered real stroke icons at 16px with strokes of 2, 1.75, 1.5 and 1.25, at 1x and 2x, in light and dark. Coordinates that fall on fractions of a pixel went grey at 1x, so the grid is 16 units: one unit per pixel at the size every site uses.
- **Colour.** Glyphs follow the host's `color`, which already comes from the `--d12-*` tokens, so light and dark need no new tokens and nothing trips the colour-literal guard. Accent parts render with class `d12-icon-accent` and take `--d12-icon-accent`, falling back to `currentColor`. The Property bar sets that variable inline per cell to the selection's colour, and its own CSS restyles `.d12-icon-accent` for the hatch (mixed) and dashed muted outline (theme colour) states. That makes Fill, Stroke, Text colour and Edge colour ordinary members of the set, and a consumer's colour editor can reuse them.
- **Option states.** Text align, vertical align, routing and the arrowheads use one glyph per state, picked by the host from the value.
- **Accessibility.** Decorative by default (`aria-hidden="true" focusable="false"`). An optional `Label` swaps that for `role="img" aria-label`. There is no `<title>`, because it would add a native tooltip that clashes with the controls' `title`s. Guidance in the XML doc and the spec: name the control and leave its icon decorative.
- **Drawing rules.** 16 grid, stroke 1.5, round caps and joins, at least 1 unit of padding. Place coordinates so a 1.5 stroke lands on whole pixels at 16px. Text vertical align must not reuse the object-align glyphs, so the bar's "align text to top" and the menu's "align shapes to top" look different.
- **Count.** The target is 30 to 50 glyphs. The inventory's list comes to about 60 if every menu row takes an icon, so the trim happens where the map already has menu rows in fog.
- **Packaging.** Nothing to ship: no third-party notice and no licence header.

Comparison renders are in the session scratchpad and are not kept. The facts above are what they showed.
