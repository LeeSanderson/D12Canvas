# 67 — Keyboard placement

**What to build:** A keyboard user places a component entirely mouse-free: palette entries are reachable and activatable by keyboard, activation places the instance at the viewport centre (the click-to-add path), focus moves to the new instance, and arrow-key nudging positions it precisely. (ADR 0010 — placement via existing focus/nudge mechanics.)

**Blocked by:** 28 (Click-to-add placement), 64 (Arrow-key move)

**Status:** ready-for-agent

- [ ] Palette entries are tabbable and activate on Enter/Space
- [ ] Activation places the instance at the viewport centre with cascading offset
- [ ] Focus lands on the newly placed instance, which is selected
- [ ] Arrow keys then position it; the whole flow needs no pointer
- [ ] bUnit coverage of the keyboard activation and focus hand-off
