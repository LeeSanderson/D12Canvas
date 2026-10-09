# 107 — Minimap

**What to build:** A minimap shows every shape as a box plus the viewport, clickable and draggable to pan, so content the user has panned away from stays findable (ADR 0015 minimap half). `Minimap` is host-placed chrome wired to the canvas by reference, like `Palette`: one plain box per instance, no edges, mapping the union of `Content extent` and the viewport, holding its own `ZoomPanTracker` and reaching its scale through the same framing computation, boxes in board space under one transformed wrapper, `ShouldRender` keyed on board revision. Click jumps with the animated flight; drag pans unanimated through `MinimapPan`, entered from the minimap root with `classify` off and no role. It is `aria-hidden` and has no tab stop, by decision. It declares the raised token values and `color-scheme`. The App's `BoardEditor` places it bottom-right, always. This ticket adds the eighth and last `Pointer gesture`, so the release-reliability theory now asserts the set is exactly eight.

**Blocked by:** 106 (Framing commands and the initial fit)

**Status:** resolved

- [x] The minimap shows a box per shape and a viewport rect; panning the canvas moves the rect; placing a shape adds a box
- [x] Clicking the minimap flies the viewport there; dragging pans it live with no easing
- [x] A release outside the minimap ends the drag and a buttonless move afterwards pans nothing
- [x] The minimap is `aria-hidden` and Tab never lands on it
- [x] It themes through the raised values and a host override on an ancestor reaches it
- [x] The gesture-kind set is closed at eight and the theory has a release-reliability and cancel case for `MinimapPan`
- [x] The App shows it bottom-right over the canvas; a demo page shows it for the visual suite
- [x] The minimap baseline shift is planned churn; full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [x] `CONTEXT.md`'s `Minimap` and `Canvas chrome` terms describe what shipped

Shipped with choices the ticket did not spell out. A drag keeps the viewport centred on the board point under the pointer, so a click is the zero-distance case of the same rule; grabbing the rect off-centre snaps its centre to the pointer once the drag threshold is crossed. The minimap's own mapping holds still from press to release and reframes then, because a mapping that followed the viewport would move the point under the pointer as it panned; a drag past the mapped area can carry the rect off the map until release. Only the primary button presses the minimap; its listener swallows the other buttons and the browser's menu over it. Boxes take a muted tint of the minimap's surface, since no registration carries the accent ADR 0015 mentions, and only the viewport rect uses `--d12-accent`.

`Minimap` hands each press to `DiagramCanvas.BeginMinimapPan` with its own board mapping and listener, and forwards the move, release and cancel to the canvas's ordinary entry points. The canvas promotes a still press through whichever listener holds it, so a viewport key under a minimap press promotes it as ADR 0056 asks. Removing the minimap, or giving it another canvas, mid-press cancels the press on the canvas that holds it. The pointer listener takes `focusTarget` in its options, and a minimap press focuses `.diagram-canvas`, so Escape reaches it.

A new `/minimap-demo` page floats the minimap over a 900 by 520 canvas and backs three baselines (opening view, a dark theme on an ancestor, dragged off the content) and the probes for a real click, a drag released outside the minimap, Escape mid-drag, a viewport key promoting a still press, a secondary press, Tab, a canvas pan moving the rect and a duplicate adding a box.
