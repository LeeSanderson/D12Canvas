# Committed state changing under a live gesture

Type: grilling
Status: open
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
