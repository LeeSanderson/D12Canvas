# Create-adjacent-and-connect

Type: prototype
Status: resolved
Blocked by: 06

## Question

Decide whether a selected instance offers a one-gesture "create a new instance beside me and connect it", and what affordance carries it.

Ticket 03 named this the single biggest widening beyond the ten seed notes: all four reference tools have it, D12Canvas has none of it, and the teardown's judgement is that it is **the gesture that makes a tool feel like a diagramming tool rather than a shape editor** — load-bearing for feel in a way that, say, a laser pointer is not. The destination was widened specifically to go looking for things like this.

The reference answers converge on the affordance and diverge on the trigger:

- **Miro and FigJam both hang it off the same side dot that draws a connector**, switching meaning by proximity — drag the dot to draw an edge, click it (or release it on empty canvas) to create and connect in one move.
- **Excalidraw binds it to `Ctrl/Cmd+Arrow`** ("Create Flowchart"), keyboard-first with no pointer affordance at all.
- **Miro adds `Tab` and `Enter` variants** in its mind-map and sticky flows.

That convergence is why this is blocked on ticket 06: the affordance it would hang off is the port dot whose visibility and geometry ticket 06 is deciding, and overloading a dot that is itself being redesigned would be deciding the same thing twice.

Prototype and decide:

