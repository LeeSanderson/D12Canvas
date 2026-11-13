# How the library defines and renders an icon

Type: grilling
Status: open
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
