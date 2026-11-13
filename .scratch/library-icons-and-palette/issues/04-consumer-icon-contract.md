# The consumer icon contract

Type: grilling
Status: open
Blocked by: 03
Map: [Library icons, Palette and Property bar](../map.md)

## Question

How do consumers supply and use icons?

Decide:

- what `ComponentRegistrationBuilder.Icon` (now `string?`) accepts: a library icon, the consumer's own SVG, a `RenderFragment`, a string, or a mix
- what happens to existing string and emoji icons, and whether this is a breaking change
- whether consumers can render the library's icons in their own components and custom editors
- whether a consumer's editable property can opt into an icon control, such as an icon-choice `EditorKind`, with its own icons per option
