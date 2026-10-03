# The viewport stays live during a press, and what the pointer holds stays under it

ADR 0038 blocked board and selection writes while a `Pointer gesture` owns the press, and left viewport input live: the wheel, `PageUp`/`PageDown`, and the framing keys `Shift`+`1`/`2`/`0`. It did not say what a live gesture does when the viewport moves under it. This decides that.

**The viewport stays live in every phase, and whatever the pointer is holding stays under the pointer.** A user can grab a shape, zoom out or wheel-pan, and drop it two screens away in one gesture. ADR 0029 recorded that route as missing, and this provides it without a clock.

## Why not block it

Blocking viewport input during a press is simpler and keeps ADR 0020's windowing argument intact. It was rejected because the wheel would go dead for every drag. ADR 0019 calls `preventDefault` on every wheel event over the canvas, so a blocked wheel doesn't scroll the host page either. Nothing happens at all.

Live input also fixes something ADR 0029 could only document. A drag that needs to reach past the viewport currently takes drag, release, pan, drag. With a live viewport it takes one press and a wheel. Unlike auto-scroll, this works on a maximised canvas, because every step is driven by an input event and none of them waits for a `pointermove` the OS has stopped sending.

## The press point is anchored in board space

ADR 0018 gives every gesture the press-anchored absolute delta, written for screen coordinates. It now holds in **board** coordinates: the press point is converted to a board point once, at press, and on every tick the current pointer is converted through the viewport as it is now. The delta is the difference of the two board points.

ADR 0029 already noted that this extends the identity rather than reversing it. A dropped frame still costs nothing, because the delta is still absolute.

The same rule applies to all four gestures with an active phase, with no branch on gesture kind:

- **`MoveSelection`.** The point that was grabbed stays under the cursor.
- **`ResizeSelection`.** The dragged handle stays under the cursor and the opposite edge stays fixed on the board, so zooming out mid-resize can make something larger than a screen.
- **`DragEdgeEnd`.** The loose end stays at the cursor, so a connector can reach a component several screens away.
- **`MarqueeSelect`.** The band's start corner stays on the board point where the press landed, so the band stretches as the viewport pans. A band fixed on screen was rejected because the selection would then be whatever happened to pass under a fixed rectangle.

Snapping is unchanged. ADR 0020's one rigid-body snap per tick and ADR 0048's anchors apply to the board-space result as before.

## C# re-ticks on `ZoomPanTracker.Changed`

A viewport change with a still pointer would leave the preview behind until the next real move. So **while a gesture owns the press, every `ZoomPanTracker.Changed` re-runs that gesture's tick with the last screen pointer position C# received.**

ADR 0043 solved the same problem for modifiers in JavaScript, because modifier state is gathered there. The viewport is C#'s. The keyboard viewport commands never pass through the pointer listener, so a JavaScript re-send would need C# to notify JavaScript first so that JavaScript could call back. Re-ticking where the state lives needs no interop.

- **No clock.** The tracker event is the trigger, so ADR 0029's refusal of a time base still holds.
- **Velocity is zero** on a re-tick, as on ADR 0043's re-send, so ADR 0024's fast-pointer cut-off doesn't suppress snapping. Guides can flicker while a shape is carried through a wheel pan. They settle when the wheel stops, which is when they matter.
- **One re-tick per tracker change.** Per-tick work is still proportional to the participants, which keeps ADR 0020's structural budget.

## A viewport change promotes a press from `pointing`

Wheel zoom is anchored on the pointer (ADR 0019), so the board point under a still cursor doesn't move and a held shape stays put. A pan, a keyboard zoom anchored on the viewport centre, or a framing flight does move it. With the threshold measured in screen pixels by JavaScript, the press would still be `pointing`, and the grabbed shape would slide away from the cursor. When the pointer later crossed the threshold, the board-anchored delta would make the shape jump the whole pan distance.

So **any viewport change while a gesture is `pointing` promotes it to `active`**, and C# tells JavaScript once so it starts forwarding moves. The threshold is crossed when the pointer has moved far enough *or* the viewport has moved. One rule covers every viewport change, including pointer-anchored zoom where it isn't strictly needed. Gestures with no active phase (`SelectEdge`, `Native`) are unaffected.

Two outcomes follow, both accepted:

- **A port-span press followed by viewport input becomes a connector drag**, not `Quick create`. ADR 0030 already reads movement as connect. Pressing, rolling the wheel and releasing without moving gives whatever a connector drag released over its own source instance gives. This ADR doesn't change that outcome.
- **A secondary press followed by viewport input becomes a `Pan`**, so its release opens no object menu (ADR 0022). The user was navigating.

## During `Pan` and `MinimapPan`, outside viewport changes add

A `Pan` read in board space keeps the grabbed board point under the cursor. Wheel zoom about the pointer leaves that point where it is, so zoom composes with a held pan without any special handling.

For any other viewport change (`Shift`/`Alt`+wheel, a trackpad swipe, `PageDown`, framing), **the pan re-anchors to the board point now under the cursor**. The two movements add: a held right-drag plus `Shift`+wheel pans further, and after a framing flight lands, the hand carries on from where it landed. "The hand wins", where the next re-tick restores the press anchor, was rejected because it makes every other viewport input dead during a held pan.

The re-anchor doesn't need to tell a pan's own tracker writes apart from anyone else's. After `Pan` writes the tracker, the anchor is already under the cursor, so re-anchoring changes nothing.

`MinimapPan` follows the same rule for keyboard viewport commands. Wheel input over a host-placed minimap never reaches the canvas's listener.

## Framing mid-press animates, without the pointer guard

