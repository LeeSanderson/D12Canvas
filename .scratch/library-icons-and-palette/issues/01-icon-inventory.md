# Icon inventory

Type: task
Status: open
Map: [Library icons, Palette and Property bar](../map.md)

## Question

Where does the library draw an icon today, or need one after this work, and what does each place need?

List every site with its file, what it draws now (emoji, Unicode, hand-built SVG, nothing), its rendered size, whether it carries meaning or is decoration, and what the new icon has to show. Cover at least:

- the built-in palette entries in `BuiltIns/BuiltInComponents.cs` and the fixed Edge entry in `Palette.razor`
- every `PropertyRole` glyph in `PropertyBar.razor`
- the `PropertyPanel` controls that could become icon controls (text align, vertical align, font weight, stroke width, edge routing, edge arrows)
- the context-menu glyphs in `ContextMenu.razor.cs` and any menu rows that could take an icon
- empty and placeholder states (the image placeholder, "No selection")

The output is a flat list of the glyphs needed, which feeds the coverage check in Icon source and style.
