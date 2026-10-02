# Alt on a resize: resize from centre

Type: grilling
Status: open
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
