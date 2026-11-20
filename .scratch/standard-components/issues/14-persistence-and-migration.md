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
