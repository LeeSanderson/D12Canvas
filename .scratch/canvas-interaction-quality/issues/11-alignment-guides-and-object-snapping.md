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
