# Remembered-offset duplicate chaining

Type: grilling
Status: resolved
Blocked by: 05

## Question

Decide whether `Ctrl+D` replays the last move's offset instead of ADR 0013's flat `+20,+20`.

Graduated from the map's fog now that ADR 0020 supplies the mechanism it was waiting on. ADR 0013 took a fixed cascade offset; ticket 03's teardown found the better behaviour is tldraw's, which remembers the last Alt-drag clone's offset in `duplicateProps` and replays it on each `Ctrl+D`, chaining so a third press continues the run, and invalidating on selection change. Figma does the same and repeats rotation with it. The principle the teardown named as worth stealing: *you can get the ergonomics of a mode by remembering a gesture's parameters, without entering one.*

The blocker was a reliable "a move gesture just committed, by this delta" signal, which ticket 04 proved does not exist today — a drag released past the clip edge never fires `OnMoved` at all. ADR 0020 provides it exactly: commitment moves onto a canvas-owned gesture that commits once, at release, writing the `Gesture preview` verbatim, so the delta is not merely available but *is* the number the user saw.

Decide:

- **What the remembered offset is a property of.** tldraw hangs it on the selection; the alternative is one canvas-scoped slot. Interacts with ADR 0013's cascade rule (successive pastes onto an unchanged `Paste anchor` cascade, a changed anchor resets), which is a second remembered thing with its own invalidation — decide whether they are one concept or two.
- **Which gestures feed it.** A clone drag is the obvious source, but ADR 0020 makes every `MoveSelection` publish an exact delta. A plain move feeding it means `Ctrl+D` after nudging a shape aside duplicates along that vector, which may read as spooky rather than helpful.
- **Whether snap interacts.** ADR 0020 snaps the preview per tick, so a remembered offset is already grid-aligned when snap is on and arbitrary when off — replaying an arbitrary offset repeatedly accumulates drift the user never chose.
- **What invalidates it.** Selection change is tldraw's answer; also candidate are an undo, a paste, and a board reload.
- **Whether rotation-style parameter replay is foreclosed.** Rotation is out of scope for this map, so decide only that the shape does not preclude it.

Ships against ADR 0013, so it amends or supersedes that decision's offset rule rather than sitting beside it.

## Answer

**Read the offset from the board, not from a gesture.** When the selection is exactly what the last duplicate produced, the next duplicate lands at that selection's offset from its own source. Named a `Duplicate run`. Recorded as [ADR 0039](../../../docs/adr/0039-duplicate-run.md).

- **The ticket's source did not exist.** tldraw replays the last Alt-drag clone's delta, and Alt-drag is [Alt-drag to duplicate](34-alt-drag-duplicate.md), which was blocked on this ticket. Taking the tldraw shape would have left `Ctrl+D` with nothing to replay, or made every plain move feed it. Figma's and PowerPoint's rule was taken instead: the offset is computed when the key is pressed, from committed bounds. So no gesture feeds it, the "spooky" plain-move case cannot happen, and ADR 0020's commit signal is not a dependency.
- **What it is a property of:** the selection being exactly the last duplicate's output, both sets (ADR 0037). The run holds the produced ids and the source's selection bounds as captured, not a reference to the source.
- **Separate from the paste cascade.** The cascade measures from an absolute point, a run from a source entity. They share only `+20, +20`, and neither reads nor resets the other. A pasted set does not start a run.
- **Snap:** the offset is replayed verbatim, never snapped. The ticket's drift worry has it backwards: a verbatim replay never rounds, and snapping against the zoom-dependent `DominantGridSpacing()` is what would make gaps uneven.
- **Invalidation:** only a selection change. Undo, redo and paste break a run only through the selection. Transient, not persisted, not in history, reset by a reload or `Board` swap.
- **Reference point:** top-left of the selection bounds, matching ADR 0013's rigid-body paste. Resizing the copy first can make the next one overlap it, accepted.
- **Sources:** `Ctrl+D`, the menu's Duplicate row, and Alt-drag if it ships. `Quick create` does not start a run, even though it shares the duplication path: `Ctrl+D` continuing its chain without an edge would be a near-duplicate gesture.
- **Rotation:** not foreclosed. The run replays a difference of placements, and placement can gain an angle.

Amends ADR 0013. `Duplicate run` added to `CONTEXT.md`. One item added to the fog: whether the first `+20, +20` step should follow `DominantGridSpacing()` at far zoom.
