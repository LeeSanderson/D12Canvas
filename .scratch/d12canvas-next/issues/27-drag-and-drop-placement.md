# 27 — Drag-and-drop placement

**What to build:** An end user drags a type from the palette and drops it on the board: a new component instance appears at the drop point, at the type's registered `DefaultSize`, initialised with its `DefaultProps`. (ADR 0009.)

**Blocked by:** 22 (Canvas renders a Board), 26 (Palette lists registered types)

**Status:** ready-for-agent

- [ ] Dropping places the instance at the drop point, converted correctly to board coordinates at any zoom/pan
- [ ] The new instance uses the registered `DefaultSize` and `DefaultProps`
- [ ] The instance is added to the `Board` (a real entity, not a visual-only artifact)
- [ ] A drag-in-progress affordance is visible; screenshot case added
- [ ] bUnit/xUnit coverage of the placement behaviour
