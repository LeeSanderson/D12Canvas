# 17. OptionGlyphs and PropertyPanel tiles

**What to build:** a Dropdown property can give each option a glyph, and the `PropertyPanel` then draws it as a grid of labelled tiles instead of a `<select>`. The built-in text align, vertical align and font weight properties use this, and a host property gets the same tiles by setting `OptionGlyphs`. A user sees every option at once, reads the current one from the pressed tile, sees an italic "Mixed" tag when the selection disagrees, and can't change tiles on a fully locked selection. Keyboard users tab once into the group, move with the arrow keys and commit with Space or Enter, so browsing doesn't add undo entries.

See the spec's "Consumer icon contract" and "PropertyPanel" sections.

**Blocked by:** 13 (Glyph set, D12Icon and the glyph gallery)

**Status:** resolved

- [x] `EditableProperty` has an optional `OptionGlyphs` (`IReadOnlyDictionary<string, Glyph>?`) keyed by option value, set through the builder's `EditableProperties` override; there is no new `EditorKind`
- [x] The built-in text align, vertical align and font weight properties, and the edge routing and arrowhead `EdgeRole` entries, declare `OptionGlyphs` through the same route as a host property
- [x] A Dropdown whose every option has a glyph draws tiles; any other Dropdown stays a `<select>` (Image `Fit` stays one)
- [x] A tile is its glyph above its name in 10px text; tiles wrap in a grid whose minimum column width comes from the longest name; a name wider than half the panel ends in an ellipsis; no tooltip
- [x] The current option's tile is pressed; mixed presses none and tags the label with an italic "Mixed"; picking a tile sets every target
- [x] A fully locked selection disables every tile, keeps the pressed tile drawn as pressed, and takes the tiles out of the tab order
- [x] The tiles are a `radiogroup` labelled by the field's label through `aria-labelledby`; each tile is a `radio` with `aria-checked`, named by its visible name
- [x] Tab lands once on the group, on the checked tile or the first tile when mixed; arrow keys move focus and wrap without committing; Space or Enter commits one edit and one history entry
- [x] The Demo's custom component type has a Dropdown property without `OptionGlyphs`, and the plain Dropdown baseline moves to it
- [x] bUnit: roles and `aria-checked`, mixed and locked states, Space or Enter commits once, arrows move the roving `tabindex` without committing, a host property with `OptionGlyphs` gets tiles and one without stays a `<select>`
- [x] Probe: arrow keys move real focus across the tiles, including onto a wrapped second row
- [x] New screenshot cases: tiles with a single value and tiles with a mixed value, each in light and dark
- [x] The full visual suite passes with PropertyPanel baselines updated in this commit

## Comments

- The built-ins declare `OptionGlyphs` by setting `EditableProperties` to `DiscoverFrom` with glyphs added per property name, so they go through the same builder route as a host. The edge `EdgeRole` entries carry `OptionGlyphs` too, but nothing reads them until ticket 19.
- Tiles show each option with a leading capital ("Center", "Bold") through an internal `OptionName.For`, which the bar's chips can reuse.
- The tiles are a `radiogroup` labelled by a span, so the italic "Mixed" tag sits beside the label but stays out of the group's name.
- `PropertyPanel` now carries `D12IconStyles.Rules` and cascades `InScope`, so its icons emit no style block of their own.
- `PropertyPanel.razor.js` gains `registerTileKeys`. It cancels the default action for the arrows, Home, End, Enter and Space on a tile, so the page doesn't scroll and a key commit isn't followed by a click.
- The column minimum is `max(52px, longest name × 1.2ch + 12px)`, capped at half the grid so two columns still fit. The `ch` estimate can be off for wide capitals.
- The Demo's custom `DemoShapeProps` gains an `Outline` Dropdown (solid, dashed, dotted) with no glyphs. The plain Dropdown baselines now select Process.
- The probe uses a sticky note, not a text. A newly placed empty text is removed when its editor loses focus to the panel.
- Baselines:
  - The plain Dropdown pair moves its PNG and HTML.
  - `TwoTextsShowTheMixedOptionInTheWeightDropdown` is replaced by `TwoTextsShowMixedWeightTiles` with a dark twin.
  - `PopulatedPanelWithGlyphTiles` is new, light and dark.
  - 9 more cases with a panel move their HTML only, by the `<!--!-->` marker of the new cascade.
  - The two `MultiSelectionMoveResizeVisualTests` HTML baselines also fold in an `aria-selected` attribute. They fail the same way on HEAD, and their PNGs are unchanged.
