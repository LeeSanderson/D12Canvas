# 32 — Marquee + shift-click multi-select

**What to build:** An end user drags on empty canvas to draw a marquee: every component instance it *intersects* (not only fully contains) joins the selection. Shift-clicking an instance toggles its membership in the current selection. (ADR 0006.)

**Blocked by:** 29 (Click select, escape, and `aria-selected`)

**Status:** ready-for-agent

- [ ] Dragging on empty canvas draws a visible marquee; screenshot case added
- [ ] Marquee selection is intersection-based
- [ ] Shift-click adds an unselected instance and removes a selected one
- [ ] `aria-selected` is correct on every member of a multi-selection
- [ ] bUnit coverage of intersection semantics and toggle behaviour
