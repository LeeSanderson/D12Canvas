# 63 — Tab stops + focus-follows-selection

**What to build:** A keyboard user tabs through board content in reading order, reaching every component instance; a `Group` collapses to a single tab stop. Focus and selection stay coherent: focusing an instance selects it, and selecting (by any means) moves focus. A visible focus indicator renders, and a screen reader announces each instance by its `AccessibleName` and `Role`. (ADR 0010.)

**Blocked by:** 29 (Click select, escape, and `aria-selected`), 44 (Group/ungroup lifecycle)

**Status:** ready-for-agent

- [ ] Tab traverses instances in reading order; Shift+Tab reverses
- [ ] A group is one tab stop — members are not individually reachable while grouped
- [ ] Focusing selects; selecting focuses — the two never diverge
- [ ] A visible focus indicator renders; screenshot case added
- [ ] Instances are announced by `AccessibleName`/`Role`, with `aria-selected` reflecting state
- [ ] bUnit coverage of tab order and focus/selection coherence
