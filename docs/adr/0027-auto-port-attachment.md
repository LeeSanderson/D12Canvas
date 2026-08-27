# An edge may attach to a component without naming a port, and the canvas keeps choosing the side

ADR 0005 gave an edge endpoint two shapes: attached to a named port, or floating at a fixed point. Ticket 03's teardown found this to be the largest single divergence from the reference bar, every one of Miro, FigJam, tldraw and Excalidraw having an auto state that `EdgeEndpoint` cannot express. This adds that state, decides what the common drop gesture produces, and leaves routing and endpoint choice independent.

## The common drop currently produces an endpoint that lies

`CompletePortDrag` resolves a drop through `Board.FindPortNear(dropPoint, PortHitRadius)` with `PortHitRadius = 10` **board units**, measured to an actual port point. A drop anywhere on the body of a shape larger than roughly 20×20 units misses all four border centres and falls through to `new FloatingEndpoint(dropPoint.X, dropPoint.Y)`.

That endpoint sits inside the shape and looks attached. It tracks nothing. Move the shape and the edge end stays behind on empty canvas.

So the gap is producing a wrong answer, not merely a missing one — which is what justifies this decision rather than four tools agreeing. ADR 0005 diverged from convention for a stated reason (the user wanted concrete, named, addressable connection points) and that reason still holds; nothing here overturns it, and after the side rule below **every attachment is still a named port**. What changes is who names it.

## A fourth endpoint shape, carrying only the component id

`IEdgeEndpoint` gains `AutoPortEndpoint(Guid ComponentId)` — a component named, no port named.

A shape rather than a `PortId.Auto` value. The codebase already faced this choice once and wrote the answer on `PortEndpoint` itself: *"A runtime-added custom port is `CustomPortEndpoint` instead — a separate `IEdgeEndpoint` shape, not a widening of this one's `PortId` field."* The sharper argument is that `PortId.Auto` would be an enum member every consumer of the enum has to exclude — `StandardPorts.FractionOf` has no fraction to return, `StandardPorts.All` must not list it, `FindPortNear` must skip it, `ComponentContainer` must not draw it, and ADR 0010's arrow-key port navigation must step over it. Five exclusions to make one value mean "not one of these". The fraction-of-bounds invariant is what ADR 0005 built the port model on, and this case is defined by not having one.

It carries **the id and nothing else**. No normalized anchor, which is the one place this diverges from tldraw, whose binding holds a `normalizedAnchor` beside an `isPrecise: false` that tells the reader to ignore it. This model already has a fractional attachment shape with an owner, a persisted form, an undoable creation command and a keyboard route; an anchor on the endpoint would compute `Bounds.PointAtFraction` down a second path from a second owner for the same result. ADR 0016 settled the same question the same way, making edge colour nullable so absence means no author opinion rather than a value. tldraw needs the anchor because its promotion is in-gesture — you drag *inside* the shape and the binding sharpens to the point under the pointer — where this model's precise path is a different drop target, so nothing is in flight to preserve.

**`CustomPortEndpoint` and custom ports therefore survive**, as the mechanism for an arbitrary point along a border. The cost is stated rather than glossed: a user cannot say "attach roughly two-thirds along the bottom edge, and do not make a port for it". They take auto, or spend one port-strip double-click on a port that is then addressable, undoable and persisted — which is the thing ADR 0005 said the user wanted. Excalidraw's per-endpoint handle is genuinely cheaper in persisted state and is declined.

**`IEdgeEndpoint` gains one member**, `Guid? ComponentId { get; }` — the three attached shapes return theirs, `FloatingEndpoint` returns null. `BoardJsonSerializer.MissingComponentIds` already carries two near-identical blocks, one per attached shape, and this case would make three; ADR 0013's interior-edge test wants the same accessor. This turns a marker interface into a real one, contradicting its own comment, and is recorded here rather than appearing during implementation.

## Aiming from the centre, attaching at a side

An auto endpoint resolves in two phases.

**Every endpoint has a reference point.** A `PortEndpoint` or `CustomPortEndpoint` its resolved port point, a `FloatingEndpoint` its own point, an `AutoPortEndpoint` its component's **centre**.

**An auto endpoint then attaches at the standard port on the side the aiming line crosses.** The aiming line runs from this component's centre to the other endpoint's reference point; whichever border of `Bounds` it crosses names the side, and that side's standard port is the attachment.

