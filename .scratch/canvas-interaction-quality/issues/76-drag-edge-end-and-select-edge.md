# 76 — DragEdgeEnd and SelectEdge

**What to build:** Connector drags and edge clicks move onto the spine (ADRs 0018, 0037 command half). `DragEdgeEnd` takes the `port`, `port-strip` and `edge-endpoint` roles: dragging from a port starts a new edge, dragging from a port that already anchors an edge or from a floating endpoint repositions that end, the preview carries at most one pending edge line, and the drop resolves in C# at release from the hit stack under the pointer. Until ticket 91 adds the `Auto endpoint`, a drop on a port pins and anything else floats, as today, but the port hit uses the port element under the pointer rather than a board-space radius. `SelectEdge` takes the `edge` and `edge-label` roles: release from `pointing` selects the edge (Shift toggles later, once ticket 80 widens the selection) and a double-press adds a label; crossing the threshold abandons it. Repositioning an endpoint enters history for the first time through a new `ChangeEdgeEndpointCommand`, closing a live defect. The edge's hit band becomes a real 20 screen-pixel `Hit region` sized by `--d12-scale`, painted by a non-participant stroke. The old port, floating-endpoint, edge click and double-click bindings are deleted, and `IsBeingEdited`, `ConnectPreviewLine` and the connect-origin refresh go with them.

**Blocked by:** 74 (MoveSelection, the gesture preview and live geometry)

**Status:** resolved

- [x] A drag from a bare port to another shape's port creates one edge in one history entry; a drag back to the same port creates nothing
- [x] Dragging an attached end off its port and dropping on empty canvas floats it, and Ctrl+Z puts it back on the port
- [x] A release outside the canvas ends the drag and a buttonless move afterwards draws no preview
- [x] A click on an edge's line selects it at any zoom through the 20 screen-pixel band; a double-press adds a label as today
- [x] The pending edge line in the preview is the only thing representing an edge being repositioned
- [x] The release-reliability theory gains `DragEdgeEnd` and `SelectEdge`
- [x] `ChangeEdgeEndpointCommand` has command tests; the press-to-kind table gains the five rows
- [x] Port-drag, floating-endpoint, edge-selection, edge-label and keyboard-connector tests that dispatched mouse events are rewritten at the new seams
- [x] A mid-connector-drag visual baseline exists; full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [x] `CONTEXT.md`'s `Hit region` term describes what shipped

## Comments

Built as specified, with these calls made along the way.

The drop reads what lies under the release point from the browser: on a connector release the listener sends `elementsFromPoint`, each element classified as a press would be, and C# takes the topmost port or body. Chrome, resize handles and edges above it are looked through. ADR 0027 had rejected this in favour of a board-space radius; the ticket asked for the port element instead, and `PortHitRadius` and `Board.FindPortNear` are gone.

Pointer capture means hover no longer tracks the pointer during a drag, so a target shape's ports would stay hidden and unhittable. While the pending line is drawn the canvas carries a `connecting` class that shows every shape's ports. Ticket 92 narrows that to the one shape under the pointer. Both in-progress connector baselines move for it.

On a selected shape the side resize handles sit over the standard port centres, so a press there resizes. That is today's precedence and ticket 92's partition removes it; the probes pull from an unselected shape's port, which hover reveals.

The port strip's browser double-click could no longer fire under capture, so a double-press on a strip now adds the custom port through `DragEdgeEnd`, until ticket 103 retires it. The CustomPort baseline moves: it used to show the shape in the container's edit mode, which the strip's double-click had leaked into.

Label editing went the same way. A double-press on `edge-label` opens the label's editor through a `DynamicComponent` ref, as ticket 74 did for instances. A real double-click on a label right after adding it counts as presses 3 and 4, so the second press landed on the label's own textarea as `author-content` carrying the edge id, and `NativeGesture` put that id into the instance selection, clearing the edge's. `NativeGesture` now leaves the selection alone for an edge's id.

A click on a floating endpoint does nothing; nothing asked for it to select the edge. The pending line still paints in the edge band, beneath instances, until ticket 79 moves it to selection chrome, so a line carried under a shape is hidden there.

Four probes: a drop on another shape's port pins, a release outside the canvas ends the drag, a click 6 pixels off an edge's line selects it below 0.5 zoom, and a double-click on a label opens its editor. The pin, zoom and label probes each failed with the `connecting` rule, the 20-pixel stroke or the label path removed; the release-outside probe was not broken separately, since it rests on the same capture the existing release-channel probes cover.

Baselines: every HTML snapshot moves for `--d12-scale` on the content and the edge's separate hit stroke. PNGs move for the two in-progress connector drags, CustomPort as above, and FloatingEndpoint by a sub-pixel shift along the line. The new one carries the sticky note's attached end away from its port.
