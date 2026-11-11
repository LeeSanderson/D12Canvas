# Board sketch component

Status: resolved
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

- [x] Given a board with components and edges, the sketch draws one rectangle per component and one line per edge.
- [x] Sticky notes draw with sticky-note styling and other components as outlines.
- [x] An edge with a floating end draws to that end's point.
- [x] Edge-label components draw nothing.
- [x] The `viewBox` covers exactly the union of the component bounds.
- [x] An empty board shows "Empty board".
- [x] The sketch uses only `--d12-*` tokens and changes with `data-d12-theme`.
- [x] bUnit tests in `D12Canvas.Tests` cover each of the above.
- [x] The full Playwright visual suite runs in the pinned Docker image with `-parallel none`, and the new baseline(s) for the Demo sketch are reviewed and folded in, per `docs/agents/testing.md`.

## Comments

Resolved 2026-11-11. `BoardSketch` is a public library component with a required `Board` parameter. It finds sticky notes through `IComponentRegistry`: any key registered with `ComponentType == typeof(StickyNote)` draws in note colours, and an unknown key draws as an outline. Each attached edge end goes to its component's centre and a floating end to its own point. An edge whose component is missing is dropped. Boxes are drawn in `ZIndex` order on top of the lines and filled with `--d12-surface`, so a line stops at a box's edge. Edge labels are never drawn, because they live on `Edge.Label` and not in `Board.Components`.

The root `.d12-board-sketch` declares the raised values through `RaisedSurface.TokenRules`, plus two new tokens, `--d12-sketch-note` and `--d12-sketch-note-border`, each with a dark value. The sketch has no background of its own, so the host's card shows through. It fills its container with 8% padding.

A new demo page, `/board-sketch-demo`, backs `BoardSketchVisualTests`: one board and one empty board, each under a light and a dark `data-d12-theme`. The full visual suite ran in the pinned image with `-parallel none`: 317 tests, and the only failure was the new baseline, which was checked and folded in. No existing baseline moved.
