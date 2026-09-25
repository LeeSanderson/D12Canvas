# Committed state changing under a live gesture

Type: grilling
Status: resolved
Blocked by: 05

## Question

Decide what happens when committed board state changes *while* a pointer gesture is in flight.

ADR 0020 holds `Board` unwritten for a gesture's duration and has the `Gesture preview` carry **absolute** bounds, computed once from press-time committed state. Nothing stops committed state moving underneath that. Three routes reach it while a button is held:

- **`Ctrl+Z`/`Ctrl+Y`.** The keyboard listener is live throughout a drag, and undo writes `Bounds` directly.
- **An arrow-key nudge, or any other keyboard command.** `NudgeCommand` deliberately writes through and grows in place (ADR 0020 confirms that as correct for the keyboard), so a nudge during a drag mutates the very entities the preview is overriding.
- **The host reassigning `Board`, or an autosave/load path.** `OnParametersSet` can swap the whole model out; ADR 0015 also has the canvas frame all content when a `Board` is first set.

The failure isn't a crash, which is why it needs deciding rather than fixing: at release the gesture commits `before = instance.Bounds` (now the *undone* value) against `after = preview` (derived from the pre-undo value), producing a history entry that describes a jump nobody performed. Undo it and the entity lands somewhere it has never been.

Decide:

- **Which of block, cancel, or rebase.** Leading candidate is **cancel the active pointer gesture first, then apply the command** — free, given ADR 0020 makes revert a discarded dictionary, and it reuses ADR 0018's existing cancel channel rather than adding a state. Blocking keyboard commands for the duration is the cheaper-sounding option but silently swallows input and needs a per-command allow-list (is `Ctrl+C` really unsafe mid-drag?). Rebasing the preview onto the new committed state is the only option that keeps the gesture alive, and it needs the press-time snapshot the preview deliberately does not keep.
- **Whether the answer is uniform across all three routes.** A host swapping `Board` is not a user action and has no undo entry to be confused by; an undo is a deliberate user action *about* history. They may not want the same treatment.
- **What the user sees.** A cancel mid-drag means content snapping back under a still-held button, with the gesture then dead until release — or does it re-arm? ADR 0018 says a gesture's identity never changes mid-press, so re-arming would be a new gesture without a new press.
- **Whether this generalises past geometry.** The same shape exists for a `Selection` an undo invalidates (an entity deleted out from under a live gesture — `CompletePortDrag` already tolerates a vanished instance, `MoveSelection` currently would not). Whether that is this ticket's or ticket 22's is part of the decision.

Small, but sharp enough to state precisely, which is why ADR 0020 declined to settle it in a clause.

## Answer

**Block.** While a pointer gesture owns the press, in any of `pointing`, `active` or `cancelled`, nothing writes `Board` except that gesture's own release, and the keyboard cannot change the selection. A key that would do either does nothing, and the gesture carries on. Recorded as [ADR 0038](../../../docs/adr/0038-the-press-owns-the-board.md).

- **Block, not the leading candidate.** Cancel-then-apply was rejected on `Ctrl`+`Z`: the drag is not in history yet, so one key would spring the drag back *and* undo the action before it, possibly off screen. Cancel-and-swallow loses a drag to any stray key. Rebase moves content under a held button and needs the press-time snapshot ADR 0020 does not keep. Blocking loses nothing; the key works again after release.
- **The allow-list objection did not hold.** Cancel needs the same sorting, since it cannot cancel on `PageUp` or `Ctrl`+`C`. And the `Board` half needs no list: every library write to `Board` goes through `CommandHistory` (`Do`, `Undo`, `Redo`, and `NudgeCommand.Extend`), so the check sits on that route.
- **Keyed on ownership, not phase.** `pointing` is covered because `Quick create` commits from it, and `cancelled` because it still owns the pointer. Blocking never ends the gesture, so the re-arm bullet does not arise.
- **Selection is included.** `Ctrl`+`A` mid-move would change what is moving, and `Space` mid-marquee is overwritten next tick. Selection writes have no single route (fifteen writes to `_selectedInstanceIds`), so that half is a guard on the two handlers.
- **Still live:** Escape, copy, the snap toggle, focus moves, viewport commands.
- **Host routes.** A `Board` reference change cancels, without restoring the `Selection snapshot`, because it names entities in a model that is gone. The hazard is concrete: a load deserializes fresh instances with the same GUIDs, and a release would write old preview geometry onto them. A host writing entity fields directly mid-press is undefined and documented as the host's contract; the library cannot see it and no new public signal is added.
- **What the user sees:** nothing. Handed to the cursor and micro-feedback fog patch.
- **Deleted under a live gesture** is unreachable through any library route after the above, so it is neither this ticket's nor ticket 22's.

Amends ADRs 0020, 0026 and 0031. `Pointer gesture` widened in `CONTEXT.md`.

Split out [Viewport changing under a live gesture](51-viewport-under-live-gesture.md): viewport commands stay live, and nothing says whether the preview follows the cursor through a zoom, or whether windowing can unmount a dragged participant once the viewport moves mid-press. Two items added to the fog: what a `Board` swap does to the selection, and what it does to history.
