# Alt on a resize anchors it at the centre

Holding Alt during a `ResizeSelection` makes it a **centre resize**. The selection's centre stays where it is, and the edge opposite the handle moves in mirror with the handle. Alt is read live, as ADR 0043 requires of a modifier that changes what the running gesture does, so pressing or releasing it mid-resize switches between the two anchors, and the release commits whichever the screen last showed.

ADR 0042 bound Alt on `MoveSelection` and left `ResizeSelection` ignoring it. All four reference tools put resize-from-centre on Alt. Without it, an Alt-resize here is a plain resize, and a user who expects the far side to stay still has to undo. That costs less than the stray copy that justified clone drag, but it is still the canvas ignoring a key every tool a user has met gives a meaning. The cost is one alternative calculation in the resizer's preview, with no new `Bounds` shape and no angle, so the out-of-scope ruling on rotation and aspect-lock does not reach it.

## Still `ResizeSelection`

A centre resize changes what the gesture commits and never which gesture owns the press. The owner, the capture, the claiming handle and the press-anchored delta stay the same through a toggle, so the closed set of eight keeps eight members, as it did for clone drag.

## The centre and the box

- **The centre is the centre of the box the handles sit on**, read from committed bounds at press and fixed for the whole gesture. That box is ADR 0037's instances-only bounding box, so edges do not move the centre. A single instance is the one-member case, and nothing branches on selection size (ADR 0020).
- **An edge handle drives one axis.** Alt on the right handle moves the left and right edges apart or together and leaves the height alone. A corner drives both axes.
- **Members scale inside the box exactly as before.** The centre anchor changes only how the box is computed from the delta. The mapping from box to members is the same proportional one, so ADR 0048's rigid-body rule holds with no new text.
- **The floor clamps symmetrically.** The box's minimum width and height are the ones the plain resize already derives, so no member can shrink below its own floor. At the floor both edges stop the same distance from the centre, so the box does not drift sideways at the limit.

## A toggle recomputes from the start box

Every move recomputes the box from the start box and the current press-anchored delta, under whichever anchor Alt selects. A centre resize mirrors the delta, so the handle stays under the pointer through a toggle and the opposite edge jumps: out to its mirrored position when Alt goes down, back to where it started when Alt comes up. Excalidraw and tldraw behave the same way.

Re-anchoring at the moment Alt changes was considered. It avoids the jump, but the result then depends on the order and timing of the key presses, which is history the preview would have to carry. ADR 0043's model is that a toggle only changes what the preview holds.

## Snapping

ADR 0024 snaps a resize on "the edges actually moving", and ADR 0048 gives grid snapping the same anchor set. Under a centre resize both edges on a driven axis are actually moving, so the rule is kept as written and both are in the set.

- **Per driven axis, the smallest correction wins.** Each of the two edges proposes its own object-snap and grid correction. The smallest one applies, and the other edge moves by the same amount the opposite way. Any correction, to either edge, moves the handle off the pointer by the same distance, so picking the smallest keeps the handle closest to the pointer. It also lets the far edge snap when that is the edge the user is aiming, which a handle-only rule would never allow.
- **The mirrored edge lands wherever the mirror puts it.** It is on a grid line only if the centre allows it. This is the same accepted cost as ADR 0048's inner member edges.
- **ADR 0024's per-axis precedence is unchanged.** Object snapping takes an axis where it fires, and grid fills the rest. `Ctrl` suppresses both.
- **Guides describe what matched.** The second pass runs from the snapped position, so a mirrored edge that happens to land on a neighbour's edge gets a guide as well.
- **The floor wins over a snap.** A correction that would take the box below its minimum is clamped after the snap, as ADR 0048 already does for `Alt+Arrow`.

A centre resize therefore snaps more often than a plain one, because it has twice the edges in play. The 8-pixel tolerance and the fast-pointer cut-off apply unchanged.

## No marker

Nothing new is drawn. The opposite edge moving in mirror is itself the feedback, and none of the reference tools draws a centre point or a mode badge. A marker would add selection chrome while the user is judging a result, which ADR 0041 keeps to a minimum.

## Keyboard and platform

ADR 0026 adds a chord only where nothing else reaches. A centred grow is `Alt+→` followed by `Alt+←`, and a centred shrink is the same pair with `Shift` (ADR 0010). Under snap each press steps to the next grid line (ADR 0048). Pairs of presses reach every centred result the pointer can, so no chord is added. None is free in any case: `Alt` and `Alt+Shift` are taken, and `Ctrl+Alt+Arrow` rotates the display under some Windows graphics drivers.

ADR 0042's two platform facts apply here too:

- **The Windows Alt-release probe gains a case.** It establishes whether releasing Alt after an Alt-resize activates the browser's menu bar, as it already does for a clone drag and an Alt+click.
- **Linux window managers that bind Alt+drag** take the press before the page sees it. Because Alt is read live, a user can press the handle first and then hold Alt, so a centre resize is still reachable there. The same holds for a clone drag.

## What this amends

**ADR 0042 is amended by addendum**: its open claim on `ResizeSelection` is answered, and Alt now binds on two pointer gestures.

**ADR 0043 is amended by addendum**: the pending row for Alt on a resize is filled, read live.

**ADR 0024 is amended by addendum**: on a centre resize, both edges of a driven axis are in the resize anchor set and the smallest correction wins. This applies to grid snapping under ADR 0048 as well.

**ADR 0018 is amended by addendum**: Alt is a live mode of `ResizeSelection`, with no new member.

**ADR 0010 and ADR 0026 are untouched.** **ADR 0020 and ADR 0048 are reused, not amended.**

## Considered and rejected

- **Declining it**: leaves a key every reference tool shares doing a plain resize here.
- **Latching Alt at press**: ADR 0043 reads a modifier that changes what the gesture does live, and latching would make Alt behave differently on a resize than on a move.
- **The handle's edge alone drives the snap**: never lets the far edge snap when the user is aiming it, and can apply a larger correction than needed.
- **Re-anchoring at the moment Alt changes**: avoids the jump, and makes the result depend on key-press history the preview would have to carry.
- **The centre of each member, or of an edge-inclusive extent**: breaks the rigid-body rule, or moves the centre away from the box the handles sit on.
- **A centre marker or mode badge mid-resize**: new chrome during the judging moment, with no reference tool drawing one.
- **A keyboard chord for a centred resize**: pairs of `Alt+Arrow` presses already reach it, and no chord is free.