ADR 0015 sets `pointer-events: none` on the container during the 250ms framing flight, so that a click can't land on whatever C# already thinks is under the cursor. While a gesture owns the press that guard does no work, because ADR 0022 drops every other button. It is also a risk: capture sits on `.diagram-canvas` inside that container, and whether an ancestor's `pointer-events: none` interrupts a capturing element's move stream has never been measured.

So **the guard is skipped while a gesture owns the press, and the flight still runs.** Someone carrying a shape across the board needs the orientation the flight gives more than anyone. The preview is computed against the destination viewport, and it is drawn inside the same easing transform as everything else. The shape drifts briefly off the cursor during the flight and lands under it.

Framing aims at the committed board, as before. `Content extent` doesn't include the carried shape's live position, and doesn't need to, because the shape follows the cursor wherever the flight ends.

## Participants have a sticky mount

ADR 0020 windows by committed bounds and says mounting is stable across a gesture *"only because"* nothing but a pan moves the viewport. That premise is gone. A participant's press-time position can now leave the viewport plus overscan mid-drag, and the instance in the user's hand would unmount.

There was also an existing gap that doesn't depend on viewport changes. A participant that wasn't mounted at press, such as an off-screen instance included by `Ctrl`+`A`, can be dragged into view, and committed-bounds windowing never mounts it.

So **for the participants of a live gesture, the mounted set only grows until release.** A participant mounted at press stays mounted. A participant whose live bounds enter the viewport plus overscan mounts, and stays mounted. Non-participants window normally, as they do during a `Pan`. At release, windowing resumes against the committed result.

- **Pinning every participant** was rejected because a `Ctrl`+`A` drag on a large board would mount the whole board at promotion.
- **Live-bounds windowing for participants** was rejected for the reason ADR 0020 rejected live LOD: members crossing the overscan edge would mount and unmount at frame rate, discarding author component state each time.

ADR 0042's rule that a `Clone drag` fragment mounts regardless of windowing is now a case of this one. The fragment has no committed bounds, and its live bounds sit under the cursor.

## Level of detail: ADR 0020's freeze stands, and a late mount resolves once

ADR 0020 freezes a participant's LOD state at press. That covers a mid-press zoom for every participant mounted at press: it stays as it was until release. The visible cost is the pop ADR 0020 already accepted, now reachable by zoom as well as by resizing to four pixels. A full component carried through a 50× zoom-out stays a mounted component drawn tiny, then becomes a placeholder on release. The cost is capped at what was mounted at press.

**A participant that mounts mid-press resolves its LOD state once, at mount**, from the current scale and its committed bounds (the inputs ADR 0011 and ADR 0020 already use), and holds it until release. A zoomed-out `Ctrl`+`A` drag therefore mounts late arrivals as placeholders, not as full components.

## Verification

Under ADR 0025, the re-tick, the promotion, the re-anchor and the sticky mount are C# behaviour with a C# entry point (the tracker), so bUnit asserts each one by changing the tracker mid-gesture and reading the `Gesture preview` and the mounted set.

One claim is about the browser and gets an `Interaction probe`: a wheel with the primary button held and capture on `.diagram-canvas` reaches the container's wheel listener, and the preview moves. Wheel events aren't pointer events and capture shouldn't affect them, but this map's rule is that browser claims are measured.

## What this amends

**ADR 0018.** The delta identity holds in board space. The threshold is crossed by pointer distance or by a viewport change, and in the second case C# promotes and tells JavaScript.

**ADR 0019** is confirmed. It said nothing about wheel input with a button held, and that was silence, not a decision.

**ADR 0020.** Windowing for participants follows the sticky mount above rather than committed bounds alone, so the "no pin rule" claim and its *"only because"* premise are withdrawn. A late-mounted participant resolves LOD at mount. `Content extent` stays committed, with a new reason: framing mid-gesture is now allowed and aims at the committed board, and the carried content follows the cursor regardless.

**ADR 0029**'s decision stands: no edge auto-scroll. Its reasoning that declining the feature keeps the windowing claim true no longer holds, and its delta is no longer screen-anchored. Its revisit section gets cheaper: the board-anchored delta and the mount rule it priced in now exist, so auto-scroll would cost only the heartbeat and its four tuned constants.

**ADR 0015.** The framing flight's pointer guard is skipped while a gesture owns the press.

**ADR 0038**'s open question about the preview under a moving viewport is answered here.

**ADR 0042.** The fragment's mount rule becomes a case of the sticky mount.

## Considered and rejected

- **Blocking viewport input during a press.** The wheel goes dead for every drag, and the cross-viewport route stays missing.
- **Allowing it for some gestures only.** A per-kind rule over ADR 0018's closed set, against that ADR's refusal of per-kind thresholds.
- **A screen-anchored marquee.** The band selects whatever scrolled under it.
- **A JavaScript re-send after a viewport change.** Keyboard viewport commands never reach JavaScript, so it would need a C# notification and a call back.
- **Waiting for the pointer threshold in `pointing`.** The shape jumps the full pan distance on promotion.
- **Anchoring at promotion instead of at press.** No jump, but the grabbed shape is left behind on the board.
- **"The hand wins" during `Pan`.** Every other viewport input is undone by the next re-tick.
- **An instant jump for framing mid-press.** Loses the orientation the flight provides.
- **Keeping the flight's pointer guard and probing whether capture survives it.** The guard has no job while a gesture owns the press.
- **Pinning every participant mounted.** A large selection mounts in bulk at promotion.
- **Live-bounds windowing for participants.** Mounts and unmounts author components at frame rate at the overscan edge.
- **Late mounts always full, or always placeholder.** Full mounts in bulk on a zoomed-out drag; always-placeholder shows a placeholder for something large on screen.
