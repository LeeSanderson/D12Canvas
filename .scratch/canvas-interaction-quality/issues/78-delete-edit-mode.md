# 78 — Delete edit mode and the legacy container plumbing

**What to build:** A `ComponentContainer` has one rendering, and `Selection` alone decides whether its affordances appear (ADR 0035). This is the contract step of the spine: edit mode (`InitialEditMode`, `OnStateChanged`, `ComponentContainerStateChangedEventArgs`, the click-outside JavaScript pair, the `edit-mode` and `view-mode` classes, the container's own JavaScript module and `DisposeAsync`), the container demo page and its nav row are deleted. Every remaining mouse binding on board content goes, and the listener's allowance from ticket 71 for roles served by old handlers is removed, so every press is classified and owned. A container outside a `Board` is a positioned box with content and nothing more.

**Blocked by:** 75 (ResizeSelection), 76 (DragEdgeEnd and SelectEdge)

**Status:** ready-for-agent

- [ ] The container has no mouse, click, double-click or focus bindings of its own and no JavaScript module; its public surface loses the three edit-mode members
- [ ] The container demo page and its nav entry are gone; the deleted nav row moves every existing `.verified.html` and `.png`, which is planned churn
- [ ] The pointer listener has no per-role allowance; every one of the eleven roles reaches C#
- [ ] The container's `ShouldRender` no longer compares edit-mode state
- [ ] Container tests that drove edit mode or the click-outside callback are deleted or rewritten; the test base no longer mocks the container module
- [ ] Full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [ ] `CONTEXT.md`'s `Component container` term describes what shipped
