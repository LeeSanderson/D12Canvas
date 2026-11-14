# Palette look

Type: prototype
Status: open
Blocked by: 02
Map: [Library icons, Palette and Property bar](../map.md)

## Question

What should an icon-first `Palette` look like?

Build a throwaway Demo route that compares layouts, for example an icon grid with tooltips and tiles with a small caption under the icon. Use icons from the chosen source. Settle:

- the layout, and how it behaves with many registered types
- where the name goes, both on screen and as the accessible name
- where the Edge entry sits and which icon it uses
- how categories show

## Comments

From [The consumer icon contract](04-consumer-icon-contract.md): every entry has an icon. `Icon` is a `Glyph?`, and a null icon draws `Glyphs.Component`, so the layout never has to handle a missing icon.