- **The trigger.** Click-the-port-dot, drag-and-release-on-empty-canvas, a keyboard chord, or more than one. The proximity switch between "draw an edge" and "create and connect" is a feel question, not a rules question — Miro and FigJam make it work, and whether it feels ambiguous or obvious is exactly what a prototype settles.
- **What gets created.** The same component type as the source, the registry's default, or a picker. ADR 0009's placement model gives `DefaultSize` per registered type, so the type choice determines the size for free.
- **Where it goes**, and what happens when that position is already occupied — cascading offset (as ADR 0009's click-to-add already does), displacement, or nearest free slot.
- **Whether the new instance takes selection and focus**, which is what makes the gesture chainable into building a flow without touching the palette. Ticket 67 of `d12canvas-next` already established the focus-after-placement mechanism this would reuse.
- **Whether the whole thing is one history entry.** ADR 0007 holds a gesture is exactly one entry, and this creates an instance *and* an edge, so it wants `CompositeCommand` — confirm rather than assume.
- **Keyboard parity**, which ticket 16 owns generally but which Excalidraw's `Ctrl/Cmd+Arrow` answers directly for this gesture.

## Answer

**A plain click on a port span duplicates the instance beside it and connects the two** (ADR 0030). Named `Quick create`, borrowed from FigJam, the only one of the four tools that names the gesture.

**The ticket's premise was half wrong and the correction matters.** It says Miro and FigJam both hang this off the same side dot that draws a connector. FigJam does not: its connector tool is separate and sticky (`X`/`Shift+L`/`L`), and the blue dot is quick-create only. Only Miro stacks both meanings on one target, so the teardown's "two of them on the very affordance D12Canvas already renders" is one of four.

**The blocker resolved into a constraint rather than an opening.** ADR 0028 turned the dot into an invisible span and had already put two gestures on it: drag draws a connector, double-click adds a custom port. A click cannot be the third. ADR 0018 reads press count from `event.detail` on `pointerdown`, so a press-count-1 outcome fires before anything can know a second press is coming; drag-versus-double-click is separated by **movement**, which the model holds when it decides, but click-versus-double-click is separated only by a press that has not happened yet. Three meanings fit one target only when at most one is a click, which is a property of ADR 0018's dispatch rather than a limitation found later.

**So the double-click retires, one ticket after ADR 0028 defended it, and that ADR's own reasoning supplies the case.** It removed `cursor: copy`, observed that this left nothing suggesting a border could be double-clicked, and added the **Add port here** menu row for exactly that reason. What retires is a route already found undiscoverable; what takes the click is, on ticket 03's evidence, the most repeated act in a diagramming tool. The menu row becomes the sole pointer route to a custom port, which sharpens ticket 43 rather than leaving it merely open. ADR 0018's three double-click meanings drop to two.

**No ninth gesture, and the slot was observably empty.** `Quick create` is `DragEdgeEnd` releasing from its `pointing` phase, an outcome ADR 0018 defined and left unassigned, so ADR 0025's release-reliability `[Theory]` over the closed set of eight gains nothing. It is empty in shipped code too: `CompletePortDrag` resolves a zero-movement release back to the port it began on, and `!resolved.Equals(source)` is then false, so a press and release on a port does nothing today. Nothing to preserve, nothing to reverse.

**What it creates is a true duplicate** — same type, same `Props`, same size — routed through ADR 0013's duplication path rather than `NewCenteredInstance`. Reuse is the point: ADR 0013 regenerates ids across five references and named the missable one, `PortDef.Id`, since `CustomPortEndpoint` names the port's own GUID, so a hand-rolled copy would share port GUIDs between a node and its source and fail later as an edge on the wrong shape. Taking the source's size **amends ADR 0009 in one place**, justified by what the gesture is: it derives from an instance where the palette derives from a registration.

**The duplicate-label cost is stated rather than glossed.** Five chained nodes read the source's label until edited. Edit-on-create would fix it and is not available: `BeginEdit` is private to each built-in and entered only by a double-click on the component, so the canvas cannot tell a new instance to start editing, and supplying a way is a new member on ADR 0001's registration contract, which this map holds settled. Carried as [Edit-on-create for a quick-created instance](44-edit-on-create.md) rather than taken in passing. One cheap-looking route does not exist: ADR 0021's `PropertyRole` can identify a property generically, but its seven author roles have none for inline text content, and adding one would contradict the argument that closes that vocabulary, since bar membership is an **output** of it.

**Placement takes ADR 0026's nudge argument verbatim and introduces no constant.** Direction from the pressed side, then the source's border plus `2 × DominantGridSpacing()`, centred on the perpendicular axis, then the `SnapBounds` every placement already calls; ADR 0024 having made snap-to-grid the default puts the landing point on-grid by construction, so a chain stays aligned with no guide work. Two cells rather than one is **judged, not derived**, and recorded as such because ADR 0025 ships no manual acceptance pass. An occupied slot steps along the **same axis** until `Bounds.Intersects` clears, ADR 0009's diagonal cascade solving a different problem: a pile built by repeated placement at one anchor, which a chain never builds because each node becomes the next source. Miro's conditional (create only when nothing is near) was rejected as one gesture with two outcomes chosen by board content the user is not looking at.

**The edge pins at the source and stays auto at the target.** Source is the pressed port, holding ADR 0028's "the source is always a named standard port" and keeping ADR 0027's finding that auto at the source needs a second gesture true through a third gesture. Target is `AutoPortEndpoint`, which follows ADR 0027's ordering and resolves correctly for free, aiming from the new centre back at the source port so it crosses the facing side and stays right after either shape moves. A pinned target would loop back from the wrong side after the first rearrangement, which is the defect ADR 0027 exists to fix.

**Selection, focus and history are all inherited.** The new instance takes selection and focus and the source gives both up, which is what makes chaining work; ADR 0010's focus-follows-selection makes that one rule, and ticket 67 of `d12canvas-next` already built the `focusTabStopAt` / `_pendingPlacementFocusId` handoff. One `CompositeCommand` of the duplication's own commands plus `AddEdgeCommand`, so bullet 5 confirms ADR 0007 rather than assuming it, and no command type is added.

**`Ctrl`+`Arrow` for the keyboard**, free in ADR 0026's table, Excalidraw's own binding, and completing a set: plain nudges, `Shift` coarse-nudges, `Alt` resizes, `Ctrl` creates. Reaching only the four standard ports is ADR 0027's existing pointer/keyboard split, not a gap. **macOS has reserved both readings** — `Cmd`+`Left`/`Right` are browser back and forward, `Ctrl`+`Left`/`Right`/`Up` are Mission Control at OS level, which outranks the browser, and Excalidraw ships it anyway. Ticket 41 is **widened** to measure `Ctrl`+`Arrow` and `Cmd`+`Arrow` alongside `Ctrl`+`Tab`: same measurement, same browsers, one more row. A platform split was declined because there is no clean macOS arrow chord to split to, so it would invent a second chord ahead of the measurement.

**Three cases need no rule.** Ports render only on a single selection (ADR 0028) and an unrendered port is not hit-testable (ADR 0027), so this is reachable **only on a selected instance** and never on a multi-selection member — which is Miro's and FigJam's documented behaviour arrived at by geometry rather than a clause. And a `Group` has no ports at all.

Recorded as **ADR 0030**, amending 0028, 0018, 0026 and 0009, confirming 0013, 0007, 0020, 0027 and 0010, with `Quick create` added to `CONTEXT.md`. Surfaced [Edit-on-create for a quick-created instance](44-edit-on-create.md); widened [Whether `Ctrl+Tab` survives the browser](41-ctrl-tab-browser-reservation.md).

**No prototype was built, and the reason is worth recording** for a ticket typed `prototype`. Its two feel-shaped questions were both answered before a build could reach them: the trigger collapsed to one option once the press-count constraint was established, and the affordance is invisible, so there is nothing a prototype could have shown. The one number left to judge, the two-cell gap, is a number a static screenshot answers no better than a description does.
