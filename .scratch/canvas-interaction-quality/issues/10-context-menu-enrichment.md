# Context menu enrichment

Type: grilling
Status: open
Blocked by: 07, 09

## Question

Decide the full action set the right-click menu offers, and in which contexts it appears.

The seed note asks for actions via a right-click menu — group/ungroup when several items are selected, duplicate, delete. Roughly half of that exists already: `SelectionContextMenu` is built, themed, keyboard-navigable, and wired into `DiagramCanvas`, offering Delete, conditional Group and Ungroup, and the four z-order commands. Duplicate is absent because no duplication model existed (ticket 09), and the menu never appears on empty canvas because `HandleContextMenu` returns early when nothing is selected, deliberately letting the browser's native menu through.

Decide:

- The action set, now that clipboard and duplication (09) have a model to draw on: cut, copy, paste, duplicate, select-all.
- Whether an empty-canvas menu exists, and what it holds — paste and select-all are the obvious candidates, and both only make sense after 09. This also settles whether the browser's native menu should ever be reachable inside the canvas. **ADR 0015 has since retired the premise that blocked this**: ADR 0009 justified having no empty-canvas menu on the grounds that no decided action belonged there, and the viewport commands (plus ADR 0011's snap-to-grid toggle) now do. So the actions exist and are waiting; this ticket decides whether they appear and alongside what. Note the asymmetry it introduces — zoom-to-fit is meaningful on empty canvas *and* on a selection, whereas paste and select-all are empty-canvas-flavoured, so a viewport section may belong in both menus rather than only the empty one.
- Whether align and distribute (12) appear here, and how a menu with two conditional sections and a growing list stays readable. Submenus, grouping, or a hard cap.
- Context sensitivity: a single instance, a multi-selection, a `Group`, a selected edge, and empty canvas are five distinct cases with different applicable actions. Today's conditional rendering handles two.
- Whether menu items surface their keyboard shortcuts, which requires the shortcut table from 09 and 16 to be settled first.
- How this composes with the selection-anchored property bar (08). Both are popovers anchored near the selection; decide whether they can be open simultaneously, and which owns dismissal.
- Disclosure of unavailable actions: hidden, as Group and Ungroup are today, or shown disabled. The current pattern makes the menu's height jump between selections.

Ticket 07 owns the *trigger* — the press semantics that decide when a right-button gesture opens this menu rather than panning. This ticket owns only what the menu contains.