The centre is an aiming reference and **never an attachment**. One degenerate case: when the other end's reference point lies inside this component's rect — overlapping shapes, or an endpoint dropped on top — there is no crossing, and the attachment is the standard port on the side nearest that point. From an *interior* point the nearest side is always a face and never a corner, so this has none of the corner behaviour that ruled out nearest-point in the general case. Ties break in `StandardPorts.All`'s own order, which makes the rule total: two coincident centres, or a zero-area rect whose four ports coincide anyway, both resolve without a special case.

**Auto chooses among the four standard ports only, never a custom one.** A custom port is a deliberate placement, and having it silently start catching edges nobody aimed at would make adding one a liability.

Attaching at a port rather than at the crossing point itself is what keeps this cheap and keeps it explicable:

- **No new geometry.** Choose a `PortId`, then the existing `StandardPorts.FractionOf` and `Bounds.PointAtFraction` path does the rest.
- **No new gap.** The library knows only `Bounds`, so a crossing point would leave an auto edge on a circular component stopping at the bounding box and short of the outline at the diagonals. A standard port position is exactly where a pinned endpoint sits, so an auto endpoint introduces no visual case that pinning does not already have.
- **The router never learns auto exists.** Both attached kinds hand a `PortId` to whatever draws the line.
- **Auto and pinned become the same geometry**, differing only in whether the choice is re-made.

The accepted cost is that the attachment **jumps rather than slides**: drag the far end past a diagonal and the edge flips from one port to the next. Four flips per full orbit is more visible than the corner behaviour rejected above, and this ADR takes it deliberately — Miro's `snapTo: auto` flips the same way and reads as decisive. Two auto edges arriving from similar directions land on the same port and overlap exactly, where a continuous crossing point would have separated them; Miro has that problem too and there is no cheap fix.

**Resolution becomes pairwise.** An auto endpoint cannot resolve alone, so `Board.ResolveEndpoint`'s unary signature gains the other end. ADR 0020's two named entry points both inherit that — committed on `Board`, live on `LiveGeometry` — and the choice of source stays un-threaded exactly as 0020 requires. There is no circularity and no iteration to a fixed point: both reference points are centres, which do not depend on any attachment, so both-ends-auto resolves in one pass and source and target compute identically.

## Three drop zones

| Where the drop lands | Endpoint produced |
|---|---|
| Within a port's hit radius | that `PortEndpoint` / `CustomPortEndpoint` — pinned, re-chooses never |
| Anywhere else over a component | `AutoPortEndpoint` on that component |
| Over nothing | `FloatingEndpoint` at the drop point |

Miro's two visible drop targets plus the existing fall-through. The ordering is the point: **the easier gesture gives the more forgiving result.** Hitting a small circle is the deliberate act and it is the one that pins. Today the difficulty runs backwards — you must hit the small target to get an endpoint that tracks anything, and missing it silently gives you one that tracks nothing.

Three rules that would otherwise be implied:

- **Zone 2 uses the same participation predicate as a press.** ADR 0017 gave entities a press margin rather than a floor; this reuses it. A second, differently shaped notion of "over a component" is the two-competing-precedence-systems failure that ADR held the line against.
- **A locked entity is not a drop target.** ADR 0017 keeps it out of primary-press hit-testing and a connector drag is a primary press, so a drop over one falls through to whatever is behind it, or to floating.
- **A drop on the component the drag started from produces nothing.** `CompletePortDrag` guards with `!resolved.Equals(source)`, and structural equality catches a drop back on the same port. It cannot catch `AutoPortEndpoint(A)` against `PortEndpoint(A, Top)` — different values — so today's guard would create an edge from A's top port to A's own auto endpoint, which the aiming rule resolves back to roughly that same port. A degenerate stub, undoable and pointless. **The guard widens from same-endpoint to same-component**, and self-loops stay out of scope because no routing style can draw one.

Both zone rules apply to `ApplyEdgeEndpointEdit` as well, so dragging an existing endpoint onto a body converts it to auto.

