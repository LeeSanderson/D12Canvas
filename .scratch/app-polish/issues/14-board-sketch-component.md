# Board sketch component

Status: ready-for-agent
Blocked by:

Spec: [App polish](../spec.md), "Board sketch (library)". Decision record: [Board list page](09-board-list-page.md), point 7.

## What to build

A public library component that takes a `Board` and draws a small picture of it as an `<svg>`. A host can use it for board previews; the App's board list will put one on each card.

- One rectangle per component from its `Bounds`. Sticky notes get sticky-note styling, found by their component type (not by searching the type key). Everything else is an outline.
- One line per edge between the centres of the components it connects. A floating end uses its own point. Edge-label components are skipped.
- The `viewBox` is the union of the component bounds, so any board scales to fit its container. Lines use `vector-effect="non-scaling-stroke"` so they stay thin.
- A board with no components shows "Empty board".
- Colours come from `--d12-*` tokens, so the sketch follows `data-d12-theme`.

The prototype's `PrototypeBoardSketch` on the `prototype/board-list` branch shows the shape, but it is App code using `--bs-*` colours and type-key searching. Don't copy those parts.

Add the sketch to the Demo so it can be checked by eye and covered by a visual baseline.

## Acceptance criteria

- [ ] Given a board with components and edges, the sketch draws one rectangle per component and one line per edge.
- [ ] Sticky notes draw with sticky-note styling and other components as outlines.
- [ ] An edge with a floating end draws to that end's point.
- [ ] Edge-label components draw nothing.
- [ ] The `viewBox` covers exactly the union of the component bounds.
- [ ] An empty board shows "Empty board".
- [ ] The sketch uses only `--d12-*` tokens and changes with `data-d12-theme`.
- [ ] bUnit tests in `D12Canvas.Tests` cover each of the above.
- [ ] The full Playwright visual suite runs in the pinned Docker image with `-parallel none`, and the new baseline(s) for the Demo sketch are reviewed and folded in, per `docs/agents/testing.md`.
