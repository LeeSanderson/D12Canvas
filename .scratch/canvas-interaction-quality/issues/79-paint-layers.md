# 79 — Four paint layers

**What to build:** Edges paint beneath every shape and the selection box above everything, so send-to-back never hides a shape under its connectors (ADR 0055). The canvas content holds four fixed `Paint layer`s, each its own stacking context, bottom to top: the edge band (lines, the selected-edge halo once ticket 94 adds it, labels), the instance layer (containers and placeholders by `ZIndex`, then host child content, plus every tab-stop proxy), the guide layer (ticket 87's guides), and selection chrome (marquee, selection box and handles, floating endpoints, the connector preview). `ZIndex` orders the inside of the instance layer only. Hit order and paint order agree everywhere. Edges have no z-order commands. The vestigial 3000 by 3000 size on the content element is removed.

**Blocked by:** 76 (DragEdgeEnd and SelectEdge)

**Status:** ready-for-agent

- [ ] A shape sent to back still paints above every edge; the selection box paints above every shape at any `ZIndex`
- [ ] The marquee, floating endpoints and the connector preview live in the chrome layer and are hittable above instances where they are hittable at all
- [ ] Z-order rows and chords act on instances only and a selected edge is skipped
- [ ] A press at a point where an edge and a shape overlap resolves to the shape, matching paint order
- [ ] The content element has no fixed width or height
- [ ] The layer wrappers move every `.verified.html`, which is planned churn; a baseline shows an edge beneath a shape sent to back
- [ ] Full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [ ] `CONTEXT.md`'s `Paint layer` term describes what shipped
