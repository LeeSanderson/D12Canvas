# 33 — Multi-selection acts as one unit

**What to build:** An end user moves or resizes a multi-selection as a single bounding-box unit: dragging any member moves all of them preserving relative offsets; resize handles on the selection's bounding box scale all members proportionally. Each drag is one gesture. (ADR 0006.)

**Blocked by:** 30 (Drag-move an instance), 31 (Resize via handles), 32 (Marquee + shift-click multi-select)

**Status:** ready-for-agent

- [ ] A bounding box is computed over the multi-selection and shown
- [ ] Moving the selection preserves every member's relative offset
- [ ] Resizing the bounding box scales member bounds proportionally
- [ ] The complete drag (move or resize) is a single gesture
- [ ] Screenshot case for a multi-selection with its bounding box
- [ ] bUnit/xUnit coverage of unit move/resize maths
