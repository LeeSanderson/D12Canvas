# A border is partitioned along its length, ports appear on selection and on the drop target, and the cursor is the only thing that draws the partition

ADR 0005 specified four standard ports and said nothing about their affordance. The code filled that silence with hover, and put a 20px port, a 10px resize handle and a 14px port strip concentrically at all four border midpoints, separated only by markup order. This decides when a port is visible, where it sits, how big it is, and what happens when a user crowds a side with custom ones.

## Hover was carrying capability, and capability cannot live there

Ticket 03 read four tools against their own sources and found one rule behind all of them: **hover reveals identity, selection reveals capability, a gesture in progress reveals compatibility.** D12Canvas uses hover for capability and has no compatibility feedback at all.

Hover is not merely the wrong slot, it is an unusable one. tldraw suppresses its hover indicator outright on coarse pointers, which is the honest answer to the touch non-foreclosure constraint: **nothing load-bearing may live on hover**, because on touch there is no hover to live on. ADR 0026 arrived at the same place from the keyboard and stated it as a constraint on this ticket: ports must be reachable and visible without hover, because ADR 0010's mouse-free connector attachment already depends on it and that code is live rather than planned.

**Ports render on selection, and on the one component under the pointer during a live connector drag.**

Selection and focus are the same trigger here, so this is one rule rather than two: ADR 0010 settled focus-follows-selection, which collapses the seed note's "focus, or an active connector drag" into "selection, or an active connector drag". Recorded as the accessibility improvement ADR 0026 asked for: a keyboard user reaching an instance by Tab now sees its ports, where previously the pick was aimed at affordances only a mouse could summon.

Miro is the only reference tool that ships both affordances, and **its documented trigger is selection** — "hover over a blue dot that appears on the sides of a *selected* shape". That the dots also appear on plain hover is observed rather than documented, and is the part we are declining.

**On a multi-selection a member shows nothing**, matching the existing `ShowSelectionOverlay` gate: the shared bounding box already owns resize, and a connector pulled from one member of a set has no defensible meaning. Drop candidacy is independent of selection, so a member of a multi-selection still lights up as a target.

### Which components light up during a drag, and what that costs

Only the one under the pointer. The alternative — every component on screen — makes pinning reachable everywhere with no aiming precondition, and puts four dots on every shape at the moment the user is trying to see where a line is going. ADR 0024 already found that this canvas has to run its full-bleed feedback quieter than the reference tools do, for the same reason.

The cost is real and is taken deliberately, because ADR 0027 made visibility a reachability decision rather than a discoverability one: **wherever a port is not rendered it is not hit-testable, so a drop there produces an `Auto endpoint` and pinning is unavailable.** Under this rule pinning is available only on a component the pointer has already arrived at, which makes pinning a two-stage act (arrive, then aim) where auto stays one. That ordering is the one ADR 0027 wanted anyway: the easier gesture gives the more forgiving result.

## The collision is a specification failure, not an aesthetic one

The geometry is exact. `.port-top` is a 20px circle at `top: -10px; left: calc(50% - 10px)`; the `.top` resize handle is a 10px circle at `top: -5px; left: calc(50% - 5px)`. The handle is a concentric disc wholly inside the port, winning on paint order because it appears later in `ComponentContainer.razor`. The 14px `.port-strip` runs the full length of the side underneath both. Three gestures per border midpoint.

WCAG 2.2 SC 2.5.8 lets undersized targets pass only if "a 24 CSS pixel diameter circle is centered on the bounding box of each" and "the circles do not intersect". **Concentric targets fail that at every zoom, by construction.** So ADR 0017's holding position — resize handle beats port, preserving today's behaviour — cannot be the end state, and it said as much: it expected this work to separate the two geometrically so the rule almost never fires.

The teardown found no reference tool has this problem because none of them creates it. Miro's resize nodes are white and on the corners; its connection dots are blue and on the sides. Different geometry, different colour, different trigger, disjoint territory. tldraw and Excalidraw have no connection points at all. Figma stacks rotation on the space outside the box.

## One computation per side

A side is a line. Everything on it is a port. Resize gets what is left.

1. The side's **run** is its length in screen pixels minus a corner reserve at each end. Corner handles are not on the side.
2. Every port on that side — the standard one at 0.5 plus any custom ones — takes the stretch of the run **nearest to it**, capped at the port target width.
3. Two ports closer than that cap split the difference at their midpoint. Neither swallows the other.
4. Resize takes the leftover runs.
5. Anything below the floor **drops from render and hit together**, which is ADR 0017's existing rule applied with no exception for ports.

Miro's layout falls out of this as the small-shape case rather than being written as a rule: shorten a side until it holds one span and every side is entirely port with resize at the corners. Today's stack is not reachable from it at any size.

