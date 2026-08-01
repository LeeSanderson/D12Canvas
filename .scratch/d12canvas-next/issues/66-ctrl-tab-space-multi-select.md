# 66 — Ctrl+Tab + Space multi-select

**What to build:** A keyboard user builds a multi-selection without a mouse: Ctrl+Tab moves focus to the next instance *without* collapsing the current selection, and Space toggles the focused instance's membership — the keyboard equivalent of shift-click. `aria-selected` tracks every change. (ADR 0010.)

**Blocked by:** 32 (Marquee + shift-click multi-select), 63 (Tab stops + focus-follows-selection)

**Status:** resolved

- [x] Ctrl+Tab moves focus while preserving the existing selection
- [x] Space toggles the focused instance in/out of the selection
- [x] The resulting multi-selection behaves identically to a pointer-built one (unit move/resize, delete, group)
- [x] `aria-selected` is correct throughout
- [x] bUnit coverage of the focus/selection interplay

## Comments

`_focusedTabStopId` is a new field tracking whichever entity currently has real DOM focus,
independent of `_selectedInstanceIds` - needed because Ctrl+Tab must move focus without touching
selection at all, so `FocusEntity` (ticket 63's "focusing selects" entry point) now sets it on
every focus arrival, before its existing hard-select branch. `OnCtrlTabPressed` computes the next
id itself (via a new `FocusableTabStopIds()` - the same `OrderedTabStops()` order, filtered to
just the ones with a tabindex - grouped members are skipped exactly like native Tab already skips
them) and updates `_focusedTabStopId` directly, rather than waiting for the resulting focus
event's own round-trip - both because a real `.focus()` call's `onfocus` is otherwise
indistinguishable from any other focus arrival (see `_suppressFocusSelect` below) and because it
keeps the C# state deterministic independent of whether the JS side actually moves DOM focus
(this matters for testability - bUnit's stubbed JS module never fires a real focus event at all).
`_suppressFocusSelect` is set right before that JS call fires and consumed by the very next
`FocusEntity` call it triggers, purely defensive for a real browser (where `.focus()` does fire a
genuine `onfocus`) - it has no effect in bUnit, where the stub never round-trips.

Space reuses `SelectComponent(id, addToSelection: true)` verbatim - the exact branch a shift-click
already takes - so a Ctrl+Tab/Space-built selection is byte-for-byte the same
`_selectedInstanceIds` a mouse-built one would produce; every existing consumer (move/resize/
delete/group/z-index, all reading through `ExpandedSelection`) needed no changes at all. Space
no-ops when `_focusedTabStopId` no longer has its own tab stop (deleted since, or grouped into a
member with none) - this one check covers every mutation that could invalidate it, rather than
needing scattered clearing at each call site (delete, group, undo, ...).

Targeting the DOM focus move itself needed no new markup: `focusTabStopAt(container, index)`
(`DiagramCanvas.razor.js`) queries `[tabindex="0"]` within the container and focuses the Nth match
- exactly the set `FocusableTabStopIds()` computes, in the same document order, since a
`ComponentContainer`'s tabindex is already omitted entirely for a grouped member and
`.group-tab-stop` always renders `tabindex="0"` (both from ticket 63). No new attribute, no new
component parameter, and no visual-test impact (confirmed against tickets 64/65's own precedent -
zero rendered markup/style touched, so the Playwright suite wasn't run for this ticket either).

Space is guarded by the existing `isEditableTarget` check (typing a literal space character during
inline WYSIWYG editing must never be hijacked) and unconditionally `preventDefault`s otherwise -
a focused non-form-control `<div>` would otherwise scroll the page on every press, which is not
what Space means here. Ctrl+Tab only intercepts the exact chord - `Ctrl+Shift+Tab` is left
untouched rather than treated as a synonym for plain `Ctrl+Tab`, since there's no reverse
traversal implemented for it (unlike every other modifier-branching chord in this same listener);
plain Tab still falls through untouched to native browser traversal, same as ticket 63 already
established.

`OnCtrlTabPressed` no-ops (no JS call, no field writes) when the only stop it could move to is the
one already focused - otherwise, with exactly one focusable stop on the board, it would still set
`_suppressFocusSelect` before a JS `.focus()` call that (already being the focused element) fires
no new `onfocus` in a real browser, leaving that flag stuck and silently swallowing the hard-select
a later, unrelated focus arrival should have performed.

New coverage: `DiagramCanvasCtrlTabSpaceMultiSelectTests.cs` (bUnit) - focus-without-select,
wraparound at both ends, repeated Ctrl+Tab with nothing initially focused, the single-stop no-op,
Space adding/removing from an existing (partly mouse-built) selection, a no-op Space with nothing
focused, a Ctrl+Tab/Space-built selection moving/deleting/grouping identically to a pointer-built
one, and Space landing on a persisted Group's own tab stop toggling the whole group.
