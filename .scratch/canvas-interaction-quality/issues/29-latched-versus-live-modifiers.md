# Latched-versus-live modifier semantics

Type: grilling
Status: open
Blocked by: 07, 34

## Question

Decide, per modifier, whether it is read once at press and latched for the gesture's duration, or read live on every move — and state a reason for each.

Graduated from the map's fog, which recorded this as unphraseable until an arbitration model existed. It now exists: ADR 0018 fixes that a pointer gesture's *identity* is chosen at press and never changes mid-press, and that the mapping reads buttons and modifiers. What it deliberately did not decide is whether a modifier's *value* is frozen at that moment or re-read as the gesture runs.

Ticket 03's teardown found the two reference tools disagreeing, both deliberately:

- **tldraw's Alt-clone toggles mid-drag**, rewinding history to the mark so undo stays clean — so the modifier is live, and the cost is a history mechanism that can unwind a partially-committed gesture.
- **Excalidraw deliberately latches `withCmdOrCtrl` at press**, in its own words, "otherwise it would have weird results (stuff jumping all over the screen)."

Both are right about their own case, which is why this has to be per-modifier rather than one blanket rule.

Decide:

- **Which modifiers latch and which stay live**, with a stated reason each. The candidates are whatever ticket 07 lands on for its button and modifier table, plus Shift for constrained/axis-locked movement and Alt for clone-on-drag if that arrives.
- **Whether a live modifier may change what the gesture *does* without changing which gesture it is.** ADR 0018 forbids identity changes mid-press; a live Alt that turns a move into a clone is either a behaviour change inside `MoveSelection` (permitted) or a different gesture (forbidden), and which one it is depends on how the commit is expressed.
- **How a live modifier reaches the gesture at all.** ADR 0018 has JS report modifiers on the press, and `OnPointerMoved` carries the pointer's position — decide whether it also carries modifier state, and note that a modifier pressed or released *without the pointer moving* generates no pointer event at all. That is a real gap: the keyboard listener would have to feed the active gesture, which is a channel ADR 0018 does not currently provide.
- **What a latched modifier does about the release.** If Shift is latched at press and released before the drag ends, the gesture commits under the latched value — which is correct by construction but needs to be visible to the user, or it reads as the canvas ignoring them.
- **How undo survives a live modifier.** ADR 0007 gives one history entry per gesture, and tldraw's mid-drag toggle needs a mark-and-rewind to hold that. Decide whether any live modifier here requires the same, since that would be a genuine addition to the history model rather than a use of it.

Blocked by ticket 07, which decides which modifiers mean what — this ticket cannot enumerate per-modifier answers before that table exists.

**Update from ADR 0022 (ticket 07 resolved):** the table this inherits is **smaller than the question above assumes**, and two of its three named candidates are gone.

`Shift` has a selection meaning on the pointer and no in-gesture meaning at all, so there is nothing to latch: it appends at press outside the selection and toggles at release inside it, both of which are read once, at the moment they act. Axis-locked movement, the reason `Shift` was listed as a candidate here, went to [Alignment guides and object snapping](11-alignment-guides-and-object-snapping.md) so the whole constrain vocabulary is decided in one place. Alt is unbound on the pointer pending [Alt-drag to duplicate](34-alt-drag-duplicate.md), which is now a second blocker on this ticket. Ctrl is unbound on the pointer entirely, deliberately left for ticket 11's snap suppression.

So what remains here is genuinely narrow, and worth re-reading before starting: whether any *surviving* modifier needs live reading, plus the two structural questions above that hold regardless of which modifiers exist — whether a live modifier may change what a gesture does without changing which gesture it is, and the channel gap, since a modifier pressed or released without pointer movement generates no pointer event. Note that ADR 0022 widened ADR 0018's ownership to key on the claiming *button*, which means a modifier-only state change now has an even clearer reason to need its own channel: no button event accompanies it either.

**Update from ADR 0024 (ticket 11 resolved):** this ticket now inherits **two bound modifiers and a derived answer for one of them**, so what is left is narrower again.

`Ctrl` means suppress-all-snapping, and it is **live, not latched, and this was forced rather than chosen**. On macOS a `Ctrl`+primary press is the system secondary click, which ADR 0024 honours with a platform check, so on that platform a `Ctrl` press is not a primary press at all and there is nothing to latch. Latching it would work on Windows and be unreachable on macOS. The user presses without `Ctrl` and holds it mid-drag, identically on both platforms.

**That also dissolves the channel gap for `Ctrl` specifically, without answering it in general.** Snapping has no observable effect until the pointer moves, so a `Ctrl` pressed with no movement changing nothing is correct rather than a defect, and modifier state carried on `OnPointerMoved` is sufficient. ADR 0024 accordingly asks ADR 0018 for modifiers-per-move and a pointer velocity, and asks for no new channel. The gap is still real for any *future* modifier whose effect should be visible while the pointer is still, so the structural question survives with one fewer claimant.

`Shift` gained an in-gesture meaning after all — `Axis lock` on a move — reversing the reading above that "there is nothing to latch". It should almost certainly be live too, since the locked axis is re-read from the press-anchored delta on every move by ADR 0024's own rule, but that ADR did not state the latch question for `Shift` explicitly, so it is genuinely this ticket's. Note the composition it has to survive: `Shift` still appends at press and toggles at release for selection, and ADR 0024 relies on ADR 0022's threshold separating those from the constraint.

`Alt` remains unbound pending [Alt-drag to duplicate](34-alt-drag-duplicate.md), which is still the other blocker.
