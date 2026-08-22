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