**A single drag can only produce auto at the target.** `StartPortDrag` begins from a port, and under ADR 0018 a drag from an instance body is `MoveSelection`. The type system already agrees: `PortRef` is a closed `Standard | Custom` union and needs no third case. Auto at the source stays reachable in two gestures — create the edge, then drag its source endpoint onto a body — so the model is fully reachable, just not in one gesture. Whether a connector drag should be startable without naming a port belongs with the port affordance work, which owns what affordances exist.

## The drop resolves in C#, the press classifies in JS

ADR 0017 resolves a press in JS, walking up from `event.target` to the nearest marked element. That mechanism cannot serve this drop: ADR 0018 puts pointer capture on a stable element for the whole gesture, and a captured pointer's events are dispatched to the capture element, so `pointerup.target` is the capture element rather than what the pointer is over. The drop has to be re-derived at release either way.

**It is derived in C#**, extending what `FindPortNear` already does.

ADR 0017's argument against C#-side geometry does not transfer. It refused geometry because the library knows only `Bounds` and goes blind inside an author's component — which mattered at press, where `author-content` earns different synchronous treatment. At a drop, `instance` and `author-content` fall in the same zone. The distinction C# cannot make is one this gesture does not need.

What C# buys:

- **Assertable without a browser.** The resolver is a pure function of `Board`, a board point and the current scale, so port-versus-body, the locked fall-through and the same-component guard are all reachable in xUnit. Under a JS route the port-versus-body distinction becomes browser-only. ADR 0025 paid real cost for exactly this property, taking `InternalsVisibleTo` for one thing so that "does the edge follow the shape" would not degrade into a question about a `style` attribute.
- **Chrome cannot swallow the drop.** ADR 0017 made `.selection-bounding-box` a real hit region with a transparent fill and `pointer-events: auto`. A connector drag usually starts from a selected shape, so under `elementFromPoint` the topmost element over a selected target would be that box. C# never consults chrome, so the problem does not arise and needs no suppression rule.
- **The concentric collision cannot misdirect a drop.** C# never sees resize handles, so proximity to the port decides, which is what a user dragging a connector means. That settles the collision for this gesture without waiting on the port affordance work.
- **No interop and no amendment.** `CompletePortDrag(clientX, clientY)` already receives the drop in C#, so ADR 0018's release payload gains nothing.

**`PortHitRadius` becomes screen-constant.** ADR 0025's relationship test flagged it as board space by its own comment, covering 2.5 screen pixels at 0.25× zoom, at the zoom where aiming is hardest. It becomes `N / scale`, joining the screen-pixel ordering alongside the drag threshold, the snap radius and the edge hit band.

**`N` is owned by C# and published to CSS.** ADR 0017 sizes a port's hit region as `calc(Npx / var(--d12-scale))`, so defining the number twice would let the visible target and the actual target drift — the two-competing-systems failure relocated from ordering to sizing. `ContentStyle` already publishes `--d12-scale`; it publishes the port region size the same way. One radius, one owner, and both descriptions are then the same circle.

The asymmetry is principle rather than accident. **The press classifies in JS because it needs the DOM and must decide before the interop hop; the drop resolves in C# because it needs `Board`, carries no synchronous obligation, and should be provable without a browser.**

One consequence for the port affordance work: ADR 0017's rule that affordances leave the render set and the hit set together now governs zone 1, so **the visibility decision is a decision about where pinning is reachable at all.** Ports hidden on an unhovered shape means a drop there gives auto; ports shown during a connector drag means it can pin.

## Routing and endpoint kind stay independent, and the router owes a direction

`RoutingStyle` and endpoint choice remain independent per-edge fields, as ADR 0005 has them.

The reference coupling — Figma's *"Elbowed connectors may use all magnets besides CENTER"*, Excalidraw's midpoint magnets existing only for elbow arrows, tldraw's elbow-only directional handles — is a statement about **what a router reads**, expressed as a constraint on data because those routers are direction-aware. An elbow router must choose a direction to leave a shape, so a side is meaningful input and a centre is not. A straight line has no direction to choose.

Ours reads neither. `EdgePathD` is unconditionally horizontal-first:

```
Orthogonal: M f.X f.Y  L midX f.Y  L midX t.Y  L t.X t.Y
Curved:     M f.X f.Y  C midX f.Y  midX t.Y  t.X t.Y
```

Nothing consults `PortId`. So an orthogonal edge attached to a **Top** port leaves that port sideways and immediately turns, and `Curved` bends the same way — wrong today, with named ports only, with nothing from this decision involved.

