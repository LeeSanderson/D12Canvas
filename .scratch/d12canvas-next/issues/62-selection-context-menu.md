# 62 — Selection context menu

**What to build:** An end user right-clicks a selection and gets a context menu mirroring the baseline shortcut actions — delete, group/ungroup (as applicable), and the four layering commands — invoking exactly the same commands the shortcuts do. Right-clicking empty canvas shows no custom menu. The menu is keyboard-operable. (ADR 0009.)

**Blocked by:** 34 (Delete selection), 44 (Group/ungroup lifecycle), 60 (ZIndex commands + new-on-top)

**Status:** resolved

- [x] Right-click on a selection opens the menu with delete, group/ungroup, and layering actions
- [x] Group appears only for 2+ selections; ungroup only when a group is selected
- [x] Menu actions invoke the same undoable commands as their shortcuts
- [x] No custom menu appears on empty canvas
- [x] The menu is keyboard-navigable and dismisses on Escape
- [x] Screenshot case for the open menu

## Comments

New `SelectionContextMenu` component (`D12Canvas/SelectionContextMenu.razor(.cs/.js)`), rendered by
`DiagramCanvas` as a sibling of `.diagram-canvas` (canvas chrome per ADR 0002 - it must not pan/zoom
with board content), anchored at plain container-relative pixels computed from the `oncontextmenu`
event's `ClientX/ClientY` minus a freshly-fetched container origin (the same "container can move on
the page" reasoning `HandleMouseDown`/`HandleDrop` already rely on).

The menu itself carries zero Board/Selection knowledge - `DiagramCanvas` computes eligibility
(`HasContextMenuEligibleSelection`, `CanGroupSelection`, `CanUngroupSelection` - the last two now
also power `OnGroupPressed`/`OnUngroupPressed`'s own guards, removing a duplicate check) and wires
every menu item's callback directly to the same `OnXPressed` method its keyboard shortcut calls, so
"the same undoable command" holds by construction rather than by parallel logic. `oncontextmenu`
lives on `.diagram-canvas` itself, gated by `@oncontextmenu:preventDefault="@HasContextMenuEligibleSelection"`
- true only when there's a selection, so an empty-canvas right-click falls through to the browser's
own default menu untouched.

Dismissal has two independent paths: a capture-phase document mousedown listener (the same
click-outside technique `ComponentContainer.razor.js` already uses for exiting inline-edit mode),
and a local `@onkeydown:stopPropagation` Escape handler - stopping propagation there means Escape
closes only the menu without ever reaching `DiagramCanvas`'s window-level shortcut listener (the
same precedent ADR 0009's addendum documents for inline text editing). `OnEscapePressed` itself also
clears the menu defensively, in case focus ever isn't inside it when Escape fires globally.

Keyboard operability is the full ARIA menu/menuitem contract the markup's `role="menu"`/`"menuitem"`
declares, not just native Tab order: ArrowUp/ArrowDown rove focus between the menu's own items
(wrapping at either end, via a small `focusAdjacentItem` JS helper), Enter/Space activate the
focused item (free from being plain `<button>`s), and Escape dismisses.

Two Playwright screenshot cases (`SelectionContextMenuVisualTests.cs`): the open menu on a single
selected instance, and on a 2+ instance selection (Group visible). A third, non-screenshot case pins
the empty-canvas no-menu behaviour. Also covered: right-click on a selected edge offers Delete (the
existing Delete shortcut already handles edges, so the menu does too, for the same consistency
reason it reuses every other shortcut's method) - layering buttons still render for an edge selection
but are harmless no-ops there, exactly matching the keyboard shortcuts' own existing behaviour.

28 new bUnit tests (`SelectionContextMenuTests.cs`, `DiagramCanvasContextMenuTests.cs`) plus the 3
Playwright cases above; full bUnit suite (557 tests, 1 pre-existing skip) and full solution build are
both clean. The full Playwright suite has 9 pre-existing flaky failures (marquee/multi-select/
z-index-layering pixel and scoped-CSS-hash nondeterminism, tickets 78/79/81) reproduced identically
on a clean `main` with none of this ticket's changes present - confirmed unrelated before relying on
a clean run of just this ticket's own new test class.
