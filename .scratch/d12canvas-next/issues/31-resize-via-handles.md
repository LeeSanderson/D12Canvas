# 31 — Resize via handles

**What to build:** A selected component instance shows resize handles; the end user drags a handle to resize, with the opposite edge/corner staying anchored. Resizing is zoom-relative correct and the whole handle-drag is one gesture.

**Blocked by:** 29 (Click select, escape, and `aria-selected`)

**Status:** ready-for-agent

- [ ] Resize handles appear on the selected instance
- [ ] Dragging a handle resizes with the opposite edge/corner anchored
- [ ] Resizing is correct at any zoom level; bounds can never invert or go negative
- [ ] The complete handle-drag is a single gesture
- [ ] Screenshot cases: handles visible, mid-resize state
- [ ] bUnit/xUnit coverage of bounds updates and anchoring
