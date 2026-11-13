# Icon source and style

Research for [02 Icon source and style](../issues/02-icon-source-and-style.md). Checked on 2026-11-13 against these package versions: lucide-static 1.72.0, @tabler/icons 3.51.0, @phosphor-icons/core 2.1.1, heroicons 2.2.0, bootstrap-icons 1.13.2.

## Recommendation

Copy path data from **Lucide**, pinned to one version, and draw the two or three glyphs it lacks ourselves on Lucide's grid and stroke rules. Lucide and Tabler tie on coverage of the glyph list, well ahead of the rest. Lucide wins the tie for three reasons. It has group, ungroup and all four z-order glyphs as one family, where Tabler has no ungroup. Its design guide makes pixel-grid alignment a rule, and its path data follows it. Tabler's `copy`, for example, puts corners at 9.667 and radii at 2.667, where Lucide's `copy` sits on whole units. And it is stroke-based like the library's current hand-drawn glyphs. Its licence asks only for a notice file in the package.

If Lucide falls short in a later ticket, Tabler is the fallback. Tabler uses the same 24-unit grid, 2-unit stroke and round caps and joins, so its glyphs sit next to Lucide's without looking foreign. Phosphor, Heroicons and Bootstrap Icons don't mix with Lucide.

## How I checked

Name lists came from each project's published package, not from search results:

- Lucide: `tags.json` in `lucide-static` on unpkg, 2,052 icons.
- Tabler: `icons.json` in `@tabler/icons`, 5,233 outline icons, 1,054 of them also filled.
- Phosphor: the `assets/regular/` folder of `@phosphor-icons/core`, 1,512 icons per weight.
- Heroicons: the `optimized/24/outline` folder of `tailwindlabs/heroicons` on GitHub, 324 icons.
- Bootstrap Icons: `font/bootstrap-icons.json` in `bootstrap-icons`, 2,078 icons.

I fetched sample SVGs from each package to read the viewBox, stroke and fill attributes. Where a name was unclear I read the icon's tags or its path data. I did not render anything, so the 16px notes below come from the geometry, not from screenshots.

## Licence and attribution

| Set | Licence | Copyright line | What the NuGet package must carry |
|---|---|---|---|
| Lucide | ISC, plus MIT for icons derived from Feather | "Copyright (c) 2026 Lucide Icons and Contributors"; Feather part "Copyright (c) 2013-present Cole Bemis" | Both notices, in full, because the ISC and MIT texts require the copyright and permission notice "in all copies". |
| Tabler | MIT | "Copyright (c) 2020-2026 Paweł Kuna" | The MIT notice. |
| Phosphor | MIT | "Copyright (c) 2023 Phosphor Icons" | The MIT notice. |
| Heroicons | MIT | "Copyright (c) Tailwind Labs, Inc." | The MIT notice. |
| Bootstrap Icons | MIT | "Copyright (c) 2019-2024 The Bootstrap Authors" | The MIT notice. |

