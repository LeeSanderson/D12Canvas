# 72 — Snap-to-grid toggle

**What to build:** An end user who wants alignment help turns on snap-to-grid — a `SnapToGrid` parameter, toggled at runtime with Ctrl+' — and placement and move operations snap to the spacing of whichever grid layer is currently dominant. Off by default; ephemeral view state like selection, never persisted regardless of the layer it tracks. (ADR 0011, amending ADR 0009's shortcut table.)

**Blocked by:** 28 (Click-to-add placement), 30 (Drag-move an instance), 71 (Adaptive multi-layer grid)

**Status:** ready-for-agent

- [ ] Snap is off by default; the `SnapToGrid` parameter and Ctrl+' both toggle it
- [ ] With snap on, placement and drag-move land on the dominant grid layer's spacing
- [ ] The snap spacing follows the dominant layer as zoom changes
- [ ] Snap state is ephemeral — never serialized with the board
- [ ] bUnit/xUnit coverage of snapping maths and the toggle
