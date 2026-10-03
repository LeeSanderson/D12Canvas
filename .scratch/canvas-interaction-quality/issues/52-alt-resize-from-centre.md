# Alt on a resize: resize from centre

Type: grilling
Status: resolved
Blocked by:

## Question

Decide whether holding Alt during a `ResizeSelection` anchors the resize at the selection's centre instead of the opposite side or corner, and if so, whether Alt is read live as it is on a move.

Surfaced while resolving [Alt-drag to duplicate](34-alt-drag-duplicate.md). The reference-tool teardown found Alt doubly booked in all four tools: duplicate-on-drag and resize-from-centre, unambiguous because they attach to different gestures. ADR 0042 took the first and left `ResizeSelection` ignoring Alt.

This is not covered by the map's out-of-scope ruling. Aspect-lock and proportional multi-resize went out with rotation, but resize-from-centre needs no angle and no new `Bounds` shape. It is a different calculation inside the resizer's preview.

Start from ADR 0042's answer: Alt is live on every gesture that binds it, arriving on `OnPointerMoved`, and a toggle only changes what the preview holds. Then decide:

- **Whether it exists.** Today Alt on a resize is a plain resize, so the far side moves when the user expected it to stay. That costs one undo, which is milder than the stray-copy case that made Alt-drag worth building.
- **How it composes with snapping.** ADR 0024 snaps the selection's edges to neighbours and anchors grid snapping at its top-left. With a centre anchor, both opposite edges move together, so decide which edge drives the snap and whether the other mirrors it.
- **How it composes with a multi-selection resize.** ADR 0018's single resizer scales members within the selection bounds. Decide whether the centre is the selection's centre, and confirm members still scale about it as a rigid body.
- **Its keyboard route.** ADR 0010's `Alt+Arrow` already resizes by one step. Decide whether a centred keyboard resize is needed at all, under ADR 0026's "a chord only where nothing else reaches".

**Update from ADR 0043 (latched-versus-live modifiers resolved):** the live question is answered by rule. Alt on a resize changes what `ResizeSelection` does, not which gesture it is, so if it ships it is read live, and a change with the pointer still re-sends the last move, so the anchor switches the moment Alt changes. Nothing on this ticket needs to decide that again.

## Answer

Recorded as [ADR 0057](../../../docs/adr/0057-alt-resizes-from-the-centre.md). Grilled with the dev, five questions, each agreed as recommended.

1. **It ships.** Alt during a `ResizeSelection` anchors the resize at the centre. Read live under ADR 0043, and a mode of `ResizeSelection`, so the closed set keeps eight members. Declining would leave a key all four reference tools share doing a plain resize.
2. **Snapping keeps ADR 0024's rule as written.** Under a centre resize both edges of a driven axis are actually moving, so both are anchors. Per axis the smallest correction wins and the other edge mirrors it, for object and grid snapping alike. That keeps the handle closest to the pointer and lets the far edge snap when it is the one being aimed.
3. **The centre is the centre of the instances-only box the handles sit on**, fixed at press. Members scale inside it unchanged, an edge handle drives one axis, the floor clamps symmetrically, and a toggle recomputes from the start box, so the opposite edge jumps and the handle stays under the pointer.
4. **No keyboard chord.** Pairs of `Alt+Arrow` presses reach every centred result, and no chord is free.
5. **No marker.** The mirrored edge is the feedback.

Checking the answers against each other turned up one interaction, already covered: a snap that would take the box below its floor is clamped after the snap, as ADR 0048 does for `Alt+Arrow`. It also turned up a benefit: because Alt is live, a user on a Linux window manager that swallows Alt+press can press first and then hold Alt, which helps clone drag as well.

Amends ADRs 0018, 0024, 0042 and 0043 by addendum. `CONTEXT.md` gains a `Centre resize` entry. ADR 0042's Windows Alt-release probe gains a resize case. No new tickets.
