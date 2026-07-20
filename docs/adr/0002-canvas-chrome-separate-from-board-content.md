# Canvas chrome (palette, future minimap) is a separate component wired by reference, not nested board content

`CanvasPalette` — and any future canvas-level UI such as a minimap — is an ordinary standalone Razor component the host places in their own page markup, not nested inside `DiagramCanvas`'s pannable `ChildContent`. It's wired to a specific canvas instance via an explicit reference, mirroring the `ParentCanvas` cascading-parameter pattern `ComponentContainer` already uses.

It has no built-in `Position` parameter; placement — including floating on top of the canvas — is entirely the host's own CSS/layout. This was deliberate: chrome isn't part of persisted board content and doesn't need per-instance coordinate persistence, so it shouldn't share board content's positioning mechanism (bounds) or grow a parallel one of its own.

**Consequence:** future canvas-level UI should follow the same pattern rather than requiring changes to `DiagramCanvas` itself, which stays ignorant of both the palette and any future chrome.
