# 28 — Click-to-add placement

**What to build:** An end user clicks a palette entry and the component instance appears instantly at the viewport centre — no drag needed. Repeated click-adds cascade with an offset so successive instances don't stack invisibly on top of each other. (ADR 0009.)

**Blocked by:** 22 (Canvas renders a Board), 26 (Palette lists registered types)

**Status:** ready-for-agent

- [ ] Clicking a palette entry places an instance at the current viewport centre in board coordinates
- [ ] Consecutive click-adds apply a cascading offset
- [ ] The new instance uses the registered `DefaultSize` and `DefaultProps`
- [ ] bUnit/xUnit coverage including the cascade behaviour
