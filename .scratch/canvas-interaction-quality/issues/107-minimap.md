# 107 — Minimap

**What to build:** A minimap shows every shape as a box plus the viewport, clickable and draggable to pan, so content the user has panned away from stays findable (ADR 0015 minimap half). `Minimap` is host-placed chrome wired to the canvas by reference, like `Palette`: one plain box per instance, no edges, mapping the union of `Content extent` and the viewport, holding its own `ZoomPanTracker` and reaching its scale through the same framing computation, boxes in board space under one transformed wrapper, `ShouldRender` keyed on board revision. Click jumps with the animated flight; drag pans unanimated through `MinimapPan`, entered from the minimap root with `classify` off and no role. It is `aria-hidden` and has no tab stop, by decision. It declares the raised token values and `color-scheme`. The App's `BoardEditor` places it bottom-right, always. This ticket adds the eighth and last `Pointer gesture`, so the release-reliability theory now asserts the set is exactly eight.

**Blocked by:** 106 (Framing commands and the initial fit)

**Status:** ready-for-agent

- [ ] The minimap shows a box per shape and a viewport rect; panning the canvas moves the rect; placing a shape adds a box
- [ ] Clicking the minimap flies the viewport there; dragging pans it live with no easing
- [ ] A release outside the minimap ends the drag and a buttonless move afterwards pans nothing
- [ ] The minimap is `aria-hidden` and Tab never lands on it
- [ ] It themes through the raised values and a host override on an ancestor reaches it
- [ ] The gesture-kind set is closed at eight and the theory has a release-reliability and cancel case for `MinimapPan`
- [ ] The App shows it bottom-right over the canvas; a demo page shows it for the visual suite
- [ ] The minimap baseline shift is planned churn; full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [ ] `CONTEXT.md`'s `Minimap` and `Canvas chrome` terms describe what shipped
