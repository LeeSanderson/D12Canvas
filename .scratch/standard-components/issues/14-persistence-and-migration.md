# Persistence and migration

Type: grilling
Status: open
Blocked by: 02, 03, 06, 07, 08, 09, 11
Map: [Better standard components](../map.md)

## Question

How do the new props and markdown text persist, and how do old boards load?

Decide:

- the board envelope changes for edge style, label position, shape kind, shape text, auto font size and port attach modes
- whether the envelope version bumps, and what each missing field defaults to on load
- how plain-text `Text` values from old boards convert to markdown
- clipboard payload changes, and paste from an older version

## Comments

- From [Sticky note shrink-to-fit text](02-sticky-note-shrink-to-fit.md): the sticky note's Auto font size keeps the `FontSize` property name and widens it to `double?`, with null meaning Auto. Old boards' stored numbers load as pinned sizes with no conversion. Only new notes get Auto.
- From [Markdown subset](03-markdown-subset.md): the project hasn't been released, so old boards need no conversion. Existing text loads as markdown and there's no schema version bump for it. The same reasoning likely removes the version question for the other new props. Text's `FontWeight` prop is removed.
- From [Inline editor markdown round trip](05-inline-editor-markdown-round-trip.md): storage holds markdown only. The JSON document tree goes between C# and JS while editing and is never persisted. Copying from the editor puts the markdown source on the system clipboard as `text/plain` and the rendered HTML as `text/html`. That's separate from the board clipboard payload this ticket covers.
- From [Shape kinds](07-shape-kinds.md): component key `rectangle` becomes `shape`, with no migration since the project is unreleased. `ShapeProps` adds a `Kind` value (8 kinds, default rectangle). Decide how a missing or unknown `Kind` loads.
- From [Text inside shapes](06-rectangle-text.md): `ShapeProps` also gains `Text` (markdown, default `""`), `TextColor` (`string?`, null is themed), `FontSize` (`double?`, default 14, null is Auto), `TextAlign` (default `center`) and `VerticalAlign` (default `middle`). Decide what each loads as when missing.
- From [Connector style props](08-connector-style-props.md): `Edge` gains `LineWidth` (`double`, default 2, clamped 1–16) and `LinePattern` (`Solid` default, `Dashed`, `Dotted`). `ArrowStyle` gains `OpenArrow`, `HollowTriangle`, `Diamond`, `HollowDiamond` and `Circle`, and `None` and `Arrow` keep their names, so stored arrow values load unchanged. There's no arrowhead size to store. Decide what a missing width or pattern and an unknown arrow or pattern name load as, and how an out-of-range width is treated on load.
