# While a pointer gesture owns the press, the keyboard cannot edit the board or the selection

While a `Pointer gesture` owns the press, in any of `pointing`, `active` or `cancelled`, nothing writes `Board` except that gesture's own release, and no keyboard command changes the `Selection`. A keyboard command that would do either does nothing, and the gesture carries on. A host reassigning `Board` mid-press cancels the gesture instead. A host writing entity fields directly mid-press is outside the contract.

This answers the question ADR 0020 handed on in its "does not decide" section.

## The failure is a history entry nobody performed

ADR 0020 keeps `Board` unwritten for a gesture's duration and has the `Gesture preview` carry absolute bounds, computed from press-time committed state. At release the commit reads `before` off `instance.Bounds` and writes `after` from the preview. If anything writes `Bounds` in between, `before` is the new value and `after` was derived from the old one. The entry records a jump nobody made, and undoing it puts the entity somewhere it has never been.

Three routes reach committed state with a button held: undo and redo, any other board-writing keyboard command such as a nudge, and the host.

## Block, rather than cancel

ADR 0020 named cancel-then-apply as the leading candidate, since ADR 0031 makes cancel cheap. It is rejected, and `Ctrl`+`Z` is the case that decides it. The drag is not in history yet, so a `Ctrl`+`Z` that cancels and then applies does two things with one key. The shape springs back, and the action *before* the drag is undone as well, possibly off screen. That is a milder form of the surprise this decision exists to remove.

Cancel-and-swallow, where the key behaves as Escape, reads `Ctrl`+`Z` the way a user probably means it and costs a drag to anyone who hit a key by accident. Rebasing the preview onto the new committed state keeps the gesture alive, moves the thing under the user's hand when a nudge or undo lands, and needs the press-time snapshot ADR 0020 deliberately does not keep.

Blocking loses nothing. The key does nothing, and pressing it again after release does what it always did.

The objection recorded against blocking was that it needs a per-command allow-list. So does cancel: it cannot cancel a drag on `PageUp` or `Ctrl`+`C`, so it has to sort commands too. And for the `Board` half no list is needed at all, because every library write to `Board` already goes through `CommandHistory`: `Do`, `Undo`, `Redo`, and `NudgeCommand.Extend` on a held arrow. The check sits on that route.

## Stated as ownership, so it reaches `pointing` and `cancelled`

The rule is keyed on who owns the press, not on whether the gesture is doing anything, which is the same line ADR 0031 drew when it had a second Escape do nothing. A press that has not crossed the threshold still owns the pointer and can still commit at release, as `Quick create` does from a port span, so `pointing` is covered. A cancelled gesture still owns the pointer until its button comes up, so `cancelled` is covered. The alternative would make a key's meaning depend on a phase the user cannot see.

Nothing re-arms. Blocking never ends the gesture, so the question of whether a cancelled gesture could restart without a new press does not arise here.

## Selection is included, and enforced differently

Two commands change only the selection: `Ctrl`+`A` and `Space`. Left live, `Ctrl`+`A` during a `MoveSelection` either jumps every entity by the drag's delta or leaves the selection disagreeing with what is moving, depending on whether participants are read live or frozen at promotion. During a `MarqueeSelect` the keypress is overwritten on the next tick anyway. ADR 0031 already treats the selection as the gesture's to restore, so the gesture owns it for the press.

Selection writes have no single route. `_selectedInstanceIds` is written in fifteen places in `DiagramCanvas`, and ADR 0037 adds a second set beside it. So this half is a guard on the two keyboard handlers rather than a structural check, and ADR 0025's `[Theory]` over the closed eight is where a third selection-writing command gets caught.

## What stays live

Escape, with ADR 0031's three-rung chain unchanged. Copy, which reads committed state and writes nothing. The snap-to-grid toggle, which changes how the next tick is computed rather than what is committed. Focus moves. Viewport commands, which change no committed state. How the preview should respond when the viewport moves under it is a separate question and not settled here.

## The host

The library cannot refuse a parameter set or stop code writing to objects it hands out, so blocking is not available and the two host routes get different answers.

**A `Board` reference change cancels the gesture.** A load path usually deserializes a fresh copy with the same GUIDs, and the preview is keyed by instance id. Left alone, the release resolves those ids against the new board and writes old preview geometry onto fresh instances, which nothing shows as wrong until later. So the canvas detects the reference change and cancels as ADR 0031 defines it, with one step removed: the `Selection snapshot` is **not** restored, because it names entities in a model that is gone. The gesture holds capture until release as any cancelled gesture does.

**A host writing `Bounds`, `ZIndex` or `Props` directly during a gesture is undefined.** `Board` and its entities raise no change events, so the library cannot see it, and a direct write already bypasses history whether or not a gesture is live. The release still commits a coherent entry, from the host's value to the preview. No public "gesture is live" signal is added. ADR 0020 declined a public live-geometry surface on the grounds that exposing it later costs nothing, and that holds for a flag too.

## What the user sees

Nothing. A blocked key is silent, the same shape as ADR 0031's silent cancel, and it goes to the same place: the cursor and micro-feedback work, which now has one more case where the pointer's own state is the thing going unexpressed.

## Deleted under a live gesture

ADR 0020's ticket asked whether an entity deleted out from under a gesture was this decision's or cancellation's. After the rule above it is neither's, because no library route reaches it: delete, undo and redo are blocked, a swap cancels, and a direct host removal is outside the contract. No gesture needs to tolerate a vanished participant.

## What this amends

**ADR 0020**: its "does not decide" section is answered, and not by the candidate it named.

**ADR 0026**: every guarded row that writes `Board` or the selection gains a second condition, that no pointer gesture owns the press. Escape, copy, the snap toggle, focus moves and the viewport rows are unaffected.

**ADR 0031**: a third cancel trigger joins Escape and interruption, a `Board` reference change, and it is the one cancel that does not restore the `Selection snapshot`. Its note that cancel was the likely answer here is superseded.

## Considered and rejected

- **Cancel, then apply.** One `Ctrl`+`Z` undoes the drag and the action before it.
- **Cancel and swallow.** Reads undo well and loses a drag to any stray key.
- **Rebase the preview.** Moves content under a held button, and needs the press-time snapshot ADR 0020 does not keep.
- **Block only during `active`.** Makes a key's meaning depend on a phase the user cannot see, and misses `Quick create`, which commits from `pointing`.
- **Blocking `Board` writes but not selection writes.** Leaves `Ctrl`+`A` able to change what a live move is moving.
- **Restoring the selection snapshot on a `Board` swap.** It names entities from the old model.
- **Deferring a `Board` swap until release.** The host sets a parameter and reads back a different one.
- **A public "gesture is live" signal for hosts.** Nothing needs it yet, and adding it later costs nothing.

**Answered by ADR 0056:** the viewport stays live in every phase, and a live gesture keeps what it holds under the pointer when the viewport moves.
