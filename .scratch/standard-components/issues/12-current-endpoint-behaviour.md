# Current endpoint and port behaviour

Type: task
Status: open
Blocked by:
Map: [Better standard components](../map.md)

## Question

What does the canvas do today when a user moves a connector or drags an endpoint?

AFK. Read the code (`DiagramCanvas*.cs`, `EdgeView.razor*`, `PortPlacement.cs`, `EdgeRouter.cs`) and drive the App if useful. Record:

- whether endpoint handles exist and how a user grabs them
- what dropping an endpoint on a port, a shape body, and empty board does
- whether a whole connector can be dragged, and what happens to attached ends
- hit target sizes for ends and ports at 100% zoom
- the friction points that make moving and reconnecting hard, with file and line references
