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
- [Palette look](issues/05-palette-look.md) — a vertical rail of icon-only buttons: Edge first, then built-ins before host types, 8 on the rail and the rest behind a "…" flyout with one column per category; dividers stand in for category headings, names show in a tooltip to the right, and the entry is called "Edge"
- [Property bar look](issues/06-property-bar-look.md) — value chips: glyph, value text and caret, each opening a flyout of glyph-and-name option rows or colour presets that a host can replace; numbers stay glyph and field with "–" when mixed; a partly locked selection ends with a lock count, and a fully locked one keeps the bar as a lone Unlock button
- [Panel icon controls](issues/07-panel-icon-controls.md) — a glyphed Dropdown becomes labelled tiles (glyph above name) that wrap, with columns sized to the longest name and no tooltip or fallback; mixed presses nothing and tags the label; locked keeps today's rule; a `radiogroup` where arrows move focus and Space or Enter commits
- [Context menu glyphs](issues/08-context-menu-glyphs.md) — the strip moves onto `Glyphs`, every text row gets a leading glyph in `currentColor`, `✓` becomes `Glyphs.Check` in its own column beside the row glyph, and the Image editor's buttons reuse the matching row glyphs; the set grows to about 65, since 30 to 50 was an estimate, not a limit
- [Colour preset option](issues/09-colour-preset-option.md) — `D12CanvasOptions.ColourPresets`, one list of `ColourPreset(Name, Hex)` for every colour role, with `#RRGGBB` checked when the list is configured; ten defaults led by Black, Grey and White; an empty list removes the grid and null throws; Property bar only, the panel is unchanged
- [Panel partial-lock count](issues/10-panel-partial-lock-count.md) — yes: a partly locked selection shows the lock glyph and "1 of 3 locked; changes skip them" above the fields, in the Unlock button's slot; it's plain text with no action, no tooltip and no live region
- [Glyph drawing handoff](issues/11-glyph-drawing-handoff.md) — the spec lists 59 glyphs with the drawing rules and no path data; an implementation slice draws them from the prototype paths, and the user signs off a Demo glyph gallery that also gets a baseline and a padding-box unit test; extra demo shapes, a broken-image variant and glyphs on the Theme colour and Custom rows are left out
- [Verification and baselines](issues/12-verification-and-baselines.md) — the drawing slice goes first and moves no existing baseline, then each site slice moves its own baselines once in its commit; nine new screenshot cases in light and dark, the tooltip covered by a probe instead; bUnit owns roles, state and validation, probes own flyout focus, tile arrow focus and the Palette overflow; the plain Dropdown baseline moves to a property without `OptionGlyphs`

## Not yet specified


## Out of scope

- App icons and Bootstrap: the App keeps `IconPaths.cs`.
- Colour tokens and the colour palette.
- Palette search, filtering, collapsing categories and placement previews.
- A full layout redesign of the `PropertyPanel` (grouped sections, W/H pairs, a custom colour field).
- Edge fields in the `PropertyPanel`, so edge routing and arrowheads as panel controls: the panel edits component instances only, and adding edges is a new feature, not an icon change (from [Panel icon controls](issues/07-panel-icon-controls.md)).
