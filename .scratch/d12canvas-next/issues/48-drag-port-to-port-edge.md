# 48 — Drag port-to-port creates an edge

**What to build:** An end user presses on a port and drags to a port on another instance: a live drag-preview follows the pointer, and dropping on the target port creates an `Edge` entity connecting the two. The edge renders (straight-line routing for now) and stays attached through moves and resizes of either endpoint. Port-drag is a distinct gesture — it doesn't conflict with drag-move or click-select. (ADR 0005.)

**Blocked by:** 30 (Drag-move an instance), 47 (Standard ports visible)

**Status:** ready-for-agent

- [ ] Dragging from a port shows a connector drag-preview; dropping on another instance's port creates an `Edge` on the `Board`
- [ ] The edge renders and tracks both endpoint instances through move and resize
- [ ] Starting a drag on a port never initiates an instance move
- [ ] Screenshot cases: mid-drag preview and a connected edge
- [ ] bUnit/xUnit coverage of edge creation and endpoint tracking