Sources: [Lucide LICENSE](https://github.com/lucide-icons/lucide/blob/main/LICENSE), [Tabler LICENSE](https://github.com/tabler/tabler-icons/blob/main/LICENSE), [Phosphor core LICENSE](https://github.com/phosphor-icons/core/blob/main/LICENSE), [Heroicons LICENSE](https://github.com/tailwindlabs/heroicons/blob/master/LICENSE), [Bootstrap Icons LICENSE](https://github.com/twbs/icons/blob/main/LICENSE).

All five allow copying path data into a commercial or open package. None asks for a credit in the UI. The duty is the same shape for each: ship the licence text with the copied data.

Lucide's LICENSE has two parts. The ISC part covers the set. A second MIT part covers a named list of icons derived from Feather, and that list includes several we would use: `square`, `type`, `trash`, `lock`, `clipboard`, `minus`, `maximize`, `zoom-in`, `zoom-out`, `corner-down-right`. The simplest way to comply is to ship Lucide's LICENSE file unchanged.

For the NuGet package that means:

1. A `THIRD-PARTY-NOTICES.txt` at the package root, packed through the csproj, holding Lucide's LICENSE file verbatim and naming the Lucide version the paths came from.
2. A licence header at the top of the C# file that holds the path data, so the notice travels with the source as well as the package. This is a legal notice, not an explanatory comment, so it doesn't clash with the repo's minimal-comments rule.

`lib/D12Canvas/D12Canvas.csproj` has no licence or notice entries today, and the repo has no LICENSE file of its own. The implementation ticket should add the notice file whichever set wins.

## Grid, style and 16px

| Set | Grid | Default render size | Drawing style | How it takes `currentColor` |
|---|---|---|---|---|
| Lucide | 24 x 24 | 24 | Stroke 2, round caps and joins, `fill="none"` | `stroke="currentColor"` on the root `<svg>` |
| Tabler outline | 24 x 24 | 24 | Stroke 2, round caps and joins, `fill="none"` | `stroke="currentColor"` on the root; a few icons such as `stack-front` also set `fill="currentColor"` on one path |
| Tabler filled | 24 x 24 | 24 | Fill | `fill="currentColor"` |
| Phosphor | 256 x 256 | none set | Fill only. Six weights: thin, light, regular, bold, fill, duotone. Each weight is pre-outlined, so "regular" is a filled shape that looks like a stroke. | `fill="currentColor"` on the root; duotone adds a second path at `opacity="0.2"` |
| Heroicons | 24 outline and solid, 20 solid, 16 solid | matches grid | Outline is stroke 1.5; 20 and 16 are fill only | `stroke="currentColor"` or `fill="currentColor"` on the root |
| Bootstrap Icons | 16 x 16 | 16 | Fill, using 0.5-unit detail | `fill="currentColor"` on the root |

Sources: [Lucide icon design guide](https://lucide.dev/contribute/icon-design-guide), [Tabler README](https://github.com/tabler/tabler-icons), [Phosphor core README](https://github.com/phosphor-icons/core), [Heroicons README](https://github.com/tailwindlabs/heroicons), sample SVGs from each package.

Lucide's design guide sets a 24-unit canvas, a 2-unit stroke, at least 1 unit of padding, at least 2 units between distinct elements, round joins and caps, and says to "align coordinates, arc centers, and other geometric elements to the pixel grid when possible" and avoid off-grid diagonals. Tabler's README says each icon is "designed on a 24x24 grid with a 2px stroke" and that paths are drawn to render well at other stroke widths. Its webfont builds ship 2, 1.5 and 1 widths.

At 16px every 24-unit set scales by two thirds. A 2-unit stroke becomes 1.33px, and coordinates that sat on whole pixels at 24 land on thirds of a pixel. On a 2x display that is invisible. On a 1x display the lines antialias across two device pixels and look a little soft. Setting `stroke-width` to 1.5 gives a 1px line at 16px, which reads sharper, though the positions still don't snap. The library's current hand-drawn glyphs use a 16-unit viewBox at stroke 1.5 and avoid this. Only Bootstrap Icons and Heroicons' 16 solid set are drawn for 16px, and neither is stroke-based.

Phosphor's regular weight has a 16-unit stroke on a 256 grid, which is exactly 1px at 16px, but its shapes are fills, so you can't change the weight with CSS. You switch to the thin, light or bold path data instead.

Stroke-based sets suit this library better. The property bar and context menu already draw strokes in `currentColor`. A consumer can thin or thicken every icon with one `stroke-width` rule. A theme switch needs nothing more than a new `color`. Fill sets theme just as well through `currentColor`, but you can't re-weight them.

## Coverage

`~` marks a stand-in: an icon that would work but was drawn for something else. `gap` means nothing usable. Names are exact file names from the packages above.

| Glyph | Lucide | Tabler | Phosphor | Heroicons | Bootstrap Icons |
|---|---|---|---|---|---|
| Rectangle | `square`, `rectangle-horizontal` | `square`, `rectangle` | `square`, `rectangle` | gap | `square` |
| Sticky note | `sticky-note` | `note` | `note`, `note-blank` | gap | `sticky` |
| Text | `type` | `typography`, `letter-t` | `text-t` | gap | `type`, `fonts` |
| Image | `image` | `photo` | `image` | `photo` | `image` |
| Connector / edge | `spline`, `waypoints` | `vector-spline`, `line` | `line-segment`, `bezier-curve` | ~`arrow-long-right` | `bezier2`, ~`diagram-2` |
| Fill | `paint-bucket` | `bucket-droplet` | `paint-bucket` | ~`swatch` | `paint-bucket` |
| Stroke | ~`pen-line`, ~`paintbrush` | ~`brush`, ~`border-style-2` | ~`paint-brush`, ~`pen` | ~`paint-brush` | ~`brush` |
| Stroke width | gap | gap | gap | gap | `border-width` |
| Text colour | `baseline` | `text-color` | `text-a-underline` | gap | gap |
| Font size | `a-large-small`, `a-arrow-up`, `a-arrow-down` | `text-size`, `text-increase`, `text-decrease` | `text-aa` | gap | gap |
| Bold | `letter-b` | `bold` | `text-b` | `bold` | `type-bold` |
| Text align left / centre / right | `text-align-start`, `text-align-center`, `text-align-end` | `align-left`, `align-center`, `align-right` | `text-align-left`, `text-align-center`, `text-align-right` | gap | `text-left`, `text-center`, `text-right` |
| Vertical align top / middle / bottom | ~`align-start-horizontal`, ~`align-center-horizontal`, ~`align-end-horizontal` | `align-box-top-center`, `align-box-center-middle`, `align-box-bottom-center` | ~`align-top`, ~`align-center-vertical`, ~`align-bottom` | gap | `align-top`, `align-middle`, `align-bottom` |
| Routing: straight | ~`minus`, ~`slash` | `stroke-straight`, `line` | `line-segment` | ~`slash` | ~`slash` |
| Routing: orthogonal | ~`corner-down-right` | ~`arrow-elbow-right` | ~`arrow-elbow-right` | ~`arrow-turn-down-right` | ~`arrow-90deg-right` |
| Routing: curved | `spline` | `stroke-curved`, `vector-spline` | `bezier-curve` | gap | `bezier2` |
| Arrowheads | `move-right`, `arrow-right` | `arrow-narrow-right`, `arrow-right-tail` | `arrow-right` | `arrow-long-right` | `arrow-right` |
| Edge colour | same as stroke | same as stroke | same as stroke | same as stroke | same as stroke |
| Align left / centre / right | `align-start-vertical`, `align-center-vertical`, `align-end-vertical` | `layout-align-left`, `layout-align-center`, `layout-align-right` | `align-left`, `align-center-horizontal`, `align-right` | gap | `align-start`, `align-center`, `align-end` |
| Align top / middle / bottom | `align-start-horizontal`, `align-center-horizontal`, `align-end-horizontal` | `layout-align-top`, `layout-align-middle`, `layout-align-bottom` | `align-top`, `align-center-vertical`, `align-bottom` | gap | `align-top`, `align-middle`, `align-bottom` |
| Distribute horizontally / vertically | `align-horizontal-distribute-center`, `align-vertical-distribute-center` | `layout-distribute-horizontal`, `layout-distribute-vertical` | gap | gap | `distribute-horizontal`, `distribute-vertical` |
| Lock / unlock | `lock`, `lock-open` | `lock`, `lock-open` | `lock`, `lock-open` | `lock-closed`, `lock-open` | `lock`, `unlock` |
| Group / ungroup | `group`, `ungroup` | ~`box-multiple` / gap | ~`selection-plus` / gap | ~`rectangle-group` / gap | gap |
| Bring forward / to front | `layers-arrow-up`, `bring-to-front` | `stack-forward`, `stack-front` | gap | gap | `layer-forward`, `front` |
| Send backward / to back | `layers-arrow-down`, `send-to-back` | `stack-backward`, `stack-back` | gap | gap | `layer-backward`, `back` |
| Copy | `copy` | `copy` | `copy` | ~`square-2-stack` | `copy` |
| Cut | `scissors` | `cut`, `scissors` | `scissors` | `scissors` | `scissors` |
| Paste | `clipboard-paste` | `clipboard` | `clipboard` | `clipboard` | `clipboard` |
| Duplicate | `copy-plus` | `duplicate` | ~`copy-simple` | `document-duplicate` | `files` |
| Delete | `trash` | `trash` | `trash` | `trash` | `trash` |
| Zoom to fit | `scan`, `maximize`, `expand` | `zoom-scan`, `arrows-maximize`, `focus-centered` | `frame-corners`, `arrows-out` | `arrows-pointing-out` | `arrows-fullscreen`, `fullscreen` |

Counting the 30 rows, excluding edge colour, which reuses stroke:

- Tabler: 26 direct, 3 stand-ins, 1 gap. The group row counts as a stand-in, but Tabler has nothing for ungroup.
- Lucide: 25 direct, 4 stand-ins, 1 gap.
- Bootstrap Icons: 24 direct, 3 stand-ins, 3 gaps.
- Phosphor: 21 direct, 5 stand-ins, 4 gaps.
- Heroicons: 9 direct, 7 stand-ins, 14 gaps.

Lucide and Tabler are level on coverage. Their misses differ in kind. Tabler's are group and ungroup, two core diagram commands that sit in the context menu next to the z-order glyphs Tabler does have. Lucide's are text vertical align and a straight-line glyph, and `minus` or `slash` already work for straight routing.

Notes on the rows that matter:

- Lucide's naming changed at 1.0. `bold` is now `letter-b`, which keeps "bold" as a tag, and `align-left`/`align-center`/`align-right` are now `text-align-start`/`text-align-center`/`text-align-end`. I diffed 0.460.0 against 1.72.0 to find these. Copying path data at a pinned version shields the library from later renames.
- Lucide's object-align icons are named by the axis of the bars, not the edge. `align-start-vertical` is align left and `align-start-horizontal` is align top; the tags say so ("left edge", "top edge"), and the path data of `align-center-vertical` draws a vertical line at x = 12. Phosphor uses the opposite axis word: `align-center-horizontal` is a vertical line at x = 128, so it means centre-left-to-right.
- Lucide has `layer-arrow-up` and `layers-arrow-up`. The tags put "bring forward" on `layers-arrow-up` and "move forward ... reorder" on `layer-arrow-up`. Either would do; the implementer should pick the one that reads better next to `bring-to-front`.
- Lucide has no text vertical-align glyphs. The object-align top/middle/bottom glyphs are the obvious reuse, but then the property bar shows the same icon for "align the text in this shape to the top" and the context menu's "align these shapes to the top". Tabler's `align-box-*` icons draw a box with content inside it, which separates the two meanings. This is the one row where Tabler beats Lucide.
- No set has an orthogonal-connector glyph. All five offer a bent arrow, which reads as "turn" or "reply" rather than "route with right angles". The current property bar draws its own (`M2.5 12.5 H8 V3.5 H13.5`), and I'd keep drawing that one.
- Only Bootstrap has a stroke-width glyph. The usual picture is three horizontal lines of increasing thickness, which is easy to draw on Lucide's grid.

## Do the sets mix?

Lucide and Tabler mix. Both use a 24 grid, a 2-unit stroke, round caps and joins, and `fill="none"` with `stroke="currentColor"`, so their SVG roots can be identical. Both put a plain square at 3 to 21. Tabler's multi-part icons sometimes use fractional coordinates and slightly larger radii, so a Tabler glyph can look a touch softer beside a Lucide one. At 16px that is hard to spot. Tabler outline also puts a `<path stroke="none" d="M0 0h24v24H0z" fill="none"/>` bounding box in every file, which should be stripped when copying.

Phosphor doesn't mix with either. Its glyphs are filled outlines on a 256 grid, so a stroke-width rule on the root won't touch them and they come out at a fixed weight next to re-weighted Lucide strokes.

Heroicons outline uses stroke 1.5 on a 24 grid, so it could sit beside Lucide set to 1.5. Its coverage is too thin to bother.

Bootstrap Icons are fills drawn for 16px. Beside Lucide strokes they look heavier and more detailed. They also tie the library's look to the App's, which the map keeps separate.

## Draw our own?

Drawing all ~45 glyphs ourselves would give pixel-snapped 16-unit art like the current context-menu and property-bar glyphs. It would also mean designing, reviewing and maintaining every glyph, and consumers would get a small one-off set instead of one they can extend from a public catalogue. Lucide's 2,000-plus icons let a consumer pick a matching icon for their own component types and get a consistent look for free. That is the deciding point given the map's goal that consumers can use the library's icons too.

The middle path is what I recommend: Lucide for everything it has, plus in-house glyphs for stroke width, orthogonal routing and, if the property bar needs it, text vertical align. Draw them on a 24 grid with stroke 2, round caps and joins, and 1 unit of padding, so they render through the same code path and pick up the same `stroke-width` override.

## Open points for later tickets

- Render size and stroke width at 16px. Lucide at stroke 2 gives 1.33px lines; stroke 1.5 gives 1px. The rendering ticket (03) or a prototype should screenshot both at 1x and 2x before fixing a default.
- The final glyph list from ticket 01 may add or drop rows. Rerun the coverage check against `tags.json` for any new glyph.
