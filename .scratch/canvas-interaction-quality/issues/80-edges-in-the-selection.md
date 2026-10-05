# 80 — Edges in the selection

**What to build:** An end user selects edges alongside shapes in one multi-selection and deletes, moves or later locks the cluster as one set (ADR 0037). `Selection` becomes two parallel sets, instances-and-groups and edges, because all three id kinds are bare GUIDs only `Board` can tell apart. The host-facing surface gains `SelectedEdges`; `SelectedComponents` reads the expanded selection and loses its edge short-circuit. Shift+click on an edge toggles it. A marquee takes an edge by closure: every endpoint attached to a component the band selected or floating inside the band, with no line geometry test. Select All (Ctrl+A and a menu row later) selects every top-level shape and every edge. A move translates floating endpoints only, through `ChangeEdgeEndpointCommand`, and attached ends keep tracking their shapes. A command that cannot express an edge skips it: delete takes both kinds; align, distribute, z-order, group and resize read instances only; an edge widens no bounding box and counts toward no threshold. The `Gesture preview` gains a moved-endpoints slot.

**Blocked by:** 76 (DragEdgeEnd and SelectEdge)

**Status:** ready-for-agent

- [ ] Shift+click on an edge adds it to a selection of shapes; Shift+click on a selected edge removes it
- [ ] A marquee over two connected shapes selects the edge between them and never an edge that only passes behind them; deleting the selection removes exactly those
- [ ] Ctrl+A selects every top-level shape and every edge; Delete then clears the board in one entry
- [ ] Moving a mixed selection translates floating ends live in the preview and commits them through `ChangeEdgeEndpointCommand` in the same history entry as the shapes
- [ ] Ctrl+G with a shape and an edge selected groups the shapes and leaves the edge selected; resize handles reflect the instances-only box
- [ ] `SelectedEdges` exists and the property panel's selection surface reads the expanded selection
- [ ] Selection-changed notifications fire for edge-only changes
- [ ] bUnit covers marquee closure, select-all and the skip rules; the moved-endpoints slot is readable through the preview
- [ ] A baseline shows a mixed selection; full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [ ] `CONTEXT.md`'s `Selection`, `Edge` and `Interior edge` terms describe what shipped
