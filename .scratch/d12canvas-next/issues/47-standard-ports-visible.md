# 47 — Standard ports visible

**What to build:** Every component instance automatically exposes four standard ports at its border centres (top/right/bottom/left) — positioned as fractions of the instance's bounds so they stay correct through move and resize, with no change to the registration contract. The end user sees them as attachment affordances on hover/selection, hidden otherwise. (ADR 0005.)

**Blocked by:** 22 (Canvas renders a Board)

**Status:** ready-for-agent

- [ ] All four standard ports exist on every instance automatically — component authors do nothing
- [ ] Ports are fractionally positioned and stay at border centres through move and resize
- [ ] Port affordances appear on hover/selection and are hidden otherwise
- [ ] Screenshot case for visible ports
- [ ] bUnit coverage of port presence and positioning
