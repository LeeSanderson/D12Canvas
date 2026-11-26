# Text inside shapes

Type: grilling
Status: resolved
Blocked by: 03, 07
Map: [Better standard components](../map.md)

## Question

How does a rectangle (and each shape kind) hold and show text?

`RectangleProps` has no text today.

Decide:

- a `Text` prop on the shape's props, edited inline with a double-press as a sticky note is
- text colour, font size, horizontal and vertical alignment props, and their defaults
- whether shape text shrinks to fit like a sticky note's or uses a fixed size
- the text area inside non-rectangular kinds (an ellipse or diamond inset), and whether text clips to it
- whether an empty shape behaves as today (no placeholder, no `IsEmpty` deletion)

## Comments

- From [Markdown subset](03-markdown-subset.md): shape text uses the Basic level: bold, italic, underline and line breaks.
- From [Inline editor markdown round trip](05-inline-editor-markdown-round-trip.md): shape text uses the same shared editor as every formatted text: a `contenteditable` container seeded with C#-rendered HTML and committed as markdown. The static render is a `MarkupString` from the C# HTML writer. Whatever inset and clipping a shape kind needs applies to that container in both states.
- From [Shape kinds](07-shape-kinds.md): the component is now `Shape` (key `shape`) with a `Kind` prop over eight kinds: rectangle, rounded rectangle, ellipse, diamond, hexagon, cylinder, parallelogram, trapezoid. Each is an inline SVG drawn from props, with fixed geometry taken from the bounds. This ticket picks each kind's text inset, for example an ellipse's or diamond's inscribed box, and the cylinder below its cap.

## Answer

**Text on every kind.** `ShapeProps` gains `Text`, a Basic-level markdown string with default `""`. All eight kinds have it, so switching kind keeps the text and still leaves nothing unused. Shape implements `IInlineEditable` and uses the shared inline editor: double-click puts the caret at the click point, Enter on a selected shape selects all, and Escape or blur commits.

**Style props.**

| Prop | Type | Default | Role |
|---|---|---|---|
| `TextColor` | `string?` | null: `--d12-board-text` through `--d12-board-text-override`, as Text does | `TextColour` |
| `FontSize` | `double?` | 14 | `AutoFontSize` |
| `TextAlign` | `left` / `center` / `right` | `center` | `TextAlign` |
| `VerticalAlign` | `top` / `middle` / `bottom` | `middle` | `VerticalAlign` |

The colour is themed, not a literal, because the fill follows the board theme and pinned black text would vanish on a dark board. There's no `FontWeight`. The whole-text B chip from Formatting UI covers bold.

**Font size.** It's pinned at 14 by default so every box in a flowchart uses the same size. Auto grows text to fill the box, which would make a one-word label about 50px in a 160×100 shape. Choosing Auto reuses `fitText.js` unchanged, measured against the text area. Pinned text that doesn't fit clips.

**Text area.** Each kind has a fixed inset rectangle, given as fractions of the box:

| Kind | Inset |
|---|---|
| Rectangle, rounded rectangle | none |
| Ellipse | 14.6% all sides (largest inscribed rectangle) |
| Diamond | 25% all sides (largest inscribed rectangle) |
| Hexagon, parallelogram, trapezoid | 20% left and right |
| Cylinder | 15% top (below the cap), 7.5% bottom (above the bottom curve) |

The area is absolutely positioned with `top/right/bottom/left` percentages. It doesn't use padding, because CSS percentage padding is measured against the width on both axes. It has `overflow: hidden`, `container-type: size` and `min(10px, 10cqmin)` padding inside, the same as a sticky note. Text clips to that rectangle, not to the curve. The static render and the editor share the box. Text doesn't wrap to the outline: `shape-outside` shapes one side of a float only and would fight the fit. The spec may tune the insets.

**Hit area.** The static text area has `pointer-events: none`, so presses reach the SVG and the body still hits only inside the outline. Without that, the text box would catch presses in a rounded rectangle's corners. Basic text has no links, so nothing in it needs presses. While editing, the editor takes pointer events as usual.

**Empty shapes.** No `IsEmpty` rule: clearing the text leaves the shape in place. No placeholder text, so an empty shape draws only its outline and existing baselines don't move. Placing from the Palette selects the shape and doesn't open the editor. Opening the editor on placement, as draw.io does, would change how every Shape is placed and isn't part of this ticket.