The pair the ticket called ill-formed therefore splits. `Straight` + `PortId.Top` is the everyday case and looks right. `Orthogonal` + any port is broken, and it is broken because the router ignores the side. **A legality rule would hide that bug rather than fix it**, and coupling the fields would make `ChangeEdgeStyleCommand` coerce endpoints — a style command rewriting what an edge is connected to, with undo restoring attachments the user never knowingly changed.

The auto endpoint gives a direction-aware router **better** input than a named port does: its side is derived from where the other end currently is, so it can never face away from the thing it connects to. A hard-coded `Top` can. Fixing the router is its own ticket.

## The keyboard's guessed side becomes a derived one

`OnEnterPressed` seeds port picking with `_portFocusEndpoint = new PortEndpoint(id, PortId.Top)` — an arbitrary side, guessed and pinned, which is this ADR's own defect in a different input path. **The stage-one default becomes `AutoPortEndpoint(id)`.**

No new binding. The keyboard already has the two-stage shape the pointer has, and the two paths end up saying the same thing in their own idiom: the pointer distinguishes by *where you release*, the keyboard by *how far you drill*, and both mean "did you name a side or not". Arrow keys and `Space` still reach every pinned option, so nothing is lost, and the tentative choice now moves with the geometry instead of always pointing up.

**Auto joins the front of the `Space` cycle** — auto, then the four standard ports, then any custom ones, wrapping back to auto. `AdvanceToNextPort` does `ports.IndexOf(_portFocusEndpoint)` and would otherwise return -1 for auto, landing on `Top` by accident and leaving auto reachable only by pressing `Escape` and starting the pick again; a state you can leave but not re-enter is a trap. **The cycle list is built at the call site, not inside `Board.AllPorts`** — `FindPortNear` shares that method and must never see an entry with no fixed point to measure against.

This closes the parity gap ADR 0026 exists to close: without it the pointer could express an endpoint kind the keyboard could not.

## Persistence needs no new field and no version bump

`EdgeEndpointEnvelope` is `(ComponentId, PortId, X, Y, CustomPortId)` under the convention that exactly one combination is populated. The combination "`ComponentId` present, `PortId` null, `CustomPortId` null" is currently unreachable — `FromEndpointEnvelope` falls through to `throw new JsonException("The edge endpoint is neither port-attached nor floating.")`. The discriminator's one free slot already means *a component named, no port named*, which is exactly this case. One new switch arm, no format change.

**No `SchemaVersion` bump, and a bump is not available.** `EnsureSupportedSchemaVersion` throws unless `schemaVersion == CurrentSchemaVersion` — strict equality, not a floor — so moving to 2 would make every board ever saved throw `UnsupportedSchemaVersionException` on load. ADR 0016 declined a bump by preference; here declining is the only option that does not also require redesigning the version gate. Worth recording, because "bump the schema so an old reader knows" is the reflex and it is wrong in this codebase.

An old reader meeting a new board already degrades gracefully: the unknown combination throws inside `ParseEntries`, which records a `BoardDeserializeWarning` and drops that one edge while the rest of the board loads.

## How this is verified

Inherited from ADR 0025, and the assertions are relationships rather than magnitudes:

- An auto endpoint's resolved point is always one of its component's standard port positions, and never the centre unless the rect is degenerate.
- Moving the other end from one side of a shape to the opposite side changes the chosen `PortId`.
- The three drop zones are a table over the resolver, not a set of hand-written drags — the same reason ADR 0025 made the press-to-kind mapping a table.
- `PortHitRadius / scale` takes its place in the screen-pixel ordering.

## Amends, confirms

- **Amends ADR 0005.** Its rejection of *dynamically-calculated, unnamed anchor points* is **narrowed, not reversed**: nearest-point-on-perimeter is still not the attachment model, and every attachment is still a named port. Its endpoint set grows from two shapes to four, and its scope boundary on attaching to a `Group` is untouched.
- **Amends ADR 0010** in one place: the keyboard port pick's default is auto rather than `Top`, and the `Space` cycle gains a member.
- **Amends ADR 0017** in one place: `PortHitRadius` becomes screen-constant with `N` published through `ContentStyle`. Its role table, precedence rules and participation predicate are untouched and reused.
- **Confirms ADR 0018.** The release payload gains nothing and the gesture set is unchanged; the drop is resolved by the gesture that already owned it.
- **Confirms ADR 0020.** Pairwise resolution lands on both named entry points, and the choice of geometry source stays un-threaded.
- **Confirms ADR 0016's** nullable-means-no-opinion reading, applied to an attachment rather than a colour, and its no-bump precedent.

