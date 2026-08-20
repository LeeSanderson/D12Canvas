# The active gesture publishes its geometry, everything reads it through one surface, and the commit writes back exactly what was previewed

Geometry that is in flight during a pointer gesture lives in a **gesture preview** — a small structure the owning gesture publishes once per frame — and every consumer that asks "where is this right now" reads it through **live geometry**, a single surface over `(Board, preview)`. `Board` is never written mid-gesture. At release the gesture commits the preview verbatim, so what the user watched is what the history entry records.

This replaces two mechanisms that solved the same problem twice and a third that didn't solve it at all. `EffectiveBounds` already derives an instance's in-flight bounds for group move and group resize, and is read by container placement, the selection bounding box and tab stops. The connector drag solves it separately with `_connectCurrentPoint` plus an `IsBeingEdited` suppression that the edge line and the edge label each consult independently. And a single-instance drag solves it not at all: `ComponentContainer` mutates its own `X`/`Y` parameters locally and nothing outside learns anything until `OnMoved` fires at release. That third gap is why an attached edge does not follow a moving shape, and why it does not follow a resizing one — one defect, reported twice.

Arbitration already removed the hard part. With gesture ownership on the canvas, the single-instance mover and the n-many mover are one gesture, so the single-instance case arrives on the group path and inherits whatever that path knows. What remains is deciding where in-flight geometry lives, who is allowed to read it, and what the release writes.

## A published map, not a derived function

The preview is **written** by exactly one thing — the gesture that owns the pointer — and **read** by everything. That asymmetry is the design.

Deriving it instead, as `EffectiveBounds` does today, forces the reader to know which gesture is running and how to interpret its state: the method is literally `if (_isGroupMoving) … if (_isGroupResizing) …`. That is a switch over gesture kinds sitting inside a geometry reader, which is what gesture arbitration refused for the pointer surface and for hit precedence, and this ADR is about to multiply the readers. Under a published map, the mover publishes translated bounds and the resizer publishes scaled bounds, each computing its own preview in its own file, and no reader branches at all. It also stops `ScaleWithinBoundingBox` being re-run per member per reader per render.

An overlay `Board` was the tempting third option, because it makes attached edges live with no change to any call site. Rejected: it wraps a gesture-aware decorator around the persisted model, and it makes "am I looking at live or committed state?" invisible at every call site — including the serializer's.

## Two typed slots behind one read surface

Walking the eight gestures for geometry that anything *outside* the gesture depends on leaves exactly two shapes. `MoveSelection` and `ResizeSelection` change rectangles. `DragEdgeEnd` changes a single point and no rectangle. `Pan` and `MinimapPan` move the viewport, which is already the tracker's. `MarqueeSelect`'s rect is read only by itself. `SelectEdge` and `Native` change no geometry.

So the preview carries:

- **Bounds overrides**, keyed by instance id. Many readers.
- **A pending edge line** — two absolute board points plus the id of the edge whose own line is suppressed, `null` when a brand-new edge is being drawn. At most one, by construction: one pointer, one gesture, one dragged end, so a map would over-model it. One reader.

The second slot exists so that **edge geometry has one live read path**: check the pending line, then the bounds overrides, then committed state. That folds the `IsBeingEdited`/`ConnectPreviewLine` special case into the same lookup that makes an attached edge follow a moving instance. Leaving `DragEdgeEnd` its own private state keeps two places to ask, and the cost of that is already on record: when the connector drag was built, the edge *line* was taught about it and then the edge *label* had to be taught separately. Every future reader of edge geometry — a property bar anchored to a selected edge, alignment guides, a normalized attachment model — is a fresh chance to be correct about shapes and wrong about edges, which is the exact bug class this ADR removes.

It also splits geometry from styling cleanly: the dashed preview stroke stops being a different thing that gets drawn and becomes "this edge has an override", so *what* is drawn comes from the preview and *how* comes from a class.

**Encoding the pending line as a `Bounds` was considered and does not work.** A rectangle spanning two points cannot say which corner is which — the two diagonals collapse — and an edge has `SourceArrow` and `TargetArrow`, so losing the orientation draws the arrowhead on the wrong end. Recovering it by allowing a signed extent would poison `Bounds` for every other consumer, since `Right`, `Bottom`, `Intersects`, `Union`, `ExpandedBy` and `PointAtFraction` all assume non-negative sides, and the marquee already normalises with `Math.Abs` for exactly that reason. An `Edge` also has no `Bounds` at all, so such an entry would *mean* something different from an instance's, forcing readers to branch on entity kind — the two-places problem again, disguised as one place. And a brand-new edge has no id to key it under.

The uniformity that idea was reaching for is real, and it belongs one level up: **no caller touches the slots.** The surface offers "the live bounds of this instance" and "the live position of this endpoint", and the slots are private behind them.

