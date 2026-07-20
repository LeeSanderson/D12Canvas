# Import/export feature design

Type: grilling
Status: resolved
Blocked by: 07

## Question

Given the resolved persistence format (versioned JSON envelope, arrays of entities, opaque per-component-type `Props`), decide what import/export actually ships as a designed feature for the PRD: which formats are supported (e.g. the native JSON envelope round-trip is already free via `IBoardSerializer`; is any non-native format — SVG/PNG export, a specific third-party tool's format — in scope for this map, or deferred entirely?), and what triggers it in the host-facing UI (a menu item, a keyboard shortcut, drag-and-drop of a file onto the canvas). Building the actual feature is not required — this ticket only needs to decide what the PRD commits to.

## Answer

Formats: native JSON round-trip only, via the already-specified `IBoardSerializer` (ticket 07). No non-native format — SVG/PNG image export, or any third-party tool's format (Miro/Figma/drawio/etc.) — ships as part of this map's PRD; all deferred entirely, consistent with this map's destination being a reusable component library, not an interop platform. Ticket 07's persistence design already keeps future formats cheap to add if a later effort wants one.

Trigger: no D12Canvas-shipped UI chrome for import/export — no button, menu item, keyboard shortcut, or drag-and-drop handling ships as canvas chrome. Unlike the palette (settled as shipped default chrome because it's load-bearing for basic board usability), import/export is peripheral, and its natural trigger is inseparable from storage medium/timing, which ticket 07 already assigned to the host. The host wires whatever trigger fits its app directly against the existing `IBoardSerializer` service.

Net effect: this ticket adds no new API surface. It confirms `IBoardSerializer` (already fully specified in ticket 07) *is* the entire import/export feature for this PRD, with nothing further to build.
