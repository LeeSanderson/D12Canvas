# Viewport changing under a live gesture

Type: grilling
Status: resolved
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

## Answer

Recorded as [ADR 0056](../../../docs/adr/0056-the-viewport-moves-under-a-live-gesture.md). Grilled with the dev, seven questions, each agreed as recommended.

1. **The viewport stays live during a press, and content follows the cursor.** Blocking was rejected because ADR 0019 always captures the wheel, so a blocked wheel does nothing at all mid-drag. Live input gives the cross-viewport route ADR 0029 recorded as missing, with no clock, and it works on a maximised canvas.
2. **The press point is anchored in board space for all four active-phase gestures.** `MoveSelection`, `ResizeSelection`, `DragEdgeEnd` and `MarqueeSelect` all keep what the pointer holds under it. The marquee's start corner sticks to the board, so a band can stretch across a pan.
3. **C# re-ticks on `ZoomPanTracker.Changed`**, using the last screen pointer. No interop, since keyboard viewport commands never reach JavaScript. Velocity is zero, so snapping stays on.
4. **A framing flight mid-press animates without ADR 0015's `pointer-events` guard.** The guard has nothing to guard while a gesture owns the press, and it sits on the ancestor of the capture element.
5. **Any viewport change while `pointing` promotes the gesture.** C# tells JavaScript once. A port-span press plus wheel becomes a connector drag, and a secondary press plus wheel becomes a `Pan` with no menu on release.
6. **Participants have a sticky mount.** Mounted at press stays mounted, and live bounds entering the window mounts until release. This also closes an existing gap: an off-screen participant dragged into view never mounted. ADR 0042's fragment rule becomes a case of it.
7. **LOD freeze stands; a late-mounted participant resolves once, at mount.** During `Pan` and `MinimapPan`, an outside viewport change re-anchors the pan so the two add. Pointer-anchored zoom composes for free.

Checking the answers against each other turned up no contradictions. It did turn up three texts whose reasons had gone stale: ADR 0020's windowing premise and its content-extent reason, `CONTEXT.md`'s `Live geometry` entry, and ADR 0029's claim that declining auto-scroll keeps windowing safe. All three are amended. ADR 0029's decision stands, and reopening it is now cheaper.

Amends ADRs 0015, 0018, 0020, 0029, 0038 and 0042, and confirms 0019. `CONTEXT.md`'s `Pointer gesture` and `Live geometry` entries are updated. One `Interaction probe` is owed: a wheel with the primary button held reaches the container listener and moves the preview.
