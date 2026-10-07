# 78 — Delete edit mode and the legacy container plumbing

**What to build:** A `ComponentContainer` has one rendering, and `Selection` alone decides whether its affordances appear (ADR 0035). This is the contract step of the spine: edit mode (`InitialEditMode`, `OnStateChanged`, `ComponentContainerStateChangedEventArgs`, the click-outside JavaScript pair, the `edit-mode` and `view-mode` classes, the container's own JavaScript module and `DisposeAsync`), the container demo page and its nav row are deleted. Every remaining mouse binding on board content goes, and the listener's allowance from ticket 71 for roles served by old handlers is removed, so every press is classified and owned. A container outside a `Board` is a positioned box with content and nothing more.

**Blocked by:** 75 (ResizeSelection), 76 (DragEdgeEnd and SelectEdge)

**Status:** resolved

- [x] The container has no mouse, click or double-click bindings and no JavaScript module, and keeps only the tab stop's `@onfocus` that ADR 0036 names as the one route by which focus selects; its public surface loses the three edit-mode members
- [x] The container demo page and its nav entry are gone; the deleted nav row moves every existing `.verified.html` and `.png`, which is planned churn
- [x] The pointer listener has no per-role allowance; every one of the eleven roles reaches C#
- [x] The container's `ShouldRender` no longer compares edit-mode state
- [x] Container tests that drove edit mode or the click-outside callback are deleted or rewritten; the test base no longer mocks the container module
- [x] Full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [x] `CONTEXT.md`'s `Component container` term describes what shipped

## Comments

The pointer listener's per-role allowance had already gone with ticket 76, so no listener change was needed. The `classify` option on `addPointerListener` stays because ADR 0018 reserves `classify: false` for the minimap.

All 56 `.verified.html` files moved for the nav row, the `view-mode` class and the merged style rule. 54 `.png` baselines moved for the nav row, the change ADR 0035 predicted. Twelve of them also differ by one channel step on an antialiased palette corner, well inside the comparer's tolerance.
