# 20. Colour presets and the colour flyout

**What to build:** clicking a colour chip in the `Property bar` opens a flyout with a grid of preset swatches, then "Theme colour" where it applies, then "Custom…" for the native picker. The library ships ten defaults. A host replaces them through `D12CanvasOptions.ColourPresets`, for example with brand colours, and one list serves every colour role. A bad hex fails at startup. An empty list removes the grid.

See the spec's "Colour presets option" and "Property bar" sections.

**Blocked by:** 19 (Property bar value chips and choice flyout)

**Status:** resolved

- [x] `D12CanvasOptions.ColourPresets` is a list of `ColourPreset(string Name, string Hex)`, both required, set in `AddD12Canvas`
- [x] Configuring a preset whose `Hex` isn't `#RRGGBB` throws; setting the list to null throws
- [x] The default list is, in order: Black `#000000`, Grey `#868E96`, White `#FFFFFF`, Red `#E03131`, Orange `#F08C00`, Yellow `#FFEB3B`, Green `#2F9E44`, Blue `#2F80ED`, Purple `#7048E8`, Pink `#D6336C`
- [x] Fill, Stroke, Text colour, Edge colour and any host colour property use the same list
- [x] The colour flyout shows the presets five per row in list order, wrapping; each swatch is a `menuitemradio` named by its `Name`, with a library tooltip of name and hex ("Blue #2F80ED")
- [x] A swatch is checked when the current value matches its hex, ignoring case; mixed checks none
- [x] "Theme colour" follows the grid when every target can hold `null`; "Custom…" follows and opens the native picker; neither has a glyph
- [x] An empty list removes the grid and leaves "Theme colour" (where it applies) and "Custom…"
- [x] The chip keeps showing the hex when the value matches a preset
- [x] The `PropertyPanel` keeps its native picker
- [x] bUnit: validation throws, the defaults, an empty list removes the grid, swatch names and checked state, "Theme colour" appears only when every target can hold `null`, picking a swatch commits one edit
- [x] New screenshot case: the colour flyout open with the preset grid, in light and dark
- [x] The full visual suite passes with baselines updated in this commit

## Comments

- `ColourPreset(string Name, string Hex)` lives in `D12Canvas.Registration`. `D12CanvasOptions.ColourPresets` starts as the ten defaults and validates on set: a null list throws `ArgumentNullException`, and a null entry, a blank `Name` or a `Hex` that isn't `#RRGGBB` throws `ArgumentException` naming the preset. These are not `ComponentRegistrationException`, because there is no component key to name. Hex casing is kept as configured.
- The list sits in an internal `ColourPresetCatalog` that `AddD12Canvas` registers as a singleton, so a second `AddD12Canvas` call edits the same list. `PropertyBar` resolves it through `IServiceProvider` and falls back to the defaults when it isn't registered, because many tests (and possibly hosts) register only `IComponentRegistry`.
- A colour chip is now a menu button like a choice chip. The native colour input stays in the chip's cell, hidden (opacity 0, no pointer events), and is named "Label: Custom colour". "Custom…" is a `menuitem`. Its click is handled in `PropertyBar.razor.js` while it is still a user gesture: the handler focuses the input and calls `showPicker()`, falling back to `click()`. .NET then closes the flyout. Focus stays on the input, so the held-pick commit on blur still works. "Theme colour" is a `menuitemradio`, checked when the value is themed.
- The flyout has the role's label as a heading, the same as the choice flyout. The spec doesn't ask for it, but the two flyouts now match.
- `flyout.js` gains a grid: an element with `data-d12-flyout-columns` makes Up and Down move by whole rows, and leaving the top or bottom row steps to the item before or after the grid. In a flyout with no column groups, Left and Right move to the previous or next item. Before, they did nothing there.
- The first run showed the edge's colour flyout clipped by the pane's right edge, because Colour is the edge bar's last chip. A chip in the bar's second half now opens its flyout from its own right edge (`d12-property-bar-flyout-end`). This applies to choice chips too, such as the edge's Target arrow and a sticky note's alignments. The routing flyout baseline is unchanged.
- Baselines: 30 existing cases move their HTML only, all inside the bar's colour cells (the chip button, the `d12-property-bar-picker` input and Blazor markers). Their PNGs are unchanged. The first run also showed two PropertyPanel PNGs off by at most 2 in a few pixels, which didn't repeat on the second run. The new case is the colour flyout open on Blue, light and dark (4 files). The `MultiSelectionMoveResize` `aria-selected` noise did not show up in either run.
- Probes added: the keyboard crosses the grid by rows, shows a swatch's tooltip on focus and picks with one undo step; hovering a swatch shows "Blue #2F80ED"; "Custom…" closes the flyout and focuses the picker input. Headless Chromium can't show the native picker, so no probe checks that it opens.
