# Align and distribute commands

Type: grilling
Status: open

## Question

Design explicit align and distribute actions on a multi-selection — the deliberate counterpart to ticket 11's during-drag guides.

Neither exists today.

Decide:

- The action set: align left/centre/right/top/middle/bottom, distribute horizontally and vertically. Whether distribute means equal gaps or equal centres — these differ whenever objects have different sizes, and tools disagree on which is the default.
- What each aligns *to*. The selection's bounding box, the first-selected item, the largest item, or the last-clicked one. Selection in ADR 0006 is a set with no inherent ordering, so "first selected" may not be recoverable — check before relying on it.
- Behaviour when the selection contains a `Group`. A group moves as one unit and its bounds are computed from members, so aligning a group to other objects is well-defined, but aligning *within* a group is not. Decide whether that case is supported or refused.
- Minimum selection size. Align needs two, distribute needs three.
- History shape. Each action moves several entities and must be exactly one entry per ADR 0007 — `CompositeCommand` over `ChangeBoundsCommand` should cover it without a new command type, matching `CONTEXT.md`'s warning against inventing one per feature. Confirm rather than assume.
- Where the actions are surfaced: context menu (10), keyboard shortcuts (16), a toolbar that does not currently exist, or the property bar (08). If a toolbar is the answer, that is new chrome and needs to be recognised as scope.
- Whether alignment respects `SnapToGrid` when it is active, or overrides it.
