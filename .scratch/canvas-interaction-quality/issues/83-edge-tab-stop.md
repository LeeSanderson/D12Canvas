# 83 — An edge is a tab stop after its source

**What to build:** A keyboard user reaches every edge with Tab, directly after the stop its source resolves to, announced as "Connector from {source} to {target}" (ADR 0054). The stop is an invisible proxy element in the instance layer, because the SVG precedes every stop in the DOM. A floating source sorts at its own point. The proxy carries `aria-selected` and no role. Landing on it hard-selects the edge; Space toggles it once ticket 84's additive traversal exists, and until then Space behaves as it does for an instance stop. No edge has a stop while a group is entered. Directional focus (ticket 85) never lands on an edge.

**Blocked by:** 81 (Entered group)

**Status:** ready-for-agent

- [ ] Tab from a shape lands on each edge leaving it, in order, before the next shape in reading order
- [ ] The stop's accessible name reads "Connector from A to B" using each end's accessible name, or the edge's position for a floating end
- [ ] Landing on an edge stop selects the edge and nothing else; the edge shows as selected
- [ ] Entering a group removes every edge stop from the ring; leaving restores them
- [ ] bUnit covers stop order, the label and `aria-selected`; a markup test asserts the proxy has no role
- [ ] `CONTEXT.md`'s `Edge` term already describes this; no vocabulary change
