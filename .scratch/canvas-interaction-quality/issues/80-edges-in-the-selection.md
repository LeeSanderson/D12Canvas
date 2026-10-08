# 80 — Edges in the selection

**What to build:** An end user selects edges alongside shapes in one multi-selection and deletes, moves or later locks the cluster as one set (ADR 0037). `Selection` becomes two parallel sets, instances-and-groups and edges, because all three id kinds are bare GUIDs only `Board` can tell apart. The host-facing surface gains `SelectedEdges`; `SelectedComponents` reads the expanded selection and loses its edge short-circuit. Shift+click on an edge toggles it. A marquee takes an edge by closure: every endpoint attached to a component the band selected or floating inside the band, with no line geometry test. Select All (Ctrl+A and a menu row later) selects every top-level shape and every edge. A move translates floating endpoints only, through `ChangeEdgeEndpointCommand`, and attached ends keep tracking their shapes. A command that cannot express an edge skips it: delete takes both kinds; align, distribute, z-order, group and resize read instances only; an edge widens no bounding box and counts toward no threshold. The `Gesture preview` gains a moved-endpoints slot.

**Blocked by:** 76 (DragEdgeEnd and SelectEdge)

**Status:** resolved

- [x] Shift+click on an edge adds it to a selection of shapes; Shift+click on a selected edge removes it
- [x] A marquee over two connected shapes selects the edge between them and never an edge that only passes behind them; deleting the selection removes exactly those
- [x] Ctrl+A selects every top-level shape and every edge; Delete then clears the board in one entry
- [x] Moving a mixed selection translates floating ends live in the preview and commits them through `ChangeEdgeEndpointCommand` in the same history entry as the shapes
- [x] Ctrl+G with a shape and an edge selected groups the shapes and leaves the edge selected; resize handles reflect the instances-only box
- [x] `SelectedEdges` exists and the property panel's selection surface reads the expanded selection
- [x] Selection-changed notifications fire for edge-only changes
- [x] bUnit covers marquee closure, select-all and the skip rules; the moved-endpoints slot is readable through the preview
- [x] A baseline shows a mixed selection; full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [x] `CONTEXT.md`'s `Selection`, `Edge` and `Interior edge` terms describe what shipped

## Comments

Built as specified, with three readings worth stating.

`SelectedComponents` now reads the expanded selection, as ADR 0021 asked. A selected group used to show an empty property panel. It now edits every member, and a group mixed with a standalone instance edits all three. The two panel tests that pinned the old empty state now pin the new behaviour.

The ticket says Ctrl+G "leaves the edge selected". ADR 0037's table says the edge is dropped. The build follows the ticket: the shapes become the group and the edge stays selected beside it. The ADR is unchanged.

A Shift marquee closes over what the band itself swept, not over the union with the press-time selection, which is the spec's wording. Already-selected edges are kept by the union. A group swept by the band counts all its members as swept, so an edge between two members of that group comes along.

The arrow-key nudge still moves instances only. A mixed selection nudges its shapes and leaves floating ends where they are, and an edge-only selection still takes the pan fallback. ADR 0037's amendment to ADR 0026 narrows the pan fallback to an empty selection and expects a nudge to carry floating ends. No ticket in this run asked for that, so it is left open, and `CONTEXT.md`'s `Selection` term names the nudge among the instance-only readers.

`Ctrl+A` is a new row in the shortcut table. It skips editable targets and Shift. The context menu's eligibility now reads `SelectedEdges`, so an edge id left over after undo does not open an empty menu.

One new baseline shows a sticky note and the edge above it selected together. No existing baseline moved.

The full visual suite ran in the pinned image under `-parallel none`: 122 tests, and the only failure was the new baseline before it was folded in. Review then narrowed the Shift-marquee closure, made the menu read `SelectedEdges` and tidied comments and signatures. None of these change what renders. The rerun after them was stopped by the host running low on memory, so it did not finish.
