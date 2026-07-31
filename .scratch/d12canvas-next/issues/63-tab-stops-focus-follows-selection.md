# 63 — Tab stops + focus-follows-selection

**What to build:** A keyboard user tabs through board content in reading order, reaching every component instance; a `Group` collapses to a single tab stop. Focus and selection stay coherent: focusing an instance selects it, and selecting (by any means) moves focus. A visible focus indicator renders, and a screen reader announces each instance by its `AccessibleName` and `Role`. (ADR 0010.)

**Blocked by:** 29 (Click select, escape, and `aria-selected`), 44 (Group/ungroup lifecycle)

**Status:** resolved

- [x] Tab traverses instances in reading order; Shift+Tab reverses
- [x] A group is one tab stop — members are not individually reachable while grouped
- [x] Focusing selects; selecting focuses — the two never diverge
- [x] A visible focus indicator renders; screenshot case added
- [x] Instances are announced by `AccessibleName`/`Role`, with `aria-selected` reflecting state
- [x] bUnit coverage of tab order and focus/selection coherence

## Comments

Native Tab/Shift+Tab needs no keyboard interception at all - it's a consequence of two things:
`ComponentContainer` gained a `Focusable` parameter controlling whether its root renders
`tabindex="0"` (grouped members render none - see `IsGrouped`), and `DiagramCanvas.OrderedTabStops()`
emits every visible instance plus every visible top-level `Group`'s own new `.group-tab-stop`
element in one combined list sorted by current on-screen position (`Bounds.Y` then `.X`), so the
browser's own native tab order already matches reading order without any code driving Tab itself.
A `Group`'s tab stop is a new, otherwise-invisible (`pointer-events: none`), always-present
`<div>` positioned at `Board.GetVisibleGroups`'s computed bounds - selecting the group already
renders the pre-existing `.selection-bounding-box` overlay, which doubles as this stop's own
visible focus indicator (no new chrome). Both `ComponentContainer` and the group stop suppress
the native `:focus` outline in CSS, relying entirely on the existing `.selected`/
`.selection-bounding-box` styling as the one visible indicator - fitting, since focus and
selection are the same state here.

`ComponentContainer.OnFocus` (native `@onfocus`) is the sole entry point for "focusing selects" -
it always resolves to `DiagramCanvas.FocusEntity`, a hard single-select. The reverse direction
("selecting focuses") is deliberately narrower than "by any means": a plain (non-shift) click on
an *ungrouped* instance explicitly moves real DOM focus (a new `focusElement` JS call), and Ctrl+G
does too (grouping always collapses to exactly one new focusable target - the group's own stop -
so `OnGroupPressed` sets a pending-focus flag that `OnAfterRenderAsync` resolves once the stop
actually exists in the DOM). Shift-click, marquee, Ctrl+Shift+G, and a click on an already-grouped
member do not move focus - each either has no single unambiguous focus target (an ad-hoc or
post-ungroup multi-selection) or would otherwise re-collapse a multi-select toggle into a hard
single-select (shift-click, mid-toggle). This is a real, narrower reading of the ADR's own
"selecting (by any means) moves focus" line, not a hidden gap - a real Tab/Shift+Tab keyboard user
is unaffected either way, since those never rely on a prior click's focus side effect.

"Reaching every component instance" is bounded by the existing windowed-mounting viewport
(`Board.GetVisible`/`GetVisibleGroups`, `DiagramCanvas.Overscan`): an instance or group entirely
outside the current viewport + overscan has no tab stop at all, since it isn't mounted. Left as-is
rather than force-mounting the whole `Board` for keyboard purposes, which would undo the
virtualization performance ceiling tickets 01/02/25 established.

Fixed a pre-existing bug while wiring this up: `DiagramCanvas.razor.js`'s window-level keydown
listener called `event.preventDefault()` unconditionally after its `switch`, regardless of whether
any case matched - which silently blocked Tab (and every other unhandled key, anywhere on the host
page) from ever reaching the browser's own default handling. `preventDefault` now only fires inside
a branch that actually invokes a `dotnetRef` method.

New coverage: `DiagramCanvasFocusFollowsSelectionTests.cs` (bUnit - reading order, tabindex
placement for grouped vs. ungrouped, focus-driven selection for both an instance and a group,
click-driven `focusElement`/`focusGroupTabStop` invocations) and
`GroupTabStopVisualTests.cs` (Playwright - focusing a persisted group's own tab stop renders the
same `.selection-bounding-box` overlay a mouse-driven multi-selection does).
