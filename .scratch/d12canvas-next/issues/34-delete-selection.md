# 34 — Delete selection

**What to build:** An end user presses Delete and every selected entity is removed from the `Board`. Works for a single instance or a whole multi-selection; the selection clears afterwards. (Baseline shortcut from ADR 0009.)

**Blocked by:** 29 (Click select, escape, and `aria-selected`)

**Status:** ready-for-agent

- [ ] Delete removes all currently selected instances from the `Board`
- [ ] Works for single and multi selections
- [ ] The selection is empty after deletion
- [ ] bUnit coverage of the delete behaviour
