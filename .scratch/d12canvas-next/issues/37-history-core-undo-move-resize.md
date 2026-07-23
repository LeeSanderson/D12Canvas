# 37 — History core: undo/redo move & resize

**What to build:** An end user presses Ctrl+Z after moving or resizing and the instance returns exactly where it was; Ctrl+Y (redo) reapplies it. Each completed gesture records one `ChangeBoundsCommand` in the `History` — a session-scoped, in-memory circular buffer capped at 1000 entries. One gesture is one entry, never one per intermediate frame. (ADR 0007.)

**Blocked by:** 30 (Drag-move an instance), 31 (Resize via handles)

**Status:** ready-for-agent

- [ ] One history entry per completed move or resize gesture — never per frame
- [ ] Undo restores the prior bounds; redo reapplies the change
- [ ] Ctrl+Z / Ctrl+Y shortcuts work on the canvas
- [ ] The buffer caps at 1000 entries, dropping oldest first
- [ ] History is in-memory and session-scoped — never serialized, never survives a reload
- [ ] A new gesture after undo clears the redo stack
- [ ] xUnit coverage at the command/history seam
