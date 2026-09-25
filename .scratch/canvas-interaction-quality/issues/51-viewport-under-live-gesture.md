# Viewport changing under a live gesture

Type: grilling
Status: open
Blocked by:

## Question

Decide how a live pointer gesture responds when the viewport moves under it.

Split out of [Committed state changing under a live gesture](30-committed-state-under-live-gesture.md). ADR 0038 blocks keyboard commands that write `Board` or the selection while a pointer gesture owns the press, and leaves viewport commands live: `PageUp`/`PageDown`, `Shift`+`1`/`2`/`0`, and the wheel. That is right, but it means the viewport can change mid-drag, and nothing says what the preview does then.

- **Which space the delta lives in.** ADR 0020 has every gesture derive from the press-anchored absolute delta, and states it for screen deltas. If a move's board delta is the screen delta divided by the current scale, a zoom mid-drag makes the shape jump away from the cursor. If the press point is fixed in board space and the current pointer is converted through the live viewport on each tick, the shape stays under the cursor through a zoom or a wheel pan, and a dropped frame still costs nothing. Decide which, and whether it is the same answer for `MoveSelection`, `ResizeSelection`, `DragEdgeEnd` and `MarqueeSelect`.
- **Whether this is a route across viewports.** ADR 0029 declined edge auto-scroll and recorded that a drag across more than one viewport stays drag, release, pan, drag. Wheel-panning or zooming with a button held, with the shape staying under the cursor, is the route Figma-style tools offer. Decide whether the product wants it, or whether viewport input should itself be blocked mid-press.
- **Whether windowing still holds.** `CONTEXT.md`'s `Live geometry` entry says committed mounting is stable across a gesture *"only because `Pan` and `MinimapPan` are the only pointer gestures that move the viewport"*. A keyboard zoom or a wheel pan mid-drag breaks that premise. Windowing reads committed bounds against the viewport, so the dragged participant's press-time position can leave the viewport plus overscan and unmount, which is the "dragging nothing" failure ADR 0020 used to reject live windowing. Decide whether participants are pinned mounted for the press.
- **Level of detail.** ADR 0020 freezes a participant's LOD state at press. Check whether a zoom mid-press changes anything that freeze does not already cover.
- **Pan and zoom during `Pan`.** A wheel zoom while a secondary-button pan is held. Decide whether the two compose or one wins.

Check ADR 0019 first: it says nothing about wheel input with a button held, which may be silence rather than a decision.
