# Selection model

Type: grilling
Status: resolved
Blocked by: 06

## Question

Design the selection model: single-select, multi-select (shift-click and/or marquee/rubber-band drag-select), and grouping (select multiple components as one movable/resizable unit). Depends on the state/data model (06) to know what's selectable and how selection state relates to board state.

## Answer

Selection is transient UI/view state — not part of `Board`, never persisted, and out of scope for undo/redo or serialization. It lives entirely in the running app's client state, alongside other view state like zoom/pan.

**Ad-hoc multi-select** (shift-click and/or marquee): a set of selected component instance IDs, held as transient state. Marquee/rubber-band drag-select uses **intersection** semantics — any component overlapping the drag rectangle is selected, not only fully-contained ones. Shift-click **toggles** membership (clicking an already-selected component again removes it). Clicking empty canvas clears the current selection. An ad-hoc multi-select supports both **move** and **resize** as one bounding-box unit — the same interaction surface a `Group` gets, computed transiently rather than backed by a persisted entity.

**Grouping**: an explicit "group" action, given 2+ ad-hoc-selected components, creates a persistent `Group` entity (per ADR 0003 / ticket 06) from the current selection. Once a `Group` exists, clicking any one of its members selects the whole group as a unit — selection and group membership converge at that point. How a user "enters" a group to select/edit an individual member (e.g. double-click-to-enter vs. requiring ungroup first) is **deferred to implementation time** — explicitly left open pending hands-on feel.

**Accessibility baseline**: selected `ComponentContainer`(s) get `aria-selected` applied now, consistent with ADR 0001's pattern of `ComponentContainer` auto-applying ARIA attributes. Full keyboard-driven selection (tab order, arrow-key nudge, keyboard-triggered marquee) remains deferred to the "Full keyboard-accessibility interaction" fog item.

**Considered and left open:**
- Full-containment marquee semantics — rejected in favor of intersection; matches common whiteboard-tool defaults and is less fiddly for the user to draw precisely around small/dense components.
- Group-entry interaction (double-click vs ungroup-only) — not resolved; punted to implementation.

Seeded ADR [0006](../../../docs/adr/0006-selection-model.md) and a new `CONTEXT.md` term (`Selection`). Graduates two fog patches into new tickets: [Tool modes & interaction details](14-tool-modes-interaction-details.md) and [Edge attachment to whole Group](15-edge-group-attachment.md).
