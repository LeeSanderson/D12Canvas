# Library icons, Palette and Property bar

Label: wayfinder:map

## Destination

A spec at `.scratch/library-icons-and-palette/spec.md`, ready for `/implement`. It covers a framework-free icon set inside the library, how consumers use and supply icons, an icon-first `Palette` with an icon for the Edge entry, and icon controls in the `Property bar` (the main focus) and the `PropertyPanel`.

## Notes

- Domain: the D12Canvas library (`lib/D12Canvas`). The App (`app/D12Canvas.App`) keeps its own Bootstrap icons and is not touched.
- The library must not depend on any CSS framework or icon font. Icons live in the library as SVG, drawn in-house; no third-party icon set is copied in.
- Consumers register their own component types and can write custom property editors. Every decision has to leave that extension route open, and ideally let consumers use the library's icons too.
- The complaint is mostly looks: emoji icons render differently per OS, the Palette's icon-plus-name rows feel old-fashioned, the Edge entry has no icon, and the Property bar's glyphs are hand-drawn.
- Colours go through the `--d12-*` token layer (ADRs 0012, 0063, 0064). A guard test rejects colour literals in style blocks, and style blocks carry no comments.
- Skills: `/grilling` and `/domain-modeling` for grilling tickets, `/prototype` for prototype tickets (throwaway Demo routes are exempt from the visual suite), `/research` for research tickets.

## Decisions so far

- [Icon source and style](issues/02-icon-source-and-style.md) — chose Lucide path data; later reversed by How the library defines and renders an icon, so nothing is copied in
- [Icon inventory](issues/01-icon-inventory.md) — six sites draw icons today, including the LOD placeholder and the menu check mark; the bar's colour and option glyphs show the current value, the panel has no edge fields, and about 60 glyphs are listed
- [How the library defines and renders an icon](issues/03-icon-definition-and-rendering.md) — an in-house `Glyph` record of path parts (stroke, fill, accent) on a 16 grid at stroke 1.5, drawn by a public `D12Icon` component; size and stroke overridable through `--d12-icon-size` and `--d12-icon-stroke`, accent colour through `--d12-icon-accent`, decorative unless given a `Label`; 30 to 50 glyphs
- [The consumer icon contract](issues/04-consumer-icon-contract.md) — `Icon` becomes `Glyph?` with no string fallback, and null draws `Glyphs.Component`; `Glyphs` is public; an optional `OptionGlyphs` on `EditableProperty` turns a Dropdown into icon buttons for built-in and consumer properties alike, and the bar reads its glyphs from the same place

## Not yet specified

- Whether the context-menu align/distribute glyphs (`ContextMenu.razor.cs`) move onto the shared set or stay hand-drawn, and whether the text menu rows and the `✓` toggle mark take icons too. This trim is what keeps the glyph set within 30 to 50.
- Who draws the glyphs and when: whether the Palette and Property bar prototypes produce final drawings, or the spec lists the glyphs and the drawing rules and leaves the drawing to implementation.
- How the visual-test baselines move and how the work is verified (palette, bar and panel baselines in light and dark).
- Whether the Edge entry becomes registration-like instead of a fixed entry after every category.

## Out of scope

- App icons and Bootstrap: the App keeps `IconPaths.cs`.
- Colour tokens and the colour palette.
- Palette search, filtering, collapsing categories and placement previews.
- A full layout redesign of the `PropertyPanel` (grouped sections, W/H pairs, a custom colour field).
