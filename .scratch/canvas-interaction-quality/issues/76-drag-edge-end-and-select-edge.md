# 76 — DragEdgeEnd and SelectEdge

**What to build:** Connector drags and edge clicks move onto the spine (ADRs 0018, 0037 command half). `DragEdgeEnd` takes the `port`, `port-strip` and `edge-endpoint` roles: dragging from a port starts a new edge, dragging from a port that already anchors an edge or from a floating endpoint repositions that end, the preview carries at most one pending edge line, and the drop resolves in C# at release from the hit stack under the pointer. Until ticket 91 adds the `Auto endpoint`, a drop on a port pins and anything else floats, as today, but the port hit uses the port element under the pointer rather than a board-space radius. `SelectEdge` takes the `edge` and `edge-label` roles: release from `pointing` selects the edge (Shift toggles later, once ticket 80 widens the selection) and a double-press adds a label; crossing the threshold abandons it. Repositioning an endpoint enters history for the first time through a new `ChangeEdgeEndpointCommand`, closing a live defect. The edge's hit band becomes a real 20 screen-pixel `Hit region` sized by `--d12-scale`, painted by a non-participant stroke. The old port, floating-endpoint, edge click and double-click bindings are deleted, and `IsBeingEdited`, `ConnectPreviewLine` and the connect-origin refresh go with them.

**Blocked by:** 74 (MoveSelection, the gesture preview and live geometry)

**Status:** ready-for-agent

- [ ] A drag from a bare port to another shape's port creates one edge in one history entry; a drag back to the same port creates nothing
- [ ] Dragging an attached end off its port and dropping on empty canvas floats it, and Ctrl+Z puts it back on the port
- [ ] A release outside the canvas ends the drag and a buttonless move afterwards draws no preview
- [ ] A click on an edge's line selects it at any zoom through the 20 screen-pixel band; a double-press adds a label as today
- [ ] The pending edge line in the preview is the only thing representing an edge being repositioned
- [ ] The release-reliability theory gains `DragEdgeEnd` and `SelectEdge`
- [ ] `ChangeEdgeEndpointCommand` has command tests; the press-to-kind table gains the five rows
- [ ] Port-drag, floating-endpoint, edge-selection, edge-label and keyboard-connector tests that dispatched mouse events are rewritten at the new seams
- [ ] A mid-connector-drag visual baseline exists; full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [ ] `CONTEXT.md`'s `Hit region` term describes what shipped
