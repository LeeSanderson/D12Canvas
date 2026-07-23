# 53 — Edge labels

**What to build:** An end user adds a label to an edge: rich content embedded on the edge itself — not a separate board entity — defaulting to text edited in place like other on-canvas text. The label rides the edge, staying positioned along it as endpoints move. (ADR 0005 — a label has no existence independent of the edge that owns it.)

**Blocked by:** 43 (Inline WYSIWYG text editing), 48 (Drag port-to-port creates an edge)

**Status:** ready-for-agent

- [ ] A label can be added to an edge and edited in place (default: text)
- [ ] The label stays positioned along the edge as either endpoint moves
- [ ] The label is embedded on the edge — deleting the edge removes the label; no separate entity exists
- [ ] Label edits are undoable gestures and the label round-trips through persistence
- [ ] Screenshot case for a labelled edge
