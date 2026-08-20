# Remembered-offset duplicate chaining

Type: grilling
Status: open
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
