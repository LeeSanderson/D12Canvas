# 30 — Drag-move an instance

**What to build:** An end user drags a selected component instance to move it. Movement is correct at any zoom level (screen delta converted to board delta), the instance's bounds update on the `Board`, and the whole press-to-release drag is one gesture — the unit undo/redo will later operate on.

**Blocked by:** 29 (Click select, escape, and `aria-selected`)

**Status:** ready-for-agent

- [ ] Dragging moves the instance with correct zoom-relative coordinates
- [ ] Bounds on the `Board` reflect the final position after release
- [ ] The complete press-to-release drag is treated as a single gesture
- [ ] Screenshot case for a mid-drag state
- [ ] bUnit/xUnit coverage of bounds updates
