# Keyboard reach for an edge

Type: grilling
Status: resolved

## Question

Decide how a keyboard user reaches an individual edge, given that an edge has no tab stop and ADR 0026 never noticed.

Split out of [Edges in a multi-selection](28-edges-in-multi-selection.md), which made the gap precise rather than creating it. ADR 0017 holds that pointer participation and keyboard reachability are separate properties, so deciding membership did not decide this.

The fact, established rather than suspected: `OrderedTabStops` enumerates visible component instances and visible groups, orders them by `Y` then `X`, and that is the whole ring. ADR 0026 supersedes ADR 0009's table, settles the guard regimes, the nudge step, chrome navigation and the chord set, and says nothing about an edge anywhere. This is **silence, not a decision**, so nothing has to be reversed.

What it costs today, stated as of ADR 0037 rather than in general:

- **A mixed selection is constructible only by pointer or by select-all.** `Ctrl`+`Tab` is the keyboard's additive route and it walks a ring edges are not in.
- **ADR 0021's four edge roles have no keyboard route to the edge they act on.** `EdgeRouting`, `EdgeSourceArrow`, `EdgeTargetArrow` and `EdgeColour` reach the property bar through `Ctrl`+`Enter`, which acts on the selection, which a keyboard user cannot point at a single edge.
- **`Ctrl`+`A` then Delete is the only keyboard route to deleting an edge**, and it deletes the board.
- ADR 0010's stated goal is that the board be operable without a mouse.

Decide:

- **Whether an edge enters the tab ring at all**, or takes a chord, or a separate ring. ADR 0026 has a rule for this shape already, written for chrome rather than content: a surface enters the tab order only if it is the only route, canvas-rendered takes a chord, host-placed takes its tab order. Decide whether an edge is governed by that rule or is content and governed by ADR 0010's reading order.
- **The order key, if it enters the ring.** An edge has no `Bounds`. Its resolved endpoints give a derivable position (source point, midpoint, the union box), and reading order over a `Y`-then-`X` sort needs one of them chosen. Note that an edge spanning the board sits nowhere meaningful in reading order whichever is picked, which is an argument about the ring rather than about the key.
- **Ring length.** ADR 0030's `Quick create` is designed to produce connector-heavy boards, so putting every edge in the ring roughly doubles it on exactly the boards where it hurts most. Weigh that against a chord, which costs a binding from a table ADR 0026 already found crowded and which [Whether `Ctrl+Tab` and `Ctrl+Arrow` survive the browser](41-ctrl-tab-browser-reservation.md) is already measuring pressure on.
- **Whether LOD excludes an edge the way it excludes an instance.** `FocusableTabStopIds` drops an LOD-placeholdered instance because it renders as a non-interactive div with no `tabindex`. ADR 0028 found the affordance floor always bites before `LodSizeThreshold`, and an edge has neither, so the exclusion has no obvious counterpart.
- **What `FocusEntity` does now that there are two sets.** It takes a `Guid` and hard-selects. With an edge focusable it has to pick a set, which is one lookup, but it also has to honour ADR 0036's rule that focus moves on three occasions and ADR 0031's `Selection snapshot`, which captures at press whatever `@onfocus` wrote.
- **Whether the DOM element already exists.** ADR 0017 made every hit region a real element, so an edge may need only a `tabindex` and an order key rather than new markup. Confirm before designing around either answer.

Amends or extends ADR 0026 (its table and its tab-stop enumeration), and touches ADR 0010, ADR 0017 and ADR 0036.

## Widened by ADR 0051

`Escape` out of an edge label's inline edit calls `EndInlineEdit`, which returns focus to the canvas container because a label has no tab stop to return to. If this ticket gives edges a tab stop, decide whether that focus return moves to the edge.

## Answer

Recorded as [ADR 0054](../../../docs/adr/0054-an-edge-is-a-tab-stop-after-its-source.md).

- **Route.** An edge is board content, so ADR 0010's reading order governs it and ADR 0026's chrome rule does not. Every edge joins the tab ring. No chord, no drill from an endpoint. A drill cannot reach a palette connector with both ends floating, and a chord would need a binding that has not been measured.
- **Order key.** An edge's stop comes directly after the stop its source resolves to: the instance, or the outermost group's stop for a grouped member. Several edges after one anchor sort by target point, `Y` then `X`. An edge with a floating source sorts as its own stop at its source point. This answers "an edge spanning the board sits nowhere meaningful": the position comes from the source, not from the edge's own geometry.
- **Which edges have a stop.** Those whose anchor is present: the source's stop is mounted, or a floating source point is inside the viewport plus overscan. LOD never removes an edge's stop. An edge whose line cannot be resolved has none.
- **Entered group.** While a group is entered, no edge has a stop. An edge is never a member, so ADR 0044 would pop the scope on the first `Tab` that reached it. Every edge stays reachable from the top level.
- **`FocusEntity`** resolves the kind with `Board.GetEdge` and hard-selects the edge alone. `Space` toggles a focused edge in `_selectedEdgeIds`. `Enter` does nothing on an edge stop. `TabStop` gains an `Edge` field.
- **The element already exists for the pointer but cannot be the stop.** The `<line>`/`<path>` sits in `<svg class="edges-layer">` ahead of every tab-stop div in the DOM. Each edge gets an invisible `.edge-tab-stop` proxy in the `OrderedTabStops` loop, as `.group-tab-stop` does for a group: `pointer-events: none`, `tabindex="0"`, sized to the endpoints' union box, `aria-label` "Connector from {source} to {target}" with "unattached end" for a floating end, `aria-selected`, no `role`.
- **Widened by ADR 0051.** `Escape` out of an edge label's edit returns focus to the edge's stop, with the canvas container as the fallback when the stop is not mounted.
- **Assumes nothing about [Keyboard multi-select without Ctrl+Tab](55-keyboard-multi-select-without-ctrl-tab.md).** Edge stops are ordinary stops and get whatever that ticket decides.

Found on the way: the ticket says `FocusableTabStopIds` drops a placeholdered instance because the placeholder has no `tabindex`. That is the code. ADR 0017 and `CONTEXT.md` make the placeholder an ordinary tab stop, so the code has not caught up with a settled decision. It does not change this answer, since the edge keeps its stop either way.