## Derived geometry leaves the model, and each derivation gets two named entry points

`Board` cannot consult the preview, so anything that turns an entity into a point or a rectangle cannot both live there and be live. Four derivations are affected: endpoint resolution, port proximity, a group's bounds, and viewport windowing.

Threading an optional bounds source through all four was rejected on its failure mode: every call site must remember to pass the live source, and forgetting reverts that one site to today's defect with no compile error and no test failure unless a test happens to drag that specific thing. That is the "correctness requires reading four mechanisms together" shape the arbitration work exists to eliminate.

Instead, each derivation exists **once**, with two entry points that name their source:

- **`Board`** keeps the committed entries. They are public, hosts already call them, and committed is what a host asking `Board` a question means.
- **`LiveGeometry`** holds the live entries, and is what the library's rendering path calls.

The shared logic is not duplicated — a group's recursive member walk, for instance, is one implementation parameterised by its bounds source, with a committed entry point on `Board` and a live one on `LiveGeometry`. What is deliberately *not* shared is the decision of which source to use, because that is the thing a call site should be forced to state.

`Board` also keeps everything that was never geometry: containment and attachment lookups, the per-instance port fraction list, z-index arithmetic.

The naming rule this rests on: **read `instance.Bounds` when you mean committed state — a command's `before`, the serializer, anything persisted — and go through live geometry when you mean what is on screen now.** Nothing prevents a raw read; the rule is what makes one legible instead of a trap.

That rule has a consequence worth banking, because getting it backwards inverts a whole class of commits: because `Board` is never written mid-gesture, `instance.Bounds` at release still holds the press-time value, so a command's `before` comes straight off the field and needs no separate snapshot. A commit that read `before` through live geometry would compare the preview against itself and record a no-op.

**The library calls a committed entry in exactly two places, both deliberate**: viewport windowing and content extent, for the reasons in the two sections below. Stating this rather than claiming the rendering path is purely live, because a reader who found those two call sites under an absolute claim would reasonably assume they were oversights.

## The preview is the truth

The commit writes the **last published preview, verbatim**. It does not recompute the gesture's result from the pointer positions.

Today's move commit does recompute, and it disagrees with what was shown: `MoveComponent` applies `SnapBounds` at commit while the drag previewed unsnapped, so with snap-to-grid on a dragged instance tracks the cursor and then jumps to the grid on release. `CommitGroupMove` does not snap at all, so the behaviour also differs by selection size — a divergence that has to be resolved by someone once the two movers collapse into one, and this is where it lands.

So **snapping moves into the tick**: the gesture computes its preview per frame with snapping already applied, and the release writes that. Three things follow.

**Alignment guides become possible at all.** A guide that shows a shape aligned while the commit lands it elsewhere is a lie the user watches happen. Live snapping is a precondition for that work, not a nicety.

**The commit stops being a second implementation of the gesture's own arithmetic.** `after` *is* the previewed value. There is no formula to keep in sync, so preview and commit cannot drift.

**Three scattered no-op guards collapse into one rule** — commit only the entities whose previewed bounds differ from their committed bounds. That covers a release that never promoted (there is no preview), a drag that returns to its origin, and a snap that rounds back to where it started, each of which currently has its own guard in its own place.

One snap per tick, applied to the selection as a **rigid body**, with no branch on selection size — which keeps members' relative offsets intact at any n and follows the align/distribute finding that you snap the *target coordinate*, not each result. Which coordinate anchors it, and the guide algorithm itself, belong to the alignment-guides decision; this ADR fixes only that snapping happens inside the preview rather than at commit.

This does **not** weaken the arbitration model's `final delta = release − press`. That identity is about the raw pointer delta JavaScript hands the gesture. Snapping is a transform the gesture applies after receiving an exact delta; the delta stays exact, the preview is what is adjusted.

## No pointer gesture creates a command before release

Elevated from convention — three commit paths that each happen to write once — to an invariant, because it is what makes everything above cheap.

**Revert is discarding a dictionary.** `pointercancel`, `Escape` and the arbitration model's loud `lostpointercapture` net all reduce to dropping the preview: committed state was never touched, so there is no inverse to apply and no snapshot to restore. One history entry per gesture stops being a discipline anyone can forget and becomes a property of where the state lives.

It has teeth, because it forbids a real alternative: recording intermediate commands per tick and coalescing them at release, which is how some editors get crash-safe live undo. That needs history surgery, and the history model already says never per intermediate frame — this makes explicit what that implied and ties it to the mechanism that guarantees it.

