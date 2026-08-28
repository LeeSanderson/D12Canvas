# A click on a port span duplicates the instance beside it and connects the two

Ticket 03 named this the largest single widening beyond the ten seed defects: every reference tool can create the next object and connect it in one act, D12Canvas can do neither half in fewer than two gestures, and the teardown's judgement was that this is what makes a tool feel like a diagramming tool rather than a shape editor. This decides what carries it, what it makes, where that lands, and what the keyboard does.

The term is **`Quick create`**, borrowed from FigJam, which is the only one of the four tools that names the gesture at all.

## The slot was already empty

A `Quick create` is `DragEdgeEnd` releasing from its `pointing` phase on the `port` or `port-strip` role.

**No ninth pointer gesture.** ADR 0018 already commits a press to an owner at press and already derives a click as "did the gesture release from the `pointing` phase", precisely so that click stops being a parallel dispatch layer. `DragEdgeEnd` already claims those roles. So this decision assigns an outcome that the arbitration model defined and left unassigned, and ADR 0025's release-reliability `[Theory]` over the closed set of eight gains no member.

The slot is not merely unassigned in the model, it is an observable no-op in shipped code. `CompletePortDrag` resolves a zero-movement release back to the port the drag began on, and `!resolved.Equals(source)` is then false, so a press and release on a port today does nothing at all. There is no behaviour to preserve and none to reverse.

## The double-click retires, one ticket after it was defended

ADR 0028 put two gestures on the port span: drag to draw a connector, double-click to add a custom port. It argued those two cannot be confused, and that is right. Its threshold means a stationary press never promotes, so a drag and a double-click are separated by **movement**, which is information the model holds at the moment it decides.

A click cannot join them on those terms. ADR 0018 takes the press count from `event.detail` on `pointerdown`, so a press-count-1 outcome fires before anything can know whether a second press is coming. Click and double-click are separated only by **a press that has not happened yet**. The three cannot share one target unless the click waits out the double-click interval, which puts half a second of latency on the gesture whose entire justification is speed, and puts a timer into a model ADR 0029 has just finished refusing a clock.

**The double-click goes and `Quick create` takes the click.** ADR 0028's own reasoning supplies the case, which is why this is a narrowing rather than a reversal of its argument. That ADR removed `cursor: copy`, observed that this left nothing on screen suggesting a border could be double-clicked, and added the **Add port here** menu row for exactly that reason. What retires here is a route that already had no way to be discovered. What takes its place is, on the teardown's evidence, the most repeated act in a diagramming tool.

The menu row becomes the only pointer route to a custom port. ADR 0018's three double-click meanings drop to two, `SwitchToEditMode` and `AddEdgeLabel`.

Frequency decides the trade and is worth stating plainly. Adding a custom port is rare and deliberate, and it keeps a route. Creating the next node in a flow is what the user does over and over, and it gets the cheap one.

## What it creates is a duplicate

**Same component type, same `Props`, same size as the source.**

This routes through ADR 0013's duplication path rather than through `NewCenteredInstance`, and reusing it is the point rather than an economy. ADR 0013 regenerates ids across five references, and named the missable one: `PortDef.Id`, because `CustomPortEndpoint` names the port's own GUID. A hand-rolled copy in this gesture would share port GUIDs between a node and its source, and the failure would surface later as an edge attaching to the wrong shape.

Taking the source's own size rather than the registration's `DefaultSize` is a deliberate divergence from ADR 0009, which says both placement gestures use the registered size. It is justified by what this gesture is: `Quick create` derives from an **instance**, where the palette derives from a **registration**. A user who widens a rectangle to fit a long label and then chains five more wants five of what they made, not five of what was registered.

**The cost is real and is not glossed.** A chained flow of five nodes reads the source's label five times until each is edited. The obvious fix is to open the new instance for editing on creation, and that is not available: `BeginEdit` is private to each built-in and entered only by a double-click on the component itself, so the canvas has no way to tell a new instance to start editing. Supplying one is a new member on the registration contract, which is ADR 0001, which this map holds settled. Carried as [Edit-on-create for a quick-created instance](../../.scratch/canvas-interaction-quality/issues/44-edit-on-create.md) rather than taken in passing, because a prototype ticket quietly containing a registration-contract decision is the failure this map has already been bitten by once.

