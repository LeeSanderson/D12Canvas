# Full keyboard-accessibility interaction

Type: grilling
Status: open
Blocked by: 14

## Question

Beyond the baseline screen-reader-friendly component contract (ADR 0001's auto-applied ARIA, ticket 05) and the baseline `aria-selected` (ticket 09), design full keyboard-driven interaction with the board: tab order among component instances and groups, arrow-key move (and resize, possibly with a modifier key), and a keyboard-triggered equivalent to marquee/multi-select. Decide how these interact with the baseline shortcuts and placement/connector gestures resolved in ticket 14 — e.g. can a new instance or connector be created and attached without a mouse at all, and does keyboard focus order need to reflect z-order, grouping, or something else?
