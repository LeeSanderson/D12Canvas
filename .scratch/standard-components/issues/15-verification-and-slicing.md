# Verification and slicing

Type: grilling
Status: open
Blocked by: 01, 02, 03, 04, 05, 06, 07, 08, 09, 10, 11, 13, 14
Map: [Better standard components](../map.md)

## Question

How is the spec sliced for `/implement`, and how is each slice verified?

Decide:

- the slice order and which slices can run in parallel
- which behaviour bUnit covers, which Playwright probes cover (drag, fit measurement, editor round trip), and which new screenshot cases each slice adds in light and dark
- which existing baselines each slice expects to move
- tests for the markdown parser and serialiser, including the C# and JS sides agreeing

## Comments

- From [Inline editor markdown round trip](05-inline-editor-markdown-round-trip.md): there's no JS parser or renderer, so no C#/JS agreement tests are needed. C# unit tests cover markdown ↔ tree, tree → HTML with an XSS fixture set, and narrowing. Interaction probes cover the JS DOM → tree walk against the markup Chrome and Firefox really produce, and each table, inline code and link operation followed by Ctrl+Z. Real IME and Android typing go on a manual check list.
- From [Text inside shapes](06-rectangle-text.md): screenshot cases for text in each kind's inset area at each alignment, in light and dark. A probe checks that a press on a rounded rectangle's corner passes through the static text area. Auto fit inside a diamond's and a cylinder's area can reuse the sticky note's fit probe. Empty shapes draw no placeholder, so existing Shape baselines shouldn't move from this ticket alone.
- From [Connector style props](08-connector-style-props.md): replacing the shared marker with per-edge head paths (ADR 0080) moves every baseline that has an edge. Pixels should barely change at width 2 with `Arrow`, but the markup does, so keep that change in one slice of its own before the new props land. Screenshot cases: each `ArrowStyle` at both ends, each pattern, widths 1, 2 and 16, and a selected thick edge's halo, in light and dark. Unit tests for head path data and inset per style and for the dash arrays per width. A probe checks that a press on a hollow head selects the edge and that a width-16 line hits across its whole width at low zoom. The 14 new glyphs join the Demo glyph gallery.
- From [Connector label position](09-connector-label-position.md): the always-on label background moves every baseline with an edge label. Unit tests for the anchor at a fraction on straight, orthogonal and curved routes, for nearest-point projection with the sign of the offset, for the clamp at the arrowhead insets, and for both snaps. Probes check that a drag on an unselected label selects the edge and moves the label, that Ctrl turns snapping off, that Escape restores the label, and that a double-click adds the label at the click point. Screenshot cases: a label off the midpoint, a label with an offset on an orthogonal route, and the masked line, in light and dark.
