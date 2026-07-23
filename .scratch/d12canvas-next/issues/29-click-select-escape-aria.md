# 29 — Click select, escape, and `aria-selected`

**What to build:** An end user clicks a component instance to select it: a visible selection affordance appears, `aria-selected` is exposed for assistive tech, and Escape (or clicking empty canvas) clears the selection. Selection is transient view state — never on the `Board`, never persisted, never in history. (ADR 0006.)

**Blocked by:** 22 (Canvas renders a Board)

**Status:** ready-for-agent

- [ ] Clicking an instance selects it; clicking empty canvas or pressing Escape clears the selection
- [ ] A visible selection affordance renders on the selected instance; screenshot case added
- [ ] `aria-selected` is set on the selected instance and absent/false otherwise
- [ ] Selection state lives outside the `Board` and is never serialized
- [ ] bUnit coverage of select/clear behaviour and ARIA state
