# Panel icon controls

Type: grilling
Status: open
Blocked by: 06
Map: [Library icons, Palette and Property bar](../map.md)

## Question

Which `PropertyPanel` controls become icon controls, and how do they behave?

The layout stays a label-and-control list. Decide:

- which `EditorKind` and `PropertyRole` combinations become segmented buttons, toggles or icon lists, following the choices made in Property bar look
- how a mixed value and a locked selection show on an icon control
- the keyboard and screen-reader behaviour of each control (radio group, toggle button)

## Comments

From [Icon inventory](01-icon-inventory.md): the `PropertyPanel` edits component instances only. It has no edge fields, so edge routing and edge arrows aren't panel controls today. Making them icon controls would first mean adding edges to the panel, which may sit past the destination.
