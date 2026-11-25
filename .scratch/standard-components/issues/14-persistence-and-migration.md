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