This works only because the gesture is 1-D. `AddCustomPort` pins the perpendicular fraction to the side's own 0 or 1, so although `PortDef` is a 2-D fraction, **every custom port the product can create lands exactly on a border.** Nothing produces an interior one, and this ADR does not add a way to.

### Resize is residual, and that is a cost rather than an oversight

Ports take their space first. Add enough custom ports to a short side and its resize spans fall below the floor and vanish, with nothing on screen explaining where they went; the only recovery is zooming in, which works because every span is measured in screen pixels. **A user can bury their own side-resize affordance.**

Corner resize is the one thing that can never be covered, and it is protected structurally rather than by a precedence rule: the corner reserve is subtracted before any port is placed, so a port span and a corner target cannot intersect whatever fractions the user picks. Two-axis resize therefore survives every configuration, at every zoom at which the shape has a border at all.

### Crowding cannot cost you a standard port

With a cap of `N` and a floor of `N/2`, two ports closer than `N` apart both fall below the floor and both drop — including the standard one. Adding a custom port near a border midpoint would then delete the port that `Auto endpoint` resolves to and that ADR 0010's arrow-key pick lands on first. Losing a built-in affordance as a side effect of adding your own is the wrong failure.

**In a below-floor collision the standard port keeps its full `N` and the custom port is clipped.** A precedence, not a special case, and justified by what the two things are: a standard port exists on every instance and is auto's target, where a custom port is a deliberate placement. It is the same move tldraw writes down for its own overlapping handles, and ADR 0017 already established that where two targets compete the precedence is written in one place rather than inherited from markup order.

## One number, and everything else derived

**`N` = 24 screen pixels, the port target.** Defined in C# and published through `ContentStyle` beside `--d12-scale`, which ADR 0027 already requires so that the circle the resolver measures and the circle the CSS sizes cannot drift. 24 is WCAG 2.2 SC 2.5.8's minimum, so the criterion is met by construction.

Derived, so there is nothing else to tune and nothing that can fall out of step:

- **Floor = `N / 2`.**
- **Corner reserve = `N`**, making the run `[N, length − N]`.
- **Corner hit target = `N`.**

`PortHitRadius = 10` **board units** dies here. ADR 0025's relationship test flagged it as the one holdout from a screen-constant family, covering 2.5 screen pixels at 0.25× zoom, at exactly the zoom where aiming is hardest.

Visuals keep the sizes they have and are decoupled from all of it: the port dot stays 20px, the corner handle stays 10px, and both **clamp below 0.25× zoom while their targets do not** — tldraw's split, whose own comment is the clearest statement of it, that the visible circle stops growing past 25% zoom "while the hit-area halo above still tracks the full zoom so handles remain grabbable". Under ADR 0011's unbounded zoom-out that stops being a refinement.

The ordering ADR 0025 asserts, which this joins:

> drag threshold 4 < object-snap radius 8 < floor 12 < edge hit band 20 < port target 24

**ADR 0026's grid nudge step cannot join it.** Its dominant cell runs 6.3 to 63 screen pixels, a range that straddles every member above, so there is no relationship to assert. That is a property of the decision rather than a gap in it: ADR 0026 deliberately made the step follow the grid instead of pinning it.

One case needs no rule. A default 200×140 instance loses every port at about 0.34× zoom, because its short side can no longer hold a run; ADR 0011's `LodSizeThreshold` does not swap it for a placeholder until 0.16×. **The affordance floor always bites before the LOD threshold**, so an `LOD placeholder` is never asked whether it has ports, and ADR 0017's rule that content stays hittable while affordances drop needs no port-specific clause.

## The cursor is the affordance, and it costs nothing

The partition is never drawn. Ports are dots at their fractions, as today; the spans are invisible.

That leaves **no drawn handle on a side at all**, which the prototype exposed and no amount of argument would have. Nothing on a selected shape says its sides are resizable. This is precedented rather than novel — Figma and tldraw both use invisible edge-resize zones and let the cursor carry it — and it is why the cursor becomes load-bearing here rather than staying in the map's micro-feedback fog.

| Region | Cursor |
|---|---|
| Port span | `crosshair` |
| Resize span | `ns-resize` / `ew-resize` |
| Corner | `nwse-resize` / `nesw-resize` |
| Instance body | `move` |
| Author content | none; `Native` keeps its own |

It is free. ADR 0017 already makes every hit region a real element, so `cursor:` is one CSS declaration per class. No hover classification, no `pointermove` listener, and nothing added to the five synchronous role-derived decisions ADR 0018 makes before the interop hop.

