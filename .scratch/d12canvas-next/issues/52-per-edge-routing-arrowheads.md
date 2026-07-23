# 52 — Per-edge routing styles & arrowheads

**What to build:** An end user chooses each edge's look individually — routing style (straight/orthogonal/curved) and arrowheads (none/start/end/both) are per-edge settings, never board-wide. Changing them is an undoable gesture and the settings persist with the edge. (ADR 0005.)

**Blocked by:** 48 (Drag port-to-port creates an edge)

**Status:** ready-for-agent

- [ ] Each edge carries its own routing style; all supported styles render correctly
- [ ] Each edge carries its own arrowhead settings; all combinations render correctly
- [ ] Changing routing or arrowheads on one edge never affects another
- [ ] Style changes are undoable gestures and round-trip through persistence
- [ ] Screenshot cases per routing style and arrowhead variant
