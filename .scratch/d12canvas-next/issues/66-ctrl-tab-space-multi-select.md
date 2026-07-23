# 66 — Ctrl+Tab + Space multi-select

**What to build:** A keyboard user builds a multi-selection without a mouse: Ctrl+Tab moves focus to the next instance *without* collapsing the current selection, and Space toggles the focused instance's membership — the keyboard equivalent of shift-click. `aria-selected` tracks every change. (ADR 0010.)

**Blocked by:** 32 (Marquee + shift-click multi-select), 63 (Tab stops + focus-follows-selection)

**Status:** ready-for-agent

- [ ] Ctrl+Tab moves focus while preserving the existing selection
- [ ] Space toggles the focused instance in/out of the selection
- [ ] The resulting multi-selection behaves identically to a pointer-built one (unit move/resize, delete, group)
- [ ] `aria-selected` is correct throughout
- [ ] bUnit coverage of the focus/selection interplay
