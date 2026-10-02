# A duplicate of the last duplicate lands where that duplicate stands relative to its source

When the selection is exactly what the last duplicate produced, the next duplicate places its copies at the same offset from that selection as the selection sits from its own source. The first duplicate in a run still offsets by ADR 0013's `+20, +20`. Move the copy anywhere, by any means, press `Ctrl+D` again, and the spacing repeats. This is a **duplicate run**.

This amends ADR 0013, which recorded remembered-offset chaining as "clearly the better interaction" and left it open because it depended on a reliable "a move gesture just committed, by this delta" signal. That signal turns out not to be needed.

## The offset is read from the board, not from a gesture

tldraw hangs the remembered offset on the last Alt-drag clone and replays that gesture's delta. Taking the same shape here has two problems. Alt-drag is not built and may not be, so the feature would have nothing to replay. And if every `MoveSelection` fed the slot instead, `Ctrl+D` after nudging an unrelated shape aside would duplicate along that vector, which nobody would predict.

Figma and PowerPoint answer it differently, and that answer is taken here: the offset is computed when the key is pressed, from committed geometry. The run holds two things, the ids the last duplicate produced and its source's selection bounds as captured at that moment. At the next press, if the selection is still exactly those ids, the offset is the selection bounds' top-left minus the captured top-left.

Three consequences fall out without further rules:

- **Nothing feeds it.** A drag, a snapped drag, a nudge and a resize all count the same, because the run never observes a gesture. Moving a shape that did not come from a duplicate has no effect on anything.
- **ADR 0020's commit signal is not a dependency.** The blocker ADR 0013 named is sidestepped rather than satisfied.
- **Undo needs no rule.** Undoing a move of the copies changes their committed bounds, and the next press follows that. Undoing the duplicate removes the copies, ADR 0013 drops ids that no longer resolve from the selection, and the run is broken because the selection no longer matches.

Continuing a run makes the new copies the next run's output and captures the previous copies' bounds as its source, so a third press continues from the second copy.

## What breaks a run

Only a selection change. The run exists while the selection, instance set and edge set together (ADR 0037), is exactly what the last duplicate produced. Paste breaks it by replacing the selection. Redo of a duplicate does not restore it, because redo does not restore selection.

It is transient UI state held by `DiagramCanvas`, like `Selection` itself (ADR 0006): never persisted, never in history. A board reload or a host swapping `Board` resets it.

The source is held as captured bounds, not as a reference. A host deleting or moving the source from outside leaves the next press with a well-defined offset.

## The offset is measured top-left to top-left

The reference point only matters when the copy is resized before the next press. Top-left of the selection bounds is what Figma uses, and it matches ADR 0013's rigid-body paste, where one delta computed from the payload's own bounding box moves everything. For a multi-entity selection it is the whole selection's top-left, never one member's.

The cost, accepted: widen the copy and the next one lands at the old left-edge spacing, so the two can overlap. Centre-to-centre is no less arbitrary, and a gap-preserving rule needs a separate case per axis and direction and means nothing when copies overlap their source.

## Replayed verbatim, never snapped

With snapping on by default (ADR 0024), replaying an arbitrary offset repeatedly looks like it accumulates drift. It does not: drift comes from rounding at each step, and a verbatim replay does not round, so the nth copy sits at exactly n offsets from the first. Snapping the replay would be what drifts. `DominantGridSpacing()` changes with zoom, so rounding each step against it produces uneven gaps, and zooming between presses moves where the next copy lands.

When the user positioned the copy with grid snapping on and the source was on-grid, the offset is already a whole number of grid steps, and verbatim and snapped agree. They differ only when the user chose off-grid geometry.

## Separate from the paste cascade

ADR 0013's paste cascade counts steps from an absolute point, the `Paste anchor`. A duplicate run measures from a source entity. Merging them would force one to invent the other's reference point. They share only the `+20, +20` constant.

They do not interfere. A paste replaces the selection, so it breaks a run with no extra rule. A duplicate does not move the paste anchor, so the cascade carries on. A pasted set does not start a run: its source is the clipboard's originals, which may be on another board or already deleted.

## What starts a run

The duplicate operation, from `Ctrl+D` or the context menu's Duplicate row (ADR 0023). If Alt-drag duplication ships, its copies are selected and have a known source, so it starts a run with no rule of its own.

`Quick create` (ADR 0030) does not, even though it routes through the duplication path and selects the node it makes. It has its own chain, pressing the new node's side. If it started a run, `Ctrl+D` straight after would place another node at the same gap with no edge, and the user would have to remember which of two nearly identical gestures draws the edge. It shares duplication's id regeneration, not duplicate's placement. `Ctrl+D` after a quick create offsets `+20, +20` from the new node.

## Rotation is not foreclosed

Rotation is out of scope for this effort. The run holds a captured placement and replays the difference between the output's placement and that capture. Today placement is the bounding box's top-left. If placement ever gains an angle, the same subtraction repeats a turn the way Figma's does, and the shape of the run does not change.

## Considered and rejected

- **Replaying the last gesture's delta**, tldraw's shape. Its only good source is a clone drag, which does not exist here, and every other source makes `Ctrl+D` depend on an unrelated earlier move.
- **One canvas-scoped offset slot** shared by duplicate and paste. The two measure from different reference points.
- **Snapping the replayed position.** Produces the drift it is meant to prevent, and makes placement depend on zoom.
- **Centre or gap as the reference point.** Neither is more predictable than top-left, and the gap has no meaning under overlap.
- **Letting `Quick create` start a run.** Two gestures with near-identical results that differ by an edge.
- **Invalidating on undo, paste or reload as separate rules.** Each already breaks the run through the selection or through the run being transient.

## Does not decide

Whether the first `+20, +20` step should follow `DominantGridSpacing()` when zoomed far out, where a 20-unit offset is a fraction of a screen pixel and the grid step is 200 or 2000. That is a question about placement generally, shared with click-to-add and the paste cascade, not about runs.

**Confirmed by ADR 0042:** Alt-drag duplication ships, as a clone drag. Its copies are selected at release and its source is the originals' committed selection bounds, so it starts a run exactly as "What starts a run" anticipated, with no rule of its own.
