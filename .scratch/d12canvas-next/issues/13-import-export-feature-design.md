# Import/export feature design

Type: grilling
Status: open
Blocked by: 07

## Question

Given the resolved persistence format (versioned JSON envelope, arrays of entities, opaque per-component-type `Props`), decide what import/export actually ships as a designed feature for the PRD: which formats are supported (e.g. the native JSON envelope round-trip is already free via `IBoardSerializer`; is any non-native format — SVG/PNG export, a specific third-party tool's format — in scope for this map, or deferred entirely?), and what triggers it in the host-facing UI (a menu item, a keyboard shortcut, drag-and-drop of a file onto the canvas). Building the actual feature is not required — this ticket only needs to decide what the PRD commits to.
