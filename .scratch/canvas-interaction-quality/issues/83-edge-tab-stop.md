# 83 — An edge is a tab stop after its source

**What to build:** A keyboard user reaches every edge with Tab, directly after the stop its source resolves to, announced as "Connector from {source} to {target}" (ADR 0054). The stop is an invisible proxy element in the instance layer, because the SVG precedes every stop in the DOM. A floating source sorts at its own point. The proxy carries `aria-selected` and no role. Landing on it hard-selects the edge; Space toggles it once ticket 84's additive traversal exists, and until then Space behaves as it does for an instance stop. No edge has a stop while a group is entered. Directional focus (ticket 85) never lands on an edge.

**Blocked by:** 81 (Entered group)

**Status:** resolved

- [x] Tab from a shape lands on each edge leaving it, in order, before the next shape in reading order
- [x] The stop's accessible name reads "Connector from A to B" using each end's accessible name (a group's label for a grouped end), or "unattached end" for a floating end, as ADR 0054 says
- [x] Landing on an edge stop selects the edge and nothing else; the edge shows as selected
- [x] Entering a group removes every edge stop from the ring; leaving restores them
- [x] bUnit covers stop order, the label and `aria-selected`; a markup test asserts the proxy has no role
- [x] `CONTEXT.md`'s `Edge` term already describes this; no vocabulary change

Still open: ADR 0054's change to ADR 0051, where `Escape` out of an edge label's edit returns focus to the edge's stop, belongs to ticket 101, which owns that edit path. `Space` on an edge stop toggles it through the same toggle a Shift press on an edge uses, as it toggles an instance stop today; ticket 84 decides how the toggle fits additive traversal.
