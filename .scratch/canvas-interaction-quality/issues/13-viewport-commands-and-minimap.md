# Viewport commands and minimap

Type: grilling
Status: open

## Question

Design the means of not getting lost on an unbounded canvas: zoom-to-fit, zoom-to-selection, and a minimap.

ADR 0011 made board extent and zoom unbounded in both directions, replacing the old fixed 3000×3000 box and 0.6×–6× clamps. That removed a real constraint and introduced a real hazard — there is currently no way to recover a view once panned or zoomed away from content, and no overview of where content sits. `CONTEXT.md` already names a minimap as a prospective canvas chrome component, so the seam ADR 0002 defines is expected to carry it.

Decide:

- **Zoom-to-fit and zoom-to-selection**: framing margin, whether zoom is clamped for a tiny or single selection (fitting one sticky note to the viewport would be absurd), and behaviour on an empty board.
- **Zoom-to-100%** and whether a "reset view" distinct from fit is worth having.
- Whether these animate. A jump is disorienting on a large canvas; an animated transition needs an interpolation home, and `ZoomPanTracker` currently holds no notion of time.
- **Minimap**: whether it is in scope for this effort at all, or belongs in the fog until the rest lands. If in scope — what it renders (real components, LOD placeholders per ADR 0011, or plain boxes), whether it is interactive (click-to-jump, drag-the-viewport-rect), how it bounds itself over content that is genuinely unbounded, and its own render cost against a dense board.
- Where these are triggered from: keyboard shortcuts (16), a chrome control, or both. If a control, whether it is a new component or joins something existing.
- Whether any of this is board state or purely view state. ADR 0003 holds zoom/pan as unpersisted view state; a saved "home view" would contradict that, so either it is out or ADR 0003's boundary needs an explicit exception — and ADR 0003 is settled.
