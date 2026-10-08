# 84 — Additive traversal and Ctrl+Tab removal

**What to build:** A keyboard user builds a multi-selection without a chord the browser steals (ADR 0059). Space on a focused tab stop adds it to the selection and starts `Additive traversal`; inside it Tab and Shift+Tab move focus without selecting and Space toggles the focused stop. The mode ends on Escape (selection kept), a pointer press, focus leaving the container, Enter on a group stop, or a command handing focus to a named target. A focused but unselected stop draws a dashed accent outline through `:focus-visible:not([aria-selected="true"])`, and there is no other indicator and no live region. Ctrl+Tab goes: the browser consumes it before the page sees it in Chrome and Edge on Windows, so a binding that looks like it works is removed along with its handler, its focus-suppression flag and the test that invoked the handler directly.

**Blocked by:** 73 (One shortcut table behind one focus guard)

**Status:** resolved

- [x] Space on a focused shape selects it and starts the mode; Tab then moves focus to the next stop without changing the selection; Space adds it; Space again removes it
- [x] Escape ends the mode and keeps the selection; a pointer press ends it; focus leaving the canvas ends it
- [x] A focused unselected stop shows the dashed outline and a focused selected stop shows the selection outline
- [x] Ctrl+Tab does nothing the canvas owns; the handler, the suppress flag and the old test are gone
- [x] Escape's staging is updated: cancel gesture, then end port picking or placement, then end additive traversal, then step out of the entered group, then clear the selection
- [x] bUnit covers the mode through the entry points and the keydown table; a markup test asserts the outline rule
- [x] A visual baseline shows a focused unselected stop; full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [x] `CONTEXT.md`'s `Additive traversal` term describes what shipped

Shipped with three choices the ADR did not spell out. A keyboard-focused selected shape used to lose its selection outline, because `.component-container:focus { outline: none }` came after `.component-container.selected` in the same style block; the two rules swap, so a focused selected stop shows the same outline a pointer-selected one does, and four baselines with a click-to-added shape moved for it. Focus leaving the container is read one tick after `focusout`, and counts only when the page still has focus, the stop that lost it is still in the DOM, and focus has landed outside the container, so a window blur or a deleted focused stop leaves the mode on. A focus landing outside the entered group inside the mode steps out of the group the way a selecting landing does, without selecting the stop.

Still open: keyboard port placement (ticket 104), `Quick create` (103) and `F2` (101) do not exist yet, so Escape's placement stage and those handoffs have nothing to hook; each ends the mode through `_pendingFocusId` if it hands focus that way. Escape with the context menu open still closes the menu in the same press as whichever stage runs, as before.
