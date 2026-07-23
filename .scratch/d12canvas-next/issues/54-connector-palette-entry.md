# 54 — "Connector" palette entry

**What to build:** An end user who prefers starting from the palette finds a built-in "Connector" entry there — present without being in the component-type registry, since an edge is not a component type. Activating it drops a floating edge (both endpoints floating) onto the board, ready for its ends to be dragged onto ports. (ADR 0009.)

**Blocked by:** 26 (Palette lists registered types), 49 (Floating endpoints)

**Status:** ready-for-agent

- [ ] A "Connector" entry appears in the palette without any registry registration
- [ ] Activating it creates an edge with both endpoints floating, placed in view
- [ ] Both floating ends can then be attached to ports
- [ ] Dropping the connector is an undoable gesture
- [ ] Screenshot case for the palette entry and the dropped floating edge
