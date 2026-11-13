# Icon inventory

Type: task
Status: resolved
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

## Answer

Six sites draw an icon today. Two of them weren't on the list: the zoomed-out LOD placeholder on the canvas, and the toggle check mark in the context menu. The `PropertyPanel` draws no icons and edits no edges, so routing and arrow controls exist only in the Property bar.

### Sites

| Site | File | Draws now | Size | Meaning |
|---|---|---|---|---|
| Palette entries (4 built-ins) | `BuiltIns/BuiltInComponents.cs` (`builder.Icon`), rendered in `Palette.razor` | Emoji/Unicode strings: `▭` `🗒️` `🔤` `🖼️` | 16px font-size | Decorative (`aria-hidden`); the button's `aria-label` carries the name |
| Palette Connector entry | `Palette.razor` | Nothing, text only | n/a | Needs one, decorative |
| LOD placeholder | `DiagramCanvas.razor` (`lod-placeholder-icon`) | The same `registration.Icon` string | 16px font-size | Decorative; the whole box is `aria-hidden` |
| Property bar, 12 roles | `PropertyBar.razor` + `TextAlignPath`/`VerticalAlignPath`/`RoutingPath` in `PropertyBar.razor.cs` | Hand-built SVG, 16 grid, mixed stroke widths (1 to 3.2) | 16px in a 26px cell | Meaningful: it is the only visible label; the name lives in `title` and the overlay control's `aria-label` |
| Context menu align/distribute strip | `ContextMenu.razor.cs` `GlyphMarkup` | Hand-built SVG, 16 grid, lines plus filled rects | 16px in a 26x28 button | Meaningful: icon-only buttons, named by `aria-label` |
| Context menu check mark | `ContextMenu.razor` | Unicode `✓` | 13px text in a 12px column | Decorative; `aria-checked` carries state |
| Image placeholder | `BuiltIns/Image.razor` | Emoji `🖼️` above "No image" / "Image unavailable" | 12px inherited | Decorative (`aria-hidden`) |
| PropertyPanel "No selection" | `PropertyPanel.razor` | Text only | n/a | Could take a decorative icon; nothing needs one |

Four Property bar glyphs aren't plain icons. Fill, Stroke, Text colour and Edge colour paint the current value into the glyph and switch to a hatch pattern (mixed) or a dashed outline (theme colour). Text align, vertical align, routing and both arrows redraw to show the current option. So the bar needs value-driven icons, not one icon per role.

Menu rows that could take an icon, all text-only today (`ContextMenuComposition.cs`): Cut, Copy, Paste, Duplicate, Delete, Select All, Group, Ungroup, Bring to Front, Bring Forward, Send Backward, Send to Back, Lock, Unlock, Unlock All, Zoom to Selection, Zoom to Fit, Zoom to 100%, Snap to Grid, Object Snapping, Add port here, Add port…, Remove port, Choose image…, Remove image. The Image editor's "Choose file…" and "Remove image" buttons (`ImagePictureEditor.razor`) are the same two actions.

### Flat glyph list

Components and placeholders:
- rectangle, sticky note, text, image, connector (Palette, LOD placeholder)
- image placeholder: the image glyph again, and possibly a broken-image variant for "Image unavailable"

Property bar, current value shown:
- fill swatch, stroke swatch, text colour (letter over a swatch bar), edge colour swatch. These need a colour, hatch and dashed state.
- stroke width
- font size
- font weight (bold)
- text align left, centre, right
- vertical align top, middle, bottom
- routing straight, orthogonal, curved
- source arrow none/arrow, target arrow none/arrow (four states, or a line plus a direction-aware head)

Context menu strip:
- align left, centre, right, top, middle, bottom
- distribute horizontally, vertically

Context menu and editor rows, if they take icons:
- check (toggle mark)
- cut, copy, paste, duplicate, delete
- select all
- group, ungroup
- bring to front, bring forward, send backward, send to back
- lock, unlock (Unlock All reuses unlock)
- zoom to selection, zoom to fit, zoom to 100%
- snap to grid, object snapping
- add port, remove port
- choose image, remove image

That is 5 component glyphs plus about 25 bar states, 8 align/distribute glyphs and about 22 row glyphs. The bar and strip glyphs match the provisional list in Icon source and style. The new ones are the check mark, select all, zoom to selection and 100%, snap to grid, object snapping, add/remove port and choose/remove image.