It also buys a property none of the drawn variants had: **the cursor draws the partition only where the user is looking.** Sliding along a side reads resize, crosshair over the dot, resize again, which teaches the boundary without putting it on screen. On a crowded side that is a run of cursor changes coinciding with visible dots, so the flicker is information rather than noise.

**Every hit region still belongs to something visible**, which ADR 0017 requires and which invisible spans risk violating. A port span's visual is its dot. A resize span's visual is the shape's own border, which is drawn. Neither is a halo over nothing.

## Adding a custom port loses its hint and gains a menu row

One element gets one cursor, and on a resize span the drag meaning has to win, so `.port-strip`'s `cursor: copy` — the only thing that has ever suggested a border can be double-clicked — goes.

The gesture itself is unharmed and is better off. ADR 0018 dispatches double-click off the press count carried on ADR 0017's classification, and its threshold means a stationary press never promotes, so a double-click and a drag share the same span with no ambiguity. That is a real improvement on today, where the strip sits under both the port and the handle and is reached only where neither covers it.

**The object menu gains an Add port here row, eligible only when the press that opened the menu landed on a border span.** ADR 0022 already stores the press point, which is exactly the fraction the port needs; ADR 0023 already made row eligibility per-item rather than per-context, so this is one predicate and no new mechanism. It adds no command type: `AddCustomPortCommand` exists.

The row is **pointer-only by construction** — a menu opened by `Shift+F10` has no press point, so it names no side and no fraction, and the row is ineligible and therefore invisible. That leaves a gap this ADR found rather than created: `AddCustomPort` has exactly one caller in the library, and neither ADR 0010 nor ADR 0026 mentions adding a port, so **a keyboard user cannot add a custom port by any route.** Inventing a keyboard fraction-picker is a decision of its own and is carried as [Keyboard route to adding a custom port](../../.scratch/canvas-interaction-quality/issues/43-keyboard-add-custom-port.md).

Separately, `CONTEXT.md`'s "small closed set" of commands lists eleven and omits five that exist — `AddCustomPortCommand`, `AddEdgeCommand`, `RemoveEdgeCommand`, `NudgeCommand` and `ResizeStepCommand`. The enumeration is corrected here. Whether the set is still meaningfully closed, and whether "not one bespoke class per gesture type" still describes the code, is left alone rather than answered in passing.

## A connector starts from the side, not from a dot

This answers the question ADR 0027 handed over: whether a connector drag should be startable without naming a port.

It is, and the partition delivers it rather than a new gesture. A port's hit region is a stretch of border, so the user grabs the side near the midpoint and never aims at a circle. That is Miro's "pull from the hovered shape's border" with the source still always a named standard port, so **ADR 0027's finding that auto at the source needs a second gesture survives untouched**, and `PortRef` still needs no third case.

A drag from the **body** remains `MoveSelection` under ADR 0018. No modifier changes it, and none is available: ADR 0024 spent `Ctrl` and `Shift`, and `Alt` is reserved.

## Touch needs nothing built

`N` reads from `pointerType`, which ADR 0017's classification already carries and which ADR 0022 named as the one dimension a screen-pixel constant is expected to vary by. Only the mouse value exists; the touch value is declared and left to be judged when there is a device to judge it on.

The layout follows from the same rule with one input changed. Side resize is a mouse-and-tablet affordance — Excalidraw's `canResizeFromSides` returns false on phones — so switching the resize spans off makes every side entirely port, which is Miro's layout. **No touch case is written anywhere**; the partition produces it.

## How this is verified

Inherited from ADR 0025, and stated as invariants rather than magnitudes:

- **No two hit regions on an instance's border intersect**, at any zoom, for any port configuration. This is the WCAG spacing criterion as a property test over the span computation, and it is the assertion that replaces ADR 0017's resize-beats-port precedence rather than a backstop being kept alongside it. A backstop for a case the geometry cannot produce is a rule no test can reach.
- Every surviving span is at least the floor, and the floor is `N/2` rather than 12.
- The standard port survives any port configuration on a side whose run is at least `N`.
- `N / scale` takes its place in the screen-pixel ordering.
- A port below the floor is still an ordinary tab stop and still resolves as an edge endpoint, which is ADR 0017's separation of pointer participation from keyboard reachability holding under a new consumer.
- Cursor is CSS, so an `Interaction probe` asserts the computed cursor over a point rather than a pixel.

## Amends, confirms

