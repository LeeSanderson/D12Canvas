# Colour preset option

Type: grilling
Status: resolved
Map: [Library icons, Palette and Property bar](../map.md)

## Question

How does a host replace the Property bar's colour presets?

[Property bar look](06-property-bar-look.md) gave the colour flyout a grid of preset swatches that the library ships and a host can replace. Decide:

- the option's name and where it lives (a canvas parameter, an options object, a registration setting)
- its type, and whether a preset carries a name for its accessible label or only a hex value
- whether presets are one list for every colour role or per role (fill, stroke, text, edge)
- the library's default list

## Answer

A host replaces the presets through `D12CanvasOptions.ColourPresets`, one list for the whole app, set inside `AddD12Canvas`. Hosts already configure the library there, so the presets don't add a second route. A parameter on the canvas would only help a host with two canvases that want different presets, and no host needs that.

- **Type.** Each entry is a `ColourPreset(string Name, string Hex)`, and both fields are required. `Hex` must be `#RRGGBB`. The library checks this when the list is configured and throws otherwise, as a registration with no `DisplayName` does. That is the only format the native picker behind "Custom…" accepts, and the chip shows values as upper-case hex anyway. `Name` is the swatch's accessible name, so a screen reader says "Blue", not "#2F80ED". The swatch's library tooltip shows both, for example "Blue #2F80ED".
- **One list.** Fill, Stroke, Text colour and Edge colour all use the same list, with no per-role override. A host's brand colours apply everywhere, and a consumer-declared colour property gets the same presets without needing a role.
- **Default list.** Ten swatches in this order: Black `#000000`, Grey `#868E96`, White `#FFFFFF`, Red `#E03131`, Orange `#F08C00`, Yellow `#FFEB3B`, Green `#2F9E44`, Blue `#2F80ED`, Purple `#7048E8`, Pink `#D6336C`. Yellow matches the sticky note's fill and Blue matches the `--d12-accent` token. Presets are fixed hex values and don't change with the theme. "Theme colour" covers that case.
- **Empty and null.** An empty list removes the grid, leaving "Theme colour" (where it applies) and "Custom…". That's how a host switches presets off, with no separate flag. The property starts as the default list, and setting it to null throws.
- **Grid.** The grid shows presets in list order, five per row, wrapping onto more rows. A swatch is checked when the current value matches its hex, ignoring case. A mixed value checks nothing.
- **Chip text.** The chip keeps showing the hex even when the value matches a preset, as [Property bar look](06-property-bar-look.md) decided. The name appears only on the swatch.
- **Panel.** The presets appear in the `Property bar` only. The `PropertyPanel` keeps its native picker, because a swatch grid there would be the custom colour field the map rules out of scope.
