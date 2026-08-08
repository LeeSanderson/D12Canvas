# Hit-test participation and hit-region geometry

Type: grilling
Status: open

## Question

Decide what participates in pointer hit-testing, in what precedence, and how a target's hit region is decoupled from the thing you can see.

Ticket 03's teardown found this is a first-class concept in all four reference tools and does not exist in D12Canvas at all — an arbitration model that classifies a press has to hit-test first, and where that hit-testing lives is the same question. Ticket 01 arbitrates *after* a hit is resolved, so it needs this seam to exist before it can decide what a press means.

The evidence is concrete rather than theoretical. `.edge-line` is `stroke-width: 2` with `pointer-events: auto` and no widened hit path — and because that width is in board units, an edge is ~0.2 screen pixels at 0.1× zoom, so selecting one at anything but high zoom is luck. ADR 0011 made zoom unbounded in both directions, which turns that from an annoyance into a guarantee of failure.

Decide:

- **Whether hit regions are a separate geometry from rendered geometry**, and where that lives. Excalidraw's `hasBoundingBox` deliberately governs the affordance *and* the hit test from one place so the two cannot drift; edges in every reference tool have a hit stroke wider than their visual stroke.
- **Precedence between overlapping participants.** tldraw hit-tests overlays before shapes and writes its vertex-beats-virtual rule down explicitly rather than inheriting it from paint order, which is exactly what D12Canvas does today via markup order and the stacking tie-break.
- **Whether anything is drawn but not hittable, or hittable but not drawn** — and whether a target too small to hit should be dropped from the set entirely rather than rendered at an unusable size.
- **Whether entities can be removed from pointer hit-testing while staying keyboard- and panel-reachable.** All three tools that document locking implement it exactly this way, so if this seam exists, locking is nearly free — decide whether to take it here or leave it as a later feature built on the seam.
- **How the seam is expressed so ticket 01 can consume it**: does a hit produce a target, a ranked list of candidates, or a classification?

Scope boundaries, so this does not re-litigate its neighbours: the *sizing* of ports and resize handles specifically stays in ticket 06, which already owns hit-target sizing under zoom. The press-becomes-drag movement threshold stays in ticket 07. This ticket owns what is hittable and how the region is derived; those two own what happens once something is hit.