- **Amends ADR 0005**, supplying the affordance decision it left open. Its port model, fractional positions and custom ports are untouched.
- **Amends ADR 0017** in three places: `PortHitRadius` becomes `N` screen pixels with one owner (already begun by ADR 0027); the resize-beats-port precedence is **retired**, replaced by the non-intersection invariant; and the visual-clamped, hit-unclamped split it deferred is taken at 0.25×.
- **Amends ADR 0023** by one row, Add port here, on the object menu.
- **Confirms ADR 0027.** Visibility is settled the way it warned it would be, pinning is reachable only on the drop candidate, and its "auto at the source needs a second gesture" finding stands.
- **Confirms ADR 0018.** No gesture is added, no synchronous decision is added, and the double-click gesture is made unambiguous by the press-count dispatch that ADR already specified.
- **Confirms ADR 0026**, delivering the without-hover reachability it made a condition, and confirming that its nudge step is a range rather than a constant.
- **Confirms ADR 0011.** The affordance floor bites before `LodSizeThreshold`, so nothing here reaches the LOD decision.

**Considered and rejected:**
- **Ports on hover** — the status quo. Dies on touch before it dies on taste: tldraw suppresses hover affordances on coarse pointers outright, and ADR 0026 needs ports visible to a keyboard user who has never hovered anything.
- **Lighting every on-screen component's ports during a connector drag** — makes pinning reachable with no aiming precondition, at the cost of dressing the whole board at the moment the user is judging where a line goes.
- **Miro's partition exactly** — sides connect, corners resize, no side-midpoint resize at all. Rejected against the ticket's own constraint that resize stay reachable along every border; partitioning along the length keeps it and produces Miro's layout as its small-shape case anyway.
- **Splitting the border by inside versus outside** — resize inward, connect outboard. Keeps both at all four midpoints and uses Figma's outboard-space idiom, but the port dot has to be drawn clear of the border and then no longer sits where the edge attaches.
- **Offsetting the resize handles along the edge** — keeps both drawn at every side, at the price of either eight side handles or one at an off-centre position that reads as a mistake.
- **Keeping a declared precedence and leaving the geometry alone** — what the code does today and what ADR 0017 held as a stopgap. Fails SC 2.5.8's spacing exception at every zoom and does not fix the reported defect.
- **A proportional port span**, Excalidraw's `clamp(0.05 × height, 5, 80)` shape — rejected on crowding: with each port claiming a share of its side, the first custom port added to a long side halves it and wipes out side resize immediately.
- **Reserving guaranteed resize spans that ports may not occupy** — caps how many ports a side can hold and makes the add-port gesture refuse under conditions the user cannot see.
- **Making the cap and the floor the same number**, so every port span is exactly `N` or absent — brutally simple, and it makes the standard port vanish whenever a custom one is placed within `N` of it.
- **Drawing the partition** — either always, or only on a drop candidate. Both were prototyped. Both make side resize discoverable without a cursor and both put a new visual language on every selected shape; the cursor does the same work only where the user is looking, and costs one CSS declaration.
- **Leaving add-custom-port on the double-click alone** — the gesture keeps working, but it loses `cursor: copy` and with it the only thing that has ever suggested it exists.
- **A keyboard fraction-picker for adding a custom port** — the gap is real and is this ADR's find, but choosing a side and a fraction from the keyboard is a decision with its own shape, carried as its own ticket.

## Addendum (surfaced while resolving the create-adjacent-and-connect ticket)

**The port span's double-click for adding a custom port is retired by ADR 0030**, which puts `Quick create` on a plain click of the same span. The rest of this decision is untouched: the partition geometry, the selection-and-drop-target visibility rule, the cursor table and the single number `N` all stand.

This ADR argued the double-click was "unharmed and better off" because a stationary press never promotes, so a drag and a double-click cannot be confused. That argument is correct and is not what fails. It fails to extend to a third meaning: ADR 0018 reads the press count from `event.detail` on `pointerdown`, so a press-count-1 outcome fires before anything can know a second press is coming. Drag versus double-click is separated by movement, which the model holds when it decides. Click versus double-click is separated only by a press that has not happened yet.

What retires is therefore a route this ADR had already found undiscoverable. Removing `cursor: copy` was this decision's own move, the loss of the only hint that a border could be double-clicked was this decision's own observation, and the **Add port here** menu row was this decision's own answer to it. That row is now the sole pointer route, which makes [Keyboard route to adding a custom port](../../.scratch/canvas-interaction-quality/issues/43-keyboard-add-custom-port.md) sharper rather than merely still open: a keyboard user has no route, and a pointer user now has exactly one.

Two of this ADR's rules are confirmed by a consumer it did not anticipate. Because ports render only on a single selection, and because ADR 0027's consequence is that an unrendered port is not hit-testable, `Quick create` is reachable **only on a selected instance** and never on a member of a multi-selection. Both are Miro's and FigJam's documented behaviour, and neither needed a clause in ADR 0030 to produce.