**The invariant is scoped to pointer gestures, and the keyboard's opposite approach is legitimate.** `NudgeCommand` creates its command on the first keydown, mutates `Bounds` immediately, and grows the same command in place via `Extend` for every repeat, so a held arrow key is one entry with no preview anywhere. That is correct rather than an exception: a keypress's result is fully determined the moment it is pressed, so there is nothing provisional to show, whereas a pointer move's result is provisional until release. Keyboard parity work should therefore *not* reach for the preview — doing so would add provisionality where none exists.

**The preview covers geometry only, and the residue is named.** Two gestures mutate live state that discarding a preview does not restore: `MarqueeSelect` replaces the selection on every tick, and press-time selection changes — including author content's additive select — happen before any preview exists. Selection revert therefore needs a pre-press snapshot, and belongs to the cancellation decision. Geometry revert is free; selection revert is not.

## Cadence, and a budget that is structural

Before this ADR, a single-instance drag re-rendered only its own container, pan renders were throttled by a `DateTime.UtcNow` check against a 16ms interval, and group move/resize ticks called `StateHasChanged` unthrottled on every `mousemove`. Afterwards every gesture tick re-renders the canvas, so cadence is no longer a per-gesture detail.

**Rate limiting moves into JavaScript: at most one `OnPointerMoved` per animation frame.** `pointermove` fires at device rate and a high-polling mouse or trackpad outpaces frames several times over. Coalescing at the source puts the limit in one place rather than having each consumer re-decide it, and it is the same principle already applied to the movement threshold — JavaScript owns the rate, C# receives meaningful events. The C#-side interval check goes away.

**Every gesture derives from the press-anchored absolute delta, never an accumulated one.** This is what makes coalescing safe. Pan currently accumulates, resetting its anchor each tick, so it must see every event or lose motion — its own comment says so. Expressed as `panOrigin + (current − press)`, a dropped frame costs nothing, and the arbitration model's press-anchored delta already supplies it. The reset-anchor pattern disappears with it.

**An edge becomes its own component, with `ShouldRender` comparing its resolved endpoints.** The same trick containers already use and the same split the minimap already made. Without it, dragging two instances on a five-hundred-edge board recomputes and diffs every edge every frame; with it, that is five hundred cheap parameter sets and real work only where an edge touches a participant.

**The budget is stated structurally rather than in milliseconds**: one frame per pointer move, with per-frame work proportional to the gesture's participants and the edges touching them, not to board size. A millisecond figure would be device-dependent and untestable in CI; this one is assertable, and it is the same shape as the minimap's "a pan costs one transform write".

A useful side effect: because the preview is a data structure rather than pixels, live geometry is assertable without a browser. "Does the edge follow the shape" becomes a unit-level question about the preview's contents, which is what the interaction-verification work has to build on.

## Mounting and level of detail both stay committed, for different reasons

**Viewport windowing reads committed bounds.** The reason is not fidelity, it is that committed bounds do not change until release, so a gesture can never unmount its own participant — mounting is stable across a move or resize with no pin rule to write or forget. Reading live instead would make an instance dragged past the viewport plus overscan vanish from the user's hand; the gesture would survive it, since capture sits on `.diagram-canvas` precisely so an unmount cannot break it, but the user would be dragging nothing. This correctly leaves `Pan` alone: mounting depends on the viewport *and* committed bounds, and it is the viewport that a pan changes, which is the entire point of panning.

**The level-of-detail state of a gesture's participants is frozen at press and re-evaluated at release.** The threshold has no hysteresis, so holding a resize handle at the boundary would mount and unmount the author's whole component tree at frame rate. Even a single crossing is destructive, because unmounting an author component discards whatever internal state it holds — a scroll position, an open dropdown, focus. Hysteresis would stop the flapping but not that.

This looks like an exception to the preview being the truth and is not: the preview governs *where* things are, not the fidelity they are drawn at, and because the commit writes the previewed geometry verbatim, the state resolved at release is the correct one for what the user was actually manipulating. The honest cost is a small pop: resize something to four pixels and it stays a mounted component rendered tiny, then becomes a placeholder on release.

Both rules have one reader and no branch on gesture kind, because a non-participant's geometry does not change and so needs no decision either way.

**The unbounded-zoom/LOD decision is confirmed, not amended, and the deviation was in the code.** That ADR specifies the cutoff as "computed at render time from a component's own bounds and the current scale" — and `Bounds` is the committed field. Reading committed bounds during a gesture is what it literally says; feeding `EffectiveBounds` into the threshold test is where the code drifted from it. This is the third time on this effort that reading an ADR rather than a summary of it changed the answer.

## The minimap and content extent stay committed on purpose

