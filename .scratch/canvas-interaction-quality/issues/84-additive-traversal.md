# 84 — Additive traversal and Ctrl+Tab removal

**What to build:** A keyboard user builds a multi-selection without a chord the browser steals (ADR 0059). Space on a focused tab stop adds it to the selection and starts `Additive traversal`; inside it Tab and Shift+Tab move focus without selecting and Space toggles the focused stop. The mode ends on Escape (selection kept), a pointer press, focus leaving the container, Enter on a group stop, or a command handing focus to a named target. A focused but unselected stop draws a dashed accent outline through `:focus-visible:not([aria-selected="true"])`, and there is no other indicator and no live region. Ctrl+Tab goes: the browser consumes it before the page sees it in Chrome and Edge on Windows, so a binding that looks like it works is removed along with its handler, its focus-suppression flag and the test that invoked the handler directly.

**Blocked by:** 73 (One shortcut table behind one focus guard)

**Status:** ready-for-agent

- [ ] Space on a focused shape selects it and starts the mode; Tab then moves focus to the next stop without changing the selection; Space adds it; Space again removes it
- [ ] Escape ends the mode and keeps the selection; a pointer press ends it; focus leaving the canvas ends it
- [ ] A focused unselected stop shows the dashed outline and a focused selected stop shows the selection outline
- [ ] Ctrl+Tab does nothing the canvas owns; the handler, the suppress flag and the old test are gone
- [ ] Escape's staging is updated: cancel gesture, then end port picking or placement, then end additive traversal, then step out of the entered group, then clear the selection
- [ ] bUnit covers the mode through the entry points and the keydown table; a markup test asserts the outline rule
- [ ] A visual baseline shows a focused unselected stop; full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [ ] `CONTEXT.md`'s `Additive traversal` term describes what shipped
