# Canvas chrome (palette, future minimap) is a separate component wired by reference, not nested board content

`CanvasPalette` — and any future canvas-level UI such as a minimap — is an ordinary standalone Razor component the host places in their own page markup, not nested inside `DiagramCanvas`'s pannable `ChildContent`. It's wired to a specific canvas instance via an explicit reference, mirroring the `ParentCanvas` cascading-parameter pattern `ComponentContainer` already uses.

It has no built-in `Position` parameter; placement — including floating on top of the canvas — is entirely the host's own CSS/layout. This was deliberate: chrome isn't part of persisted board content and doesn't need per-instance coordinate persistence, so it shouldn't share board content's positioning mechanism (bounds) or grow a parallel one of its own.

**Consequence:** future canvas-level UI should follow the same pattern rather than requiring changes to `DiagramCanvas` itself, which stays ignorant of both the palette and any future chrome.

**Addendum (surfaced while resolving the viewport-inset ticket):** nothing here is reopened, and ADR 0033 spells out what "stays ignorant" costs a host that takes up the permission to float. The canvas places content against the full `.diagram-container` rect, so chrome floated on top occludes whatever the library put there. Chrome laid out beside the canvas shrinks that rect and needs no mechanism at all; a host that floats compensates through the public `ZoomPanTracker`. No preference between the two is expressed, here or there.

Note also that ADR 0021 named a second category this ADR did not contemplate. **Canvas-rendered** chrome, the selection context menu and the property bar, is drawn by `DiagramCanvas` itself inside `.diagram-container` and positioned from board geometry. This ADR governs **host-placed** chrome only: the palette, the minimap and the property panel.
