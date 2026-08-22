# Alignment guides and object snapping

Type: prototype
Status: open
Blocked by: 05

## Question

Design snap-lines to neighbouring objects during a drag or resize — the single largest gap between the current canvas and a tool that feels professional to lay out with.

Nothing of this exists today. The only snapping is `SnapToGrid` (ADR 0011), an off-by-default toggle that snaps to whichever adaptive grid layer is currently dominant, wired into `NewCenteredInstance` and the single-instance branch of `MoveComponent` only.

Build a prototype and decide:

- Which anchors participate: edges, centres, both. Whether equal-spacing detection between three or more objects is in scope, or only pairwise alignment.
- Snap tolerance, and whether it is measured in screen pixels or board units. Screen pixels keep the feel constant across zoom, which matters a great deal on ADR 0011's unbounded zoom range; board units keep behaviour reproducible. These genuinely conflict.
- Which candidates are considered. Scanning every instance is O(n) per mousemove on top of the live-geometry render cost from ticket 05. `Board.GetVisible(viewport, overscan)` already exists and bounds the set to what is on screen — decide whether that is sufficient, and what happens with a large selection dragged across a dense board.
- Composition with `SnapToGrid`. Both active at once, one suppressing the other, or a combined precedence. Ticket 72 of `d12canvas-next` deliberately kept grid snapping out of multi-selection and group moves because a shared delta snapped per-member would break `CommitGroupMove`'s relative-offset guarantee — object snapping faces the identical problem and cannot dodge it, since aligning a multi-selection is exactly the case users want.
- Whether it applies to resize as well as move, and to edge endpoints.
- Rendering: guides are chrome drawn in board space, appearing and disappearing per frame. Which layer, and how they read against both light and dark themes (ADR 0012's token set).
- A suppression modifier, so a user can place something deliberately unaligned.
- Whether guides are purely visual or actually alter the committed position, and how that lands in ADR 0007's gesture-level history.

**This ticket now owns the whole constrain-modifier vocabulary, not just its own suppression key** (added while resolving ADR 0022, which deliberately left the pointer's modifiers almost entirely unbound so they could be decided together rather than in three places). Three claimants, one budget:

- **Snap suppression**, this ticket's own. Ticket 03 found Ctrl is the "escape from automatic help" key in all four reference tools, and that it **inverts** rather than merely suppresses in both open-source ones. Ctrl is unbound on the pointer today precisely so this can take it.
- **Axis-locked movement** — constraining a drag to whichever axis moved further, which Excalidraw binds to `Shift`. ADR 0022 confirmed it composes with `Shift`'s selection meaning without a clash (below the 4-pixel drag threshold the press is a selection toggle, above it the `Shift` reads as a constraint and the toggle never happens), so the mechanism is free. It was routed here rather than bound there because axis-lock and snapping answer the same user question, and deciding them apart risks two "help me be precise" modifiers that were never designed together.
- **Pressing through content.** Not a constraint, but it wants the same key. A selection band can only start on empty canvas, and ADR 0017 widened that hole by making the multi-selection box a solid hit target that consumes presses aimed beneath it. tldraw turns an accel-drag on a shape into a band; Excalidraw refuses to drag under it. Both exist because on a dense board the thing under your pointer is usually not the thing you want to grab. ADR 0022 rejected it *only* because Ctrl had the stronger claimant here — so if snapping ends up wanting a different key, this is next in line.

One trap that binds whichever of these takes Ctrl: **Ctrl+click is the macOS secondary click.** `DiagramCanvas.razor.js` uses `(event.ctrlKey || event.metaKey)` throughout, which is correct for keyboard shortcuts and wrong for a pointer modifier — a pointer-path platform check is needed, the same shape of split ADR 0018 already made for its two target predicates. Note also that ADR 0019 gives Ctrl+wheel a zoom meaning on the trackpad profile, and ticket 03 flagged Excalidraw's five-way overloading of Ctrl as "a warning, not a template".

Whatever this lands on, [Latched-versus-live modifier semantics](29-latched-versus-live-modifiers.md) then decides per modifier whether it is read once at press or live on every move.
