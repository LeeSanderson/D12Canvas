# The consumer icon contract

Type: grilling
Status: resolved
Blocked by: 03
Map: [Library icons, Palette and Property bar](../map.md)

## Question

How do consumers supply and use icons?

Decide:

- what `ComponentRegistrationBuilder.Icon` (now `string?`) accepts: a library icon, the consumer's own SVG, a `RenderFragment`, a string, or a mix
- what happens to existing string and emoji icons, and whether this is a breaking change
- whether consumers can render the library's icons in their own components and custom editors
- whether a consumer's editable property can opt into an icon control, such as an icon-choice `EditorKind`, with its own icons per option

## Comments

From [Icon inventory](01-icon-inventory.md): `registration.Icon` renders in two places, the Palette and the zoomed-out LOD placeholder in `DiagramCanvas.razor`. Whatever `Icon` accepts has to render in both.

## Answer

Consumers supply icons as `Glyph`s and can use the library's own glyphs anywhere.

- **Icon type.** `ComponentRegistrationBuilder.Icon` and `ComponentRegistration.Icon` change from `string?` to `Glyph?`. Strings go away, with no emoji fallback, so OS-dependent glyphs can't come back through registrations. A consumer who needs their own drawing builds a `Glyph` from path data. This is a breaking change, and acceptable because the library is not published. The built-ins move to `Glyphs.Rectangle`, `Glyphs.StickyNote`, `Glyphs.Text` and `Glyphs.Image`. The two Demo types in `test/D12Canvas.Demo/Program.cs` and the `D12CanvasOptionsTests` case move to glyphs too.
- **No icon.** `Icon` stays optional. When it is null, the Palette and the LOD placeholder in `DiagramCanvas.razor` both draw `Glyphs.Component`, a plain rounded square with a small corner mark. Neither site needs a branch for a missing icon.
- **Library glyphs for consumers.** The `Glyphs` catalogue is public along with `D12Icon`. Consumers can render `<D12Icon Glyph="Glyphs.AlignLeft" />` in their own components and custom editors, and reuse `Glyphs.Fill` with `--d12-icon-accent` in a colour editor. Glyph names are public API from now on, so renaming or removing one is a breaking change.
- **Icon option controls.** `EditableProperty` gets an optional `OptionGlyphs` (`IReadOnlyDictionary<string, Glyph>?`), keyed by option value. It is set only through the builder's `EditableProperties` override, because `[PanelEditable]` can't carry a `Glyph`. When every option of a Dropdown has a glyph, the `PropertyPanel` draws icon buttons in place of a `<select>`. With no glyphs, it stays a dropdown. The built-in Text align, Vertical align, routing and arrowhead properties use the same route, so the library uses no private shortcut. There is no new `EditorKind`.
- **Bar glyphs.** The Property bar reads `OptionGlyphs` from the property that declares the role (the representative in `PropertyBarRows`), the same data the panel uses. The library has no glyph table per role. A value with no glyph shows its option text in the bar cell. The edge rows carry `OptionGlyphs` on their `EdgeRole` entries in the same way.