**Considered and rejected:**
- **`PortId.Auto`** — rejected; five separate exclusions across consumers of the enum, and a member of a fractional-position enum defined by having no fractional position.
- **A normalized anchor on the endpoint** (tldraw's `normalizedAnchor` plus `isPrecise`) — rejected; a second fractional-attachment representation with a different owner, and a stored value the reader is told to ignore. Also implies deleting `CustomPortEndpoint`, `AddCustomPortCommand`, `ComponentInstance.CustomPorts` and the port-strip gesture, which is a fresh effort rather than a bullet on this one.
- **Resolving to the component's centre** (Figma's `CENTER` magnet) — rejected; it very nearly works, because ADR 0016 established that edges paint beneath components and are occluded rather than crossing them, so the shape would clip the interior of the line for free. It dies on the default edge being `TargetArrow = Arrow`: the arrowhead would be drawn under the shape that occludes it, so a directed edge arriving at a shape would show no arrow. On a transparent built-in such as `Text` the line and arrow would cross the glyphs instead.
- **Nearest point on the perimeter to the other end** — rejected as the general rule; the nearest point on a rect to an exterior point in a corner region *is* the corner, so the attachment sticks there while the other end orbits and then jumps, giving four dead zones per shape. It also minimises edge length, making the line graze the shape rather than point at it. Kept for the interior degenerate case only, where the nearest point is always on a face.
- **Attaching at the crossing point rather than the crossed side's port** — rejected; needs a rect-segment intersection nothing in the codebase has, reintroduces the bounding-box gap on non-rectangular components, gives the router a point instead of a side, and makes auto and pinned two different geometries. Its one advantage, sliding continuously instead of flipping, is the cost this ADR accepts.
- **Resolving the drop in JS via `document.elementFromPoint`** — rejected; it would make port-versus-body browser-only, let `.selection-bounding-box` swallow drops from the shape the drag started on, hand the concentric resize-handle collision a way to misdirect a drop, and widen ADR 0018's release payload. Its advantages (paint order for free, one geometry with the press) are weaker than they look, since instance stacking is driven by `ZIndex` in CSS and C# sorting by `ZIndex` therefore agrees with what the user sees.
- **Coupling `RoutingStyle` to endpoint kind** (Figma, Excalidraw, tldraw all do) — rejected; the coupling describes direction-aware routers, ours reads neither field, and the combination it would forbid is drawn wrongly for an unrelated reason. It would also make a style change mutate attachment.
- **Bumping `SchemaVersion`** — rejected; the version gate is strict equality, so a bump breaks every existing board rather than signalling anything.
- **Auto choosing among custom ports too** — rejected; a hand-placed port would start catching edges nobody aimed at, making adding one a liability.

## Addendum (surfaced while resolving the port affordance ticket)

ADR 0028 settles the two things this ADR handed over, and confirms it in both.

**The consequence flagged above is taken in the narrow direction.** Ports render on a single selection and on the one component under the pointer during a live connector drag, never on hover — so pinning is reachable only on a component the pointer has already arrived at, and a drop anywhere else produces an `Auto endpoint`. That makes pinning a two-stage act where auto stays one, which is this ADR's own ordering rather than a cost imposed on it: the easier gesture gives the more forgiving result.

**A connector drag is startable without naming a port, and it needs no new gesture to be.** A port's hit region is a stretch of border rather than a circle, so the user grabs the side near the midpoint and never aims at the dot — Miro's pull-from-the-border, reached through geometry this ADR already assumed. The source is still always a named standard port, so **"a single drag can only produce auto at the target" stands unchanged** and `PortRef` still needs no third case. A drag from the body remains ADR 0018's `MoveSelection`.

`PortHitRadius`'s move to `N / scale` completes here at `N` = 24 screen pixels, with the port-span floor and the corner reserve derived from the same number so the one-radius-one-owner rule extends to everything on the border rather than the port alone.