One route was considered and does not exist. ADR 0021 replaced `SharedTag` with a closed `PropertyRole` enum, which can identify a property generically, so "copy everything except the label" looked cheap. Its seven author-facing roles are `Fill`, `Stroke`, `StrokeWidth`, `TextColour`, `FontSize`, `FontWeight` and `TextAlign`, and there is no role for inline-edited text content. There should not be: ADR 0021's argument is that bar membership is an **output** of the vocabulary, so a member that must never appear in the bar would contradict the reason the vocabulary is closed.

## Where it lands

Direction comes from the side the pressed span belongs to. The new instance sits at the source's border on that side, plus a gap, centred on the source's perpendicular axis, then snapped by the same `SnapBounds` call every other placement already makes.

**The gap is `2 × DominantGridSpacing()`**, reusing the method ADR 0026 already extracted for the nudge step, and inheriting its argument verbatim: a step that follows the grid stays screen-relative within a bounded range instead of becoming a fixed board amount. No new constant is introduced. ADR 0024 having made snap-to-grid the default means the landing point is on-grid by construction, so a chain stays aligned with no guide work and no alignment command.

**Two cells rather than one**, judged rather than derived. One cell is 20 board units at the default zoom, which is tighter than any reference tool leaves between connected nodes. This is one of the numbers ADR 0025 leaves defended by nothing, since that decision ships no manual acceptance pass, and it is recorded here as judged at the moment it was chosen rather than presented as falling out of something.

**An occupied slot steps along the same axis.** Probe at the gap, then twice the gap, then three times, until the candidate rect intersects nothing, using the `Bounds.Intersects` that ticket 24 of `d12canvas-next` added alongside `ExpandedBy`. Stepping on one axis is what keeps a chain a chain; ADR 0009's diagonal cascade solves a different problem, a pile built by repeated clicks on one palette entry, and chaining never builds a pile because each new node becomes the next source.

The cost of stepping is that a dense board can push the new instance off screen. It is recoverable rather than lost, because the instance takes selection and ADR 0015 binds `Shift+2` to frame the selection.

Miro's conditional was rejected. Its documentation says "if there are no objects near the selected one, you will be suggested to create the same object linked to the chosen one", which reads as one gesture with two outcomes chosen by board content the user is not looking at. That is the ambiguity ADR 0018 spent its whole argument removing.

## The edge pins at the source and stays auto at the target

**Source: the `PortEndpoint` or `CustomPortEndpoint` whose span was pressed. Target: `AutoPortEndpoint(newId)`.**

Two settled findings hold the source end. ADR 0028 states that a connector's source is always a named standard port, and ADR 0027's conclusion that auto at the source needs a second gesture survives only if nothing produces one in a single act. A drag from this exact span already pins its source, so a click on the same target producing a different endpoint kind would be a divergence with nothing behind it.

The target end follows ADR 0027's ordering, that the easier gesture gives the more forgiving result. The user aimed at nothing, because the instance did not exist when the press happened. Auto also resolves correctly with no extra work: it aims from the new instance's centre back at the source port's point, which crosses the side facing the source, and it stays right when either shape later moves. A pinned target would leave the edge looping back from the wrong side after the first rearrangement, which is the defect ADR 0027 exists to fix.

ADR 0027's same-component guard is not engaged. The two endpoints name different components by construction.

## Selection, focus, and one history entry

**The new instance takes selection and focus, and the source gives both up.** This is what makes the gesture chainable, which is the entire reason it is worth building: five keypresses instead of five place-then-connect cycles. ADR 0010 settled focus-follows-selection, so this is one behaviour rather than two, and the mechanism exists. Ticket 67 of `d12canvas-next` built `focusTabStopAt(container, index)` driven by a `_pendingPlacementFocusId` set before the render that creates the tab stop, which is exactly this shape.

