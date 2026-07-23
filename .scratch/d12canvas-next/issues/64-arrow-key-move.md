# 64 — Arrow-key move

**What to build:** A keyboard user nudges the focused/selected content with the arrow keys — a zoom-relative step, so movement stays proportionate at any magnification. Nudges are undoable gestures. Works for single instances, multi-selections, and groups. (ADR 0010.)

**Blocked by:** 37 (History core: undo/redo move & resize), 63 (Tab stops + focus-follows-selection)

**Status:** ready-for-agent

- [ ] Arrow keys move the selection by a zoom-relative step in the pressed direction
- [ ] Nudges record undoable history entries (a rapid burst doesn't flood history per ADR 0010's gesture intent)
- [ ] Works uniformly for a single instance, a multi-selection, and a group
- [ ] bUnit coverage of step size at different zoom levels and undo behaviour
