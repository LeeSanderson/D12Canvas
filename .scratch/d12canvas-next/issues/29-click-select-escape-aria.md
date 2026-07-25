# 29 — Click select, escape, and `aria-selected`

**What to build:** An end user clicks a component instance to select it: a visible selection affordance appears, `aria-selected` is exposed for assistive tech, and Escape (or clicking empty canvas) clears the selection. Selection is transient view state — never on the `Board`, never persisted, never in history. (ADR 0006.)

**Blocked by:** 22 (Canvas renders a Board)

**Status:** resolved

- [x] Clicking an instance selects it; clicking empty canvas or pressing Escape clears the selection
- [x] A visible selection affordance renders on the selected instance; screenshot case added
- [x] `aria-selected` is set on the selected instance and absent/false otherwise
- [x] Selection state lives outside the `Board` and is never serialized
- [x] bUnit coverage of select/clear behaviour and ARIA state

## Comments

Selection is a single `Guid? _selectedInstanceId` field on `DiagramCanvas` (per ADR 0006 - transient view state, never on `Board`), plus `IsSelected(Guid)`/`SelectComponent(Guid)` helpers the markup binds per-instance. Only single-select is in scope here; ad-hoc multi-select/marquee is a later ticket.

`ComponentContainer` gains `IsSelected`/`OnSelect` parameters: a plain `@onclick` (alongside the pre-existing `@onclick:stopPropagation`, which keeps the click from also reaching the canvas background) invokes `OnSelect`, and `IsSelected` renders both the `aria-selected` attribute (omitted, not `"false"`, when unselected - matching the "absent/false" latitude in the ticket) and a `.selected` CSS class (a blue outline, visually distinct from the pre-existing always-on resize-handle dots). `IsSelected` was also added to `ShouldRender`'s `_lastRendered*` tracking - without that, the skip-render optimization from ticket 25 would suppress the very re-render selection depends on.

`DiagramCanvas` clears selection two ways: `HandleCanvasClick` (a plain `@onclick` on `.diagram-canvas`, which never fires for a click that landed on a `ComponentContainer` since that element stops propagation) and `OnEscapePressed` (a new `[JSInvokable]`, wired to a new `"Escape"` case in `DiagramCanvas.razor.js`'s existing keydown listener, following the same pattern as `OnZoomIn`/`OnPanLeft`/etc.). Per ADR 0009, Escape's remit will later extend to cancelling an in-progress connector drag - no such gesture exists yet, so clearing selection is its whole job today.

One correctness wrinkle needed a guard: a pan drag starts and ends with mousedown/mouseup on the same `.diagram-canvas` element, so the browser's native `click` fires right after a pan completes - without a guard, panning after selecting something would immediately clear the selection as an unintended side effect of "click empty canvas clears selection." A `_panMoved` field (set on drag, checked and reset in `HandleCanvasClick`) suppresses exactly that one click.

bUnit coverage: `DiagramCanvasSelectionTests.cs` (unselected-by-default, click-to-select with aria-selected/class assertions, selecting a second instance moves selection off the first, click-empty-canvas clears, Escape clears, pan-then-click preserves selection) plus three new cases in `ComponentContainerTests.cs` exercising `IsSelected`/`OnSelect` directly.

New Playwright visual coverage (`SelectionVisualTests.cs`, reusing `/placement-demo`): click-to-add then click-to-select, asserting `aria-selected="true"` before the screenshot. Baseline captured via the pinned `mcr.microsoft.com/playwright/dotnet:v1.61.0-noble` Docker image per the README process, not the local host (font/scoped-CSS rendering differs enough to produce false-positive diffs). Note: the *other* pre-existing visual-test baselines in this repo currently fail even on a clean checkout inside that same pinned image - a pre-existing environment/baseline-drift issue unrelated to this ticket, not introduced or fixed here.