The minimap maps the whole content extent into a small rect, so a two-hundred-unit drag is typically sub-pixel there, and its box layer already sits behind a `ShouldRender` keyed on board revision. A preview is not a board revision, so **committed reading is that design working as intended** rather than something this ADR changes: the boxes settle at release, and no per-frame invalidation of an uncapped box layer is bought for motion nobody can see.

The split falls along a line that is already true: the minimap's **viewport rect stays live** because it reads the tracker, not the preview. So during a pan the minimap tracks exactly, and during a move or resize its content settles at release.

Content extent goes the same way, since it is the same shared computation, and framing mid-gesture would move the viewport out from under a live gesture.

Recorded with its reason attached, because without one it reads as an oversight and someone will eventually "fix" it.

## What this amends and what it confirms

**The pointer gesture arbitration model is amended in one place**: `OnPointerMoved` is rate-limited in JavaScript to at most one call per animation frame. The four `[JSInvokable]` methods keep their shape and the threshold rule is untouched — this constrains the rate at which the second of them fires. Declared rather than left for a reader to discover, since that ADR's own promise is that a press which never promotes produces exactly two calls.

**The undo/redo history model is confirmed and strengthened.** One entry per gesture now follows from where the state lives rather than from three commit paths behaving. Its scope is clarified in one respect — the no-command-before-release invariant binds pointer gestures, and the keyboard's write-through-and-extend route satisfies the same one-entry rule by a different mechanism.

**The board state model is untouched.** No overlay, no wrapper, no change to what `Board` stores or how entities are shaped. Derived geometry gaining a second entry point elsewhere adds nothing to the persisted model.

**The unbounded-zoom/LOD decision, the viewport-commands and minimap decision, and the wheel device profile are all confirmed**, per the sections above. The last of these already ended the blanket `transition: transform 0.1s ease-out` on `.canvas-content` — it names a pointer drag as pure lag in as many words — so suppressing easing while a pointer gesture is active is that decision being honoured, not a new one taken here.

**The align/distribute decision is reused, not amended**: its finding that you snap the target coordinate rather than the result is what makes one rigid-body snap per tick work at any selection size.

## What this deliberately does not decide

**What happens when committed state changes under a live gesture** — an undo or an arrow-key nudge while a drag is held, or a host reassigning `Board`. The preview holds absolute bounds computed from press-time committed state, so an undo mid-gesture leaves the release writing a command derived from a state that no longer exists. Sharp enough to state precisely and therefore owed its own decision rather than a clause here; the leading candidate is that a command arriving during a pointer gesture cancels it first, which the revert invariant makes free.

The snap anchor and the guide algorithm belong to the alignment-guides decision, selection revert to the cancellation decision, and the property bar's own anchoring to the property-bar decision. Each of the three now has the live geometry it was waiting on.

## Considered and rejected

- **A derived in-flight-bounds function, as `EffectiveBounds` is today** — puts a switch over gesture kinds inside a geometry reader, and re-runs each gesture's arithmetic per reader per render.
- **An overlay `Board` that intercepts reads** — makes edges live with no call-site change, at the price of a gesture-aware decorator around the persisted model and an invisible live-versus-committed distinction at every call site, the serializer's included.
- **Keeping in-flight geometry in `ComponentContainer` with a push channel outward** — already dead: a container holds no gesture state, because a container is a box the pointer can leave and a gesture outlives the box.
- **A bounds-only preview, with `DragEdgeEnd` keeping private state** — leaves two places to ask where an edge is, which has already produced one defect and invites one per new reader.
- **Encoding the pending edge line as a `Bounds`** — cannot express which end is which, poisons `Bounds`' non-negative invariant if made signed, means something different for an edge than for an instance, and has no id to key on when the edge does not exist yet.
- **Threading an optional bounds source through `Board`'s four geometry methods** — every call site must remember, and forgetting is silent.
- **Moving only endpoint resolution** — leaves a group's own tab stop reading raw bounds while its members track live, and defers the same question.
- **Free-follow during the drag, settle at commit** (today's behaviour) — the release-jump under snap-to-grid, a commit that reimplements the gesture's arithmetic, and no honest way to draw an alignment guide.
- **Intermediate commands coalesced at release** — crash-safe live undo, bought with history surgery the history model rules out.
- **Live windowing** — the dragged instance vanishes from the user's hand once past the overscan margin.
- **Live level-of-detail evaluation for participants** — mounts and unmounts an author's component tree at frame rate at the threshold, discarding its internal state each time.
- **Hysteresis on the level-of-detail threshold** — stops the flapping, still unmounts an author component mid-gesture.
- **Feeding the preview into the minimap** — invalidates an uncapped box layer every frame to animate motion that is sub-pixel at minimap scale.
- **A public live-geometry surface** — no host needs to ask where something is mid-drag, `Board`'s committed queries answer the question a host actually asks, and additive exposure later costs nothing.
