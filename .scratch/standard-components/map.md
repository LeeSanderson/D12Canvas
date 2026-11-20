# Better standard components

Label: wayfinder:map

## Destination

A spec at `.scratch/standard-components/spec.md`, ready for `/implement`. It covers smaller sticky notes with shrink-to-fit text, markdown-backed formatted text in Text, sticky notes and rectangles, shape kinds for Rectangle, connector styling (width, dash, arrowhead size, label position), a WYSIWYG preview while drawing a connector, better ports, and easier moving and reconnecting of connectors.

## Notes

- Domain: the D12Canvas library (`lib/D12Canvas`), its built-ins in `lib/D12Canvas/BuiltIns`, and `Edge` in `lib/D12Canvas/Model/Edge.cs`. New features show up in D12Canvas.App's editor.
- The library takes no new dependencies. Markdown parsing, rendering and the editor's markdown round trip are hand-written, in C# and in `wwwroot/*.js`. Supporting a smaller markdown subset is fine. Images are out.
- Formatted text is stored as markdown. Users don't see the format, except that pasting markdown formats correctly.
- Formatting levels: Text, and therefore connector labels (a label is a `text` instance), get the full chosen subset. Sticky notes and rectangles get bold, italic and underline only.
- Colours go through the `--d12-*` token layer. A guard test rejects colour literals in style blocks, and style blocks carry no comments.
- Skills: `/grilling` and `/domain-modeling` for grilling tickets, `/prototype` for prototype tickets (throwaway Demo routes are exempt from the visual suite), `/research` for research tickets.

## Decisions so far

- [Sticky note default size](issues/01-sticky-note-default-size.md): new notes are 100×100 with an Auto font. The shared resize floor drops from 50×50 to 16×16 for every component, and no per-type minimum is added.

## Not yet specified

- Property bar and PropertyPanel controls for every new prop (line width, dash, arrowhead size, shape kind, auto font size). Each ticket decides its own props, but the bar's chip order and grouping across all of them may need a pass once they're known.
- `CONTEXT.md` terms: what to call a shape kind, formatted text, the markdown subset, and a label's position on its connector.
- Keyboard and accessibility for formatting and for endpoint reconnecting.
- Whether the existing `FontWeight` prop on Text survives once inline bold exists.

## Out of scope

- Images inside markdown, and any third-party markdown parser or editor.
