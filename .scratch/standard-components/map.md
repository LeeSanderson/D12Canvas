# Better standard components

Label: wayfinder:map

## Destination

A spec at `.scratch/standard-components/spec.md`, ready for `/implement`. It covers smaller sticky notes with shrink-to-fit text, markdown-backed formatted text in Text, sticky notes and rectangles, shape kinds for Rectangle, connector styling (width, dash, arrowhead size, label position), a WYSIWYG preview while drawing a connector, better ports, and easier moving and reconnecting of connectors.

## Notes

- Domain: the D12Canvas library (`lib/D12Canvas`), its built-ins in `lib/D12Canvas/BuiltIns`, and `Edge` in `lib/D12Canvas/Model/Edge.cs`. New features show up in D12Canvas.App's editor.
- The library takes no new dependencies. Markdown parsing, rendering and the editor's markdown round trip are hand-written, in C# and in `wwwroot/*.js`. Supporting a smaller markdown subset is fine. Images are out.
- Formatted text is stored as markdown. Users don't see the format, except that pasting markdown formats correctly.
- Formatting levels: Text, and therefore connector labels (a label is a `text` instance), get the full chosen subset. Sticky notes and shapes get bold, italic and underline only.
- Colours go through the `--d12-*` token layer. A guard test rejects colour literals in style blocks, and style blocks carry no comments.
- Skills: `/grilling` and `/domain-modeling` for grilling tickets, `/prototype` for prototype tickets (throwaway Demo routes are exempt from the visual suite), `/research` for research tickets.

## Decisions so far

- [Sticky note default size](issues/01-sticky-note-default-size.md): new notes are 100×100 with an Auto font. The shared resize floor drops from 50×50 to 16×16 for every component, and no per-type minimum is added.
- [Sticky note shrink-to-fit text](issues/02-sticky-note-shrink-to-fit.md): the sticky note's `FontSize` becomes `double?` (null means Auto), edited through a new `AutoFontSize` role. `fitText.js` fits the size live in the browser from 6px up with no cap, and text past that clips. Padding is `min(10px, 10cqmin)`, and an empty note starts at half its height.
- [Markdown subset](issues/03-markdown-subset.md): two formatting levels. Full (Text and labels) adds strikethrough, code, links (Ctrl/Cmd+click, http/https/mailto, autolinked), `#`–`###`, lists 3 deep, quotes, code blocks, rules and GFM tables. Basic (notes, shapes) is bold, italic, `<u>` underline and line breaks. Markup outside the level shows raw. No span colour or size, no migration, and Text's `FontWeight` goes.

- [Formatting UI](issues/04-formatting-ui.md): while editing, the Property bar swaps to the formatting controls for the component's level, then the box props. There's no bubble and no markdown conversion as you type. Ctrl+K opens a link field at the caret that edits both the link's text and its address. A double-click starts editing with the caret at the click point, and Enter still selects all. A table bar appears under a table, and a selected box gets a whole-text B chip in place of `FontWeight`.
- [Inline editor markdown round trip](issues/05-inline-editor-markdown-round-trip.md): the editor edits `contenteditable` with `execCommand` and native undo, and C# owns the one parser, serialiser, level narrowing and escaped HTML writer. JS only walks DOM into a JSON tree, on commit and on HTML paste. Paste prefers HTML that carries formatting we recognise, otherwise `text/plain` as markdown, and Ctrl+Shift+V pastes literally.
- [Shape kinds](issues/07-shape-kinds.md): Rectangle becomes `Shape` (key `shape`) with a `Kind` prop over eight kinds: rectangle, rounded rectangle, ellipse, diamond, hexagon, cylinder, parallelogram, trapezoid. The Palette places a rectangle, and a `ShapeKind` chip leads the Property bar. Geometry is fixed with no per-kind props. Each kind is an inline SVG drawn from props, and its body hits only inside the outline. A new registration hook puts the standard ports on each outline.
- [Text inside shapes](issues/06-rectangle-text.md): every kind gets Basic `Text`, edited inline the way a sticky note is, plus a themed `TextColor`, `FontSize` (`double?` pinned at 14, Auto optional), and centred `TextAlign` and `VerticalAlign`. Text sits in a fixed inset rectangle per kind, for example 25% on a diamond and below the cylinder's cap, and clips to it. The static text area ignores presses. An empty shape stays, with no placeholder.

## Not yet specified

- Property bar and PropertyPanel controls for every new prop (line width, dash, arrowhead size, shape kind, auto font size). Each ticket decides its own props, but the bar's chip order and grouping across all of them may need a pass once they're known.
- `CONTEXT.md` terms: what to call a sticky note's Auto font size, and a label's position on its connector.
- A Palette kind flyout, to place a diamond or ellipse in one go rather than placing a rectangle and switching. Revisit if switching after placement proves slow in use.
- Keyboard and accessibility for endpoint reconnecting, and how a keyboard user moves focus between the editor and the formatting bar while editing. Formatting shortcuts are settled in Formatting UI.

## Out of scope

- Images inside markdown, and any third-party markdown parser or editor.