**One `CompositeCommand`**, wrapping the duplication's own commands plus `AddEdgeCommand`. ADR 0007 is confirmed rather than amended, and no command type is added: ADR 0013 already established that duplicate is a composite of existing primitives, and `AddEdgeCommand` already exists and is already what both `CompletePortDrag` and the palette's connector entry use.

ADR 0020's invariant that no pointer gesture creates a command before release holds without a clause. `Quick create` is defined by its release.

## `Ctrl`+`Arrow`, and a chord macOS has reserved twice

**`Ctrl`+`Arrow` creates and connects in the arrow's direction**, reaching the four standard ports only. This is Excalidraw's own binding for the same gesture, and it is free in ADR 0026's table. It also completes a set rather than squeezing into one: a plain arrow nudges, `Shift` coarse-nudges, `Alt` resizes, and `Ctrl` creates. Four modifiers over the arrows, four meanings, no collision.

The keyboard reaching only the four standard ports is not a parity gap. It is the same split ADR 0027 already drew, where the pointer distinguishes by where you release and the keyboard by how far you drill. There is no fraction to name from a keypress, and a custom port at 0.8 along a side is reachable by the pointer route.

**On macOS the chord is contested in both of its readings, and this ships as a documented doubt.** Bindings here accept `(ctrlKey || metaKey)`. `Cmd`+`Left` and `Cmd`+`Right` are back and forward in Chrome and Safari; `Ctrl`+`Left`, `Ctrl`+`Right` and `Ctrl`+`Up` are Mission Control at the operating-system level, which outranks the browser. Excalidraw ships `Ctrl/Cmd`+`Arrow` regardless, which means its own binding is at best half-working on macOS.

This is ticket 41's failure family exactly, arriving on a second chord. ADR 0026 already carries `Ctrl`+`Tab` as a doubt for the same reason, so rather than opening a parallel investigation, [Whether `Ctrl+Tab` survives the browser](../../.scratch/canvas-interaction-quality/issues/41-ctrl-tab-browser-reservation.md) is **widened to measure `Ctrl`+`Arrow` and `Cmd`+`Arrow` alongside it**. It is the same measurement, in the same browsers, on the same day, and it is one more row in a table someone is already building.

A platform split was considered and declined for now. ADR 0024 established that a platform check exists in this codebase, for `Ctrl`+click being the macOS secondary click, so a conditional binding is precedented. But there is no clean macOS arrow chord to split **to**, so splitting now means inventing a second chord before the measurement that would say whether one is needed.

## Three cases need no rule

**Only a selected instance offers it.** ADR 0028 renders ports on a single selection and on the one component under the pointer during a live connector drag, and ADR 0027's consequence is that an unrendered port is not hit-testable. So a press on an unselected instance's border classifies as `instance` and becomes `MoveSelection`, and `Quick create` is unreachable there. That is Miro's and FigJam's documented behaviour, both of which describe the dot as appearing on a **selected** object, arrived at here by geometry rather than by a clause.

**A multi-selection offers nothing.** ADR 0028 already shows no ports on a member of a multi-selection, matching `ShowSelectionOverlay`. So "which member is the source" is never asked.

**A `Group` offers nothing.** Edges never attach to a `Group` (ADR 0005, narrowed by ticket 15 of `d12canvas-next`), so a group has no ports and therefore no port span.

## How this is verified

Inherited from ADR 0025, stated as relationships and counts.

- The gap is asserted as a **multiple of `DominantGridSpacing()`**, never as a board value. Like ADR 0026's nudge step and for the same reason, it cannot join the screen-pixel ordering, because the dominant cell spans 6.3 to 63 screen pixels and straddles every member of it.
- **One history entry per gesture**, asserted as a count on `CommandHistory` rather than by inspecting the composite's contents.
- The new instance's id differs from the source's, and **every `PortDef.Id` on it differs**, which is the ADR 0013 reference a hand-rolled copy would miss.
- The created edge's source is a `PortEndpoint` or `CustomPortEndpoint` and its target is an `AutoPortEndpoint`. A table over the four side directions, not four hand-written gestures, for the reason ADR 0025 made the press-to-kind mapping a table.
- The occupancy loop terminates and its result intersects nothing, as a property over a board seeded with obstacles.
- ADR 0025's release-reliability `[Theory]` is unchanged, because the closed set of eight is unchanged. `Quick create` is covered by `DragEdgeEnd`'s existing case.

