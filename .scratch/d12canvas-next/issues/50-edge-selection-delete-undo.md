# 50 — Edge selection, delete & undo

**What to build:** Edges join the selection model: an end user clicks an edge to select it (visible affordance, `aria-selected`), presses Delete to remove it, and undo/redo covers both edge creation and deletion — restoring attachments exactly.

**Blocked by:** 38 (Undo/redo placement & delete), 48 (Drag port-to-port creates an edge)

**Status:** ready-for-agent

- [ ] Clicking an edge selects it, with a visible selection affordance and `aria-selected`
- [ ] Delete removes the selected edge
- [ ] Creating an edge is undoable; undoing removes it, redo restores it with the same attachments
- [ ] Undoing an edge delete restores it with both endpoints (attached or floating) intact
- [ ] Screenshot case for a selected edge
