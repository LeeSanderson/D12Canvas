# Align and distribute commands

Type: grilling
Status: resolved

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

## Answer

Recorded as **ADR 0014** (`docs/adr/0014-align-and-distribute-model.md`). No ADR is reopened: 0006 and 0007 are relied on as they stand, and 0011's adaptive grid is consumed rather than amended.

**Reference frame — the selection bounds, no anchor.** Both commands compute geometry from the union of the bounds of the *top-level* selected entities. The ticket's suspicion about "first selected" was right and is now a checked fact: `_selectedInstanceIds` is a `HashSet<Guid>` (`DiagramCanvas.razor.cs:151`), so insertion order is not merely unused, it is unrecoverable. A key object would need ordering or an anchor field on ADR 0006's model, a visible indicator, and a gesture to set it — and marquee selection would fill any such order in hit-test sequence, which is arbitrary to the user, so the ordering would be a lie even once stored.

**Top-level entities, rigid-body groups.** Both read the **unexpanded** selection, where an entry is a component instance or a top-level `Group` whose bounds come from `Board.GetBounds(group)` (`Board.cs:163`). A group's delta is computed once and applied to every leaf member, so its internal arrangement survives; nesting falls out of `ExpandInto`'s existing recursion.

This is the finding most likely to be mis-implemented, so it is stated in the ADR rather than left to implementation time: **`ExpandedSelection()` (`:1100`) is the wrong tool here**, and it is the obvious code to reach for. Every existing multi-entity operation uses it, and `CommitGroupMove` (`:1218`) builds one `ChangeBoundsCommand` per flattened member. Flattening is invisible for a move, which applies *one delta to everything*. Align computes a delta *per entity* — so reuse would slam each member of a group independently onto the shared edge, dissolving the arrangement that made it a group. Only the traversal is new; the command vocabulary is unchanged.

**Aligning within a group is refused, with no carve-out.** One group is one entity and align requires two, so the case is already ineligible under the minimum-selection rule. Reaching inside a group needs the click-through-into-group gesture ADR 0006 explicitly punted, which this ticket does not invent.

**Eight actions.** Align left/centre/right and top/middle/bottom; distribute horizontally and vertically. Excluded: a tidy-up/auto-arrange action (a re-layout heuristic deciding row counts and wrap points — the auto-layout family the map holds out of scope); align-to-canvas (ADR 0011 left the board unbounded, so there is no referent); a distinct align-to-grid action (already expressible as `SnapToGrid` plus any align). Both middle actions ship — centring a row of mixed-height nodes is the commonest tidy-up in a diagram, and a user who cannot find it drags by hand.

**Distribute means equal gaps**, entities ordered by current centre on the axis, one action per axis, no equal-centres variant. Equal centres would suit a flowchart's connector rhythm, but a *regular* pitch is better served by the existing `SnapToGrid` than by a second pair of menu items in a menu ticket 10 already flags as filling up. Negative gaps are permitted: when the objects do not fit they overlap evenly, which is the honest result and one undo away.

**Eligibility: 2+ for align, 3+ for distribute**, counted unexpanded — the same notion of selection size as `CanGroupSelection` (`:1047`), so one selected `Group` of five members qualifies for neither. Distribute's threshold is definitional, not defensive: with two entities both are extremes, both pinned, nothing between to move. Below threshold the items are **hidden**, following the existing `CanGroup`/`CanUngroup` pattern (`SelectionContextMenu.razor:10`); a keyboard invocation is a silent no-op. Zero-delta entities contribute no command, so **an align that moves nothing pushes nothing** — align-left never moves the entity defining the left edge, and a second press must not cost an undo to get past. Precedent: `MoveComponent:1205` and `RestackSelection`.

**Snap is observed** (this reversed the initial recommendation, which was to ignore it), using the same zoom-adaptive `DominantGridSpacing()` as drag-snap. That spacing steps by ten — `GridBaseSpacing = 20`, `GridSpacingStep = 10` (`:1832`), so the unit is 20/200/2000 — but is defined so a step stays ~20 screen px at any zoom, keeping the snap displacement ~10 screen px regardless. Accepted cost, stated in the ADR: the same align at two zoom levels can land at different board coordinates.

- **Align snaps the target coordinate, not the result.** `target = snap(bbox edge)`, then every entity's corresponding edge is set to exactly `target`. This is what makes snap-observance viable at all: snapping each entity's `Bounds` after aligning rounds **X**, which preserves align-left (all X equal, all rounding alike) but pulls align-right and align-centre apart, since differing widths give differing X. Three exact and three approximate actions is not a model. Consequence for implementation: **`SnapBounds` (`:1874`) is not reusable** — it snaps a whole `Bounds`; a scalar snap of one coordinate is needed.
- **Distribute rounds the gap**, clamped to at least one grid step when the ideal gap is positive, so it never degenerates into *pack* — not exotic, it is any row of small nodes where the dominant step is 200. No clamp when the gap is already zero or negative.
- **The first entity is pinned, the last drifts** (leftmost/topmost), by up to half a grid step per interval. Pinning the selection centre would keep the selection visually in place but makes the first entity's position depend on the entity count. **With snap off, both extremes stay put** — the drift exists only because snapping introduced it.

**History — confirmed, not assumed.** One `CompositeCommand` per invocation over one `ChangeBoundsCommand` per moved instance, a rigid-body group contributing one command per leaf member sharing its delta. `ChangeBoundsCommand` swaps whole `Bounds` and align only changes X or Y, so no new command type. Matches `CommitPropsChangeBatch`, `CommitGroupMove` and `OnDeletePressed`.

**API and surfacing.** Eight one-line public methods (`OnAlignLeftPressed` … `OnDistributeVerticallyPressed`) over two private parameterised implementations, enums private — mirroring the z-order quartet (`:928–961`), where four named commands delegate to `RestackSelection`/`ApplyZIndexChange`. The counter-case (two public methods with public enums, so ticket 10 could iterate `Enum.GetValues`) was weighed and rejected: two permanent public types for a convenience the menu likely will not use, since its items need per-item labels anyway.

Surfaces are the **context menu** and **keyboard**; not the property bar (it edits properties, not spatial arrangement) and **no toolbar** (new persistent chrome this effort did not take, colliding with the still-unresolved chrome-layout question). This ticket delivers **only the commands and their geometry**. Menu readability with eight new items is ticket 10's already-written question; chord assignment is ticket 16's, which exists to keep chords coherent across everything this map adds.

**Map consequence:** because the commands are public methods with no chrome of their own, **ticket 12 depends on neither 10 nor 16 in either direction**. They are buildable and unit-testable with no chrome at all, and would otherwise have sat blocked behind ticket 07 for no reason.

No new tickets surface and nothing new goes out of scope. One fog patch amended: align-within-a-group now waits on the same click-through-into-group gesture as the multi-select refinements patch.

**No domain term is introduced.** `PointIsWithinSelectionBounds` (`:2169`) already establishes "selection bounds" and ADR 0013 established "top-level entities", so `CONTEXT.md` needs no change.