## Amends, confirms

- **Amends ADR 0028** in one place, and against its own reasoning rather than despite it: the port span's double-click for adding a custom port is **retired**, leaving the Add port here menu row that ADR added as the sole pointer route. Its partition geometry, visibility rule, cursor table and the number `N` are untouched.
- **Amends ADR 0018** in one place: the three double-click meanings become two. The closed set of eight, the role-to-owner table, the synchronous decisions and the capture rules are all untouched, and the `pointing`-phase release outcome this defines is one the ADR provided for.
- **Amends ADR 0026** by one row, `Ctrl`+`Arrow`, carried as a documented doubt on macOS in the same way that table already carries `Ctrl`+`Tab`.
- **Amends ADR 0009** in one place: `Quick create` sizes a new instance from the source instance rather than from the registration's `DefaultSize`. Both palette placement gestures are untouched.
- **Confirms ADR 0013.** Duplication gains a second consumer and needs no change to do it, including its id regeneration across five references.
- **Confirms ADR 0007** and **ADR 0020**. One gesture, one entry, created at release, out of a composite of existing primitives.
- **Confirms ADR 0027.** Auto at the source stays unreachable in one gesture, the target takes the forgiving result, and the pairwise resolution needs nothing added.
- **Confirms ADR 0010**, whose focus-follows-selection makes the chaining behaviour one rule rather than two.

**Considered and rejected:**
- **Deferring the click by the double-click interval** to keep all three gestures on the span. Puts roughly half a second of latency on the gesture whose whole value is speed, and introduces a timer into a model that ADR 0029 has just declined to give a clock.
- **Releasing a connector drag on empty canvas**, which is FigJam's drag variant and dodges the click collision entirely. Rejected because ADR 0027's zone 3 gives a drop over nothing a `FloatingEndpoint`: the user drags out expecting a dangling line and gets a whole new shape. The palette's connector entry still makes floating edges, so the state survives, but its primary gesture would die silently.
- **A keyboard-only gesture with no pointer affordance**, which is Excalidraw's model. Nothing collides and nothing is spent, but it makes the gesture invisible to a mouse user, against a destination that is about direct manipulation.
- **Creating from the registration's `DefaultProps` and `DefaultSize`.** Avoids the duplicate-label cost and needs no new code path at all, since `NewCenteredInstance` already does exactly this. Rejected in favour of a true duplicate, which is Miro's documented wording ("the same object") and what a user resizing a shape to fit their content expects the next one to match.
- **Copying everything except the inline text**, keyed off ADR 0021's `PropertyRole`. There is no role for text content and adding one would contradict the argument that closes that vocabulary.
- **Reopening ADR 0001 here** for a begin-editing signal, so a chained node opens ready to type. The feature is worth having and is inside this map's destination, but it widens a prototype ticket into a registration-contract decision, so it is carried as its own ticket.
- **A type picker on release.** Turns one gesture into two and kills the chaining that justifies the feature. No reference tool does it.
- **Miro's conditional outcome**, connecting to a nearby object instead of creating one when the space is occupied. One gesture with two outcomes selected by board content the user is not looking at.
- **A diagonal cascade on an occupied slot**, reusing ADR 0009's rule. Breaks the axis the chain is establishing, so the fourth node in a row lands somewhere that reads as a mistake.
- **Both endpoints auto.** Reads the press as pure direction and would give a direction-aware router better input at both ends, which ADR 0027 notes is genuinely true. It creates an auto source in one gesture, reversing a finding that ADR 0027 made and ADR 0028 confirmed.
- **Both endpoints pinned.** Fully determined and easy to reason about, at the cost of the self-correction: move the new node above its source and the edge still leaves the right side and loops back.
- **A platform-split keyboard chord**, deciding macOS's binding now. Precedented by ADR 0024's platform check, but there is no clean macOS arrow chord to split to, so it would invent a second chord ahead of the measurement.
