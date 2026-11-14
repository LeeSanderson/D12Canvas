# Palette look

Type: prototype
Status: resolved
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

## Answer

The `Palette` becomes a vertical rail of icon-only buttons. I prototyped four layouts (icon grid, captioned tiles, vertical rail, top toolbar with category flyouts) next to today's Palette, in light and dark, with the real registrations and with 22 fake entries. The rail won, with two changes from the first draft: the tooltip moved off the icon, and a "more" button took over from scrolling.

- **Layout.** One column of 36px square buttons with 18px icons, on the Palette's raised surface. The host still places it (Host-placed chrome); the App keeps its top-left overlay.
- **Order.** Edge first, then a divider, then registered types. Built-ins come before host registrations, which reverses today's order, where `AddD12Canvas` appends the built-ins after the host's own types.
- **Many types.** The rail shows the first 8 registered types, a fixed number with no parameter. Edge doesn't count towards the 8. Anything past 8 goes behind a "…" button at the bottom, after a divider. With 8 or fewer types the button isn't drawn.
- **The "…" flyout.** Opens to the right of the rail, bottom-aligned with the button. It holds only the types not on the rail, one column per category, each with a small uppercase category heading and icon-plus-name rows. Picking a row adds the type and closes the flyout, and so does Esc. Rows drag like rail buttons.
- **Categories.** On the rail, a divider between categories and no heading. In the flyout, a heading per column. Uncategorised types keep the `Uncategorized` group.
- **Name.** Each button's accessible name is the registration's `AccessibleName`, and the icon stays decorative. The visible name is a library tooltip showing `DisplayName`, placed 12px to the right of the rail and centred on the button's height. It shows on hover and on keyboard focus, and doesn't use the native `title`. The "…" button's tooltip gives the count, for example "More components (14)", and its accessible name is "More components" with `aria-expanded`.
- **Edge.** Named "Edge" in its tooltip and accessible name, which replaces "Connector" and matches `CONTEXT.md`. Its icon is a diagonal arrow with a dot at the start, as a new `Glyphs.Edge`. The prototype drew a rough version, and the final drawing follows the drawing rules.
- **Roles.** The rail is a vertical `toolbar` named "Component palette", and the flyout is a `menu`. Keyboard movement inside the toolbar isn't prototyped. The spec follows the WAI-ARIA toolbar pattern unless a later ticket decides otherwise.

The prototype, all five variants, is on the throwaway branch `prototype/palette-look` (`/prototype/palette-look` in the Demo, with `?variant=C&many=1`). Do not merge it.
