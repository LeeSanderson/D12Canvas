# 79 — Four paint layers

**What to build:** Edges paint beneath every shape and the selection box above everything, so send-to-back never hides a shape under its connectors (ADR 0055). The canvas content holds four fixed `Paint layer`s, each its own stacking context, bottom to top: the edge band (lines, the selected-edge halo once ticket 94 adds it, labels), the instance layer (containers and placeholders by `ZIndex`, then host child content, plus every tab-stop proxy), the guide layer (ticket 87's guides), and selection chrome (marquee, selection box and handles, floating endpoints, the connector preview). `ZIndex` orders the inside of the instance layer only. Hit order and paint order agree everywhere. Edges have no z-order commands. The vestigial 3000 by 3000 size on the content element is removed.

**Blocked by:** 76 (DragEdgeEnd and SelectEdge)

**Status:** resolved

- [x] A shape sent to back still paints above every edge; the selection box paints above every shape at any `ZIndex`
- [x] The marquee, floating endpoints and the connector preview live in the chrome layer and are hittable above instances where they are hittable at all
- [x] Z-order rows and chords act on instances only and a selected edge is skipped
- [x] A press at a point where an edge and a shape overlap resolves to the shape, matching paint order
- [x] The content element has no fixed width or height
- [x] The layer wrappers move every `.verified.html`, which is planned churn; a baseline shows an edge beneath a shape sent to back
- [x] Full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [x] `CONTEXT.md`'s `Paint layer` term describes what shipped

## Comments

The 3000 by 3000 size was not vestigial. `.diagram-canvas` sat in a `minmax(400px, auto)` grid row, so in any container taller than 400px the canvas element stopped at 400px. Presses below that reached the pointer listener only because they landed on the oversized content box, which is a descendant of the canvas. Removing the size broke every probe that pressed low on the probe page. The canvas now fills its container with `position: absolute; inset: 0`, so its hit area is the visible viewport at any pan. The container keeps its grid rows, because they still give a host that sets no height a minimum one. `GroupTabStopVisualTests` clicked canvas point (900, 500) on a canvas about 418 pixels wide. Only the content box covered that point, and scrolling it into view panned the `overflow: hidden` container by about 11 pixels, which the old baseline had captured. The click now lands inside the visible canvas.

The two edge svgs are 1 by 1 with `overflow: visible`. Edges outside the old 3000 pixel box, or at negative board coordinates, were clipped before and now paint.

A selected edge's menu has no arrangement rows: `SelectionContextMenu` gained `CanArrange`. The chords already read only the instance selection. `focusTabStopAt` now counts only tab stops inside the instance layer, so focusable content in an edge label, which now comes earlier in the DOM, cannot shift the index.

Hit order is verified with `elementFromPoint` on a new `/paint-layers-demo` board. The listener classifies a press by walking up from the event target, and the target is the element the browser hit-tests, so the same check covers the press path. The `CONTEXT.md` entry already matched what shipped and is unchanged. Its halo slot waits for ticket 94.

All 54 `.verified.html` files moved for the layer wrappers and the CSS. 23 `.png` baselines moved. Seventeen are antialiasing from the new stacking contexts: 5 pixels at one channel step on the palette border in most, and up to 889 pixels on small text at reduced zoom. Four show the marquee now painting over shapes. One shows a carried connector preview painting over the sticky note it crosses. `GroupTabStopVisualTests` lost the accidental scroll. One new baseline shows an edge and its label beneath a shape sent to back.
