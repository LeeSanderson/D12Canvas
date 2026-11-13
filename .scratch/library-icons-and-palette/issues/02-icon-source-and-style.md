# Icon source and style

Type: research
Status: resolved
Map: [Library icons, Palette and Property bar](../map.md)

## Question

Which source should the library's icons come from: an openly licensed set copied in as SVG path data, or icons we draw ourselves?

Compare at least Lucide, Tabler, Phosphor and Heroicons on:

- licence, and what attribution a NuGet package that ships the path data must carry
- design grid and default size, and whether they stay crisp at 16px
- stroke vs fill, and how each themes through `currentColor`
- coverage of this provisional glyph list (Icon inventory gives the final one): rectangle, sticky note, text, image, connector/edge; fill, stroke, stroke width, text colour, font size, font weight/bold, text align left/centre/right, vertical align top/middle/bottom, edge routing (straight, orthogonal, curved), arrowheads, edge colour; align left/centre/right/top/middle/bottom, distribute horizontally/vertically; lock, unlock, group, ungroup, bring forward/to front, send backward/to back, copy, cut, paste, duplicate, delete, zoom to fit
- whether the sets mix well, in case one set lacks some glyphs

Recommend one source.

## Answer

Use Lucide (ISC, with an MIT part for Feather-derived icons), copying path data at a pinned version (checked at 1.72.0).

- Lucide and Tabler tie on coverage (25 and 26 of 30 glyphs direct). Lucide has group, ungroup and all four z-order glyphs; Tabler has no ungroup.
- Both are 24-grid, stroke 2, round caps and joins, `stroke="currentColor"`, so Tabler can fill a gap without looking foreign. Phosphor, Heroicons and Bootstrap are fill-based or mismatched and don't mix.
- Gaps to draw ourselves on Lucide's rules: stroke width, orthogonal routing, and text vertical align if it must differ from object align.
- The package must ship Lucide's LICENSE verbatim in a `THIRD-PARTY-NOTICES.txt`, plus a licence header on the path-data file.
- At 16px the 2-unit stroke is 1.33px; compare stroke 2 and 1.5 at 1x and 2x before fixing a default.

Full findings: [research/icon-source-and-style.md](../research/icon-source-and-style.md).

## Comments

Reversed by [How the library defines and renders an icon](03-icon-definition-and-rendering.md). The set needs only 30 to 50 glyphs and most are specific to D12Canvas, so the library draws all of them in-house on a 16 grid at stroke 1.5. No Lucide paths are copied in, and the package ships no third-party notice. The research file stays as a record of the comparison.
