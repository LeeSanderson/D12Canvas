# 109 — Property bar and chrome suppression while judging

**What to build:** A compact bar of glyphs floats above the selection for the properties judged by eye, and the selection chrome gets out of the way while the user looks at a colour (ADRs 0021, 0041). The `Property bar` is canvas-rendered chrome anchored by `bounds × Scale + Pan` with one measurement of its own width, centred above the selection's top, clamping and sliding along the container edge rather than flipping. It shows only role-tagged properties as glyphs with no text, 26px cells, the colour glyph painted in its own value with a themed state for null and a hatched state for mixed. It reads the expanded selection and across types shows the role intersection; on a mixed instance-and-edge selection it is empty because the role sets are disjoint. It hides for any pointer gesture and while a menu is open. Ctrl+Enter moves focus into it with roving arrows. It declares the raised token values and `color-scheme` on its own root. While it has hover or focus-within, one CSS rule hides all selection chrome, outline and edge halo included, with no timer or C# state. Rows are produced through one row seam (id, role, kind, options, value, `IsMixed`, commit callback) by two producers: instance rows commit through the props batch, edge rows (routing, arrows, colour) through `ChangeEdgeStyleCommand`. The prototype on `prototype/property-bar` is evidence, not code to merge.

**Blocked by:** 89 (Composed context menu with shortcut hints), 94 (Edge colour, selection halo and themed edges), 108 (Mixed values)

**Status:** ready-for-agent

- [ ] Selecting a rectangle shows fill, stroke and stroke-width glyphs above it; changing the fill from the bar is one undoable entry
- [ ] Selecting a shape near the top edge slides the bar along the edge rather than flipping below
- [ ] The bar is absent during a drag and while a menu is open, and reappears after
- [ ] Hovering the bar hides the selection outline, handles and edge halo; leaving restores them
- [ ] Selecting an edge shows routing, arrow and colour glyphs; recolouring is visible at once
- [ ] Selecting a shape and an edge together shows no bar; selecting a group shows the members' roles
- [ ] Ctrl+Enter focuses the first glyph and arrows move between them; Escape returns focus to the shape
- [ ] bUnit covers anchoring, hiding, the role intersection and both row producers; the one-rule suppression is asserted as markup
- [ ] A demo page shows the bar, including hover suppression and a mixed glyph, in light and dark; full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [ ] `CONTEXT.md`'s `Property bar` and `Canvas chrome` terms describe what shipped
