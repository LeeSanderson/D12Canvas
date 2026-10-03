# `ZIndex` orders instances only; every other layer stacks outside it

ADR 0008 settled layering as arithmetic over `ComponentInstance.ZIndex` and never considered anything else competing in the same stacking context. Plenty does. `.canvas-content` carries the canvas transform, so it is one stacking context, and its children stack like this today:

| Element | `z-index` |
|---|---|
| `.edges-layer` | `0` |
| each `ComponentContainer` and `LOD placeholder` | its `ZIndex`, unbounded, the first placement at `0` |
| `.edge-label` | auto: layer 0, after instances in DOM order |
| `.marquee-select` | auto, likewise |
| `.selection-bounding-box` | `1000` |
| `Alignment guide` layer (ADR 0024, not yet built) | "above content and below selection chrome" |

Every row except the instances has a value, or a lack of one, that the arithmetic never accounts for. That produces five live defects:

- **Send-to-back drops a shape beneath every edge.** `PreviousZIndex()` returns `min - 1`, which goes negative while `.edges-layer` stays at `0`, and edges then paint across the shape's fill. ADR 0016 recorded this as the one case its contrast reasoning excludes.
- **The first instance ties with the edges layer at `0`**, and DOM order breaks the tie.
- **The marquee paints under every instance after the first**, since each later placement gets `ZIndex >= 1`.
- **An edge label paints under any instance with a positive `ZIndex`.**
- **The multi-selection box goes under content once `ZIndex` passes `1000`.** `NextZIndex()` is max + 1 with no ceiling, so enough placements or bring-to-fronts bury the box and its resize handles.

ADR 0024's guide layer would have joined them on the day it landed, because "above content" has no fixed number to sit above.

The intended order was never in doubt. ADR 0016 says edges paint beneath components and the components hide them, and ADR 0037 says an edge is not a peer in z-order. This decision is about making the browser follow that order.

## Four layers, each its own stacking context

**`.canvas-content` holds four layer elements in a fixed order. Each layer is its own stacking context, and `ZIndex` orders only the inside of one of them.** From bottom to top:

1. **Edge band.** Edge lines, the selected-edge halo from ADR 0041 under each line's stroke, then edge labels above the lines.
2. **Instance layer.** Every `ComponentContainer` and `LOD placeholder` ordered by `ZIndex`, then `ChildContent`. Every tab stop lives here too, including the `.group-tab-stop` and `.edge-tab-stop` proxies.
3. **Guide layer.** The `Alignment guide`s.
4. **Selection chrome.** The marquee, the multi-selection bounding box and its handles, floating endpoints, and the connector drag preview.

The instance layer is isolated (`isolation: isolate`), so a `ZIndex` of `-50` or `5000` stays inside it. **ADR 0008's arithmetic is unchanged.** No floor, no reserved values, no renumbering. It just has a scope it never had to state before.

## Where the in-between items go

**Edge labels paint with their edge, beneath instances.** A label usually sits at its edge's midpoint, in the open space between the two shapes it connects, so in the common case it looks the same in either layer. The layers differ when an edge runs under a shape. With labels above instances, the label would sit on the shape's fill while its own line was hidden: text with no visible owner, on a background ADR 0016 never promised contrast against. Keeping labels in the edge band also matches ADR 0017's hit precedence (instances, then edge labels, then edges), so paint and hit order agree without any extra rule.

**Floating endpoints and the connector drag preview move out of `.edges-layer` into selection chrome.** In the edge band they would sit beneath every instance. A floating endpoint dropped over a shape would be invisible and impossible to grab, which breaks ADR 0017's "affordances beat content". The drag preview would disappear just as it reached the shape it was being dragged to, which is when the user most needs to see it. Both are transient, both exist only during interaction, and neither is board data, which is what makes something chrome. Both are drawn in board space, so selection chrome gets an svg of its own under the same transform.

**`ChildContent` stays in the instance layer, after the board's instances.** That is the DOM position and stacking context it has today. A host's hand-placed `ComponentContainer` still competes with board instances by `ZIndex`, and a host's plain element still stacks as it did. This decision is about the library's own layers. Where host content belongs relative to guides or chrome is a question nobody has asked yet.

**Tab stops stay interleaved in the instance layer, even for edges.** ADR 0054 puts an edge's stop directly after its source's stop in DOM order, because `Tab` follows DOM order. The proxy paints nothing and takes no pointer events, so where its edge paints doesn't matter. This is the same reason ADR 0054 gave for not putting the stop on the `<line>`.

## Hit order and paint order now agree

ADR 0017 fixed "instances always beat edges, regardless of `ZIndex` arithmetic" because send-to-back could paint an instance beneath an edge whose 20 screen-pixel band would then take the press across it. With edges always beneath every instance:

- Where an edge passes under an instance, the instance is on top in paint order and wins in hit order.
- Where an edge crosses empty canvas, nothing else is there and the edge wins.
- An edge's band can only win where no instance covers it, which is exactly where the edge is visible.

No board state leaves the two orders disagreeing. **ADR 0017's rule stays and its reason changes**: it now matches paint order by construction instead of working around a paint bug. ADR 0046's hit stack, which reads paint order, now has the same top entry as a plain click, so the case it "accepted rather than patched" can no longer occur.

One consequence worth stating so nobody fights it later: an edge's hit element lives inside the edge band's stacking context, so **no `z-index` on it can lift an edge above an instance.** That's deliberate.

## A selected edge gets no arrangement section

ADR 0023 showed a selected edge with no arrangement rows, and called that an honest picture of how second-class an edge is. Under this decision an edge can never cover a shape, so there is nothing to restack an edge against. **The absence is correct, and the object menu stays at its four rows.** ADR 0023's rule that ineligible rows are absent rather than inert means there is no greyed-out Bring to Front.

The one thing an edge command could still do is order edges among edges where two lines cross. Today `Board.Edges` insertion order decides that. Giving `Edge` its own `ZIndex` for it would add a persisted field and four commands for two crossing 2px lines, where the selection halo already shows which one is selected. Worse, a "Bring to Front" row on an edge that then stays under the shape it crosses would mislead more than no row does. This isn't left as fog. It comes back only if ADR 0037's "not a peer" is reopened.

## How this is verified

Per ADR 0025:

- After send-to-back on an instance an edge crosses, the instance's container paints above the edge's line. Asserted as a Playwright screenshot of that crossing.
- The first placed instance paints above an edge running beneath it.
- A marquee drawn across two placed instances paints above both.
- An edge label paints beneath an instance with a positive `ZIndex` that overlaps it.
- With an instance's `ZIndex` set above `1000`, the multi-selection box and its handles paint above it.
- A floating endpoint lying over an instance is visible and takes the press.
- `OrderedTabStops` and the rendered `[tabindex="0"]` elements still agree in count and order on a board mixing instances, groups and edges, with the edge labels now earlier in the DOM than every stop.

Every `.verified.html` baseline moves, since every board gains the layer wrappers. A `.png` baseline moves only where the old order was one of the defects above.

## Amends, discharges, confirms

- **Amends ADR 0008** in one place: layering arithmetic is scoped to the instance layer, and an edge has no arrangement commands because edges sit beneath every instance by rule.
- **Discharges ADR 0016's send-to-back trap.** Its claim that edges paint beneath components is now true in every case, not only in the cases where the arithmetic happened to agree.
- **Amends ADR 0017's reason, not its rule.** "Instances always beat edges" now follows paint order.
- **Amends ADR 0024** by making "above content and below selection chrome" a concrete layer.
- **Amends ADR 0046** at its paint-order caveat: the hit stack's top entry and a plain click no longer differ for an instance sent behind an edge.
- **Confirms ADR 0037.** An edge is not a z-order peer, and reading order (ADR 0054) is untouched.

## Considered and rejected

- **A floor in the arithmetic**, reserving `0` for edges and keeping `PreviousZIndex()` at 1 or above. Send-to-back would have to renumber once the minimum reached the floor, which breaks ADR 0008's arithmetic-only rule, and it does nothing about the ceiling problem.
- **Reserved extreme values**, edges at `int.MinValue` and chrome near `int.MaxValue`. No DOM change, but every layer then depends on `ZIndex` never reaching a reserved band, and nothing enforces that.
- **Edge labels in a layer above instances.** Puts text on a shape's fill while the line it labels is hidden.
- **Floating endpoints and the drag preview left in the edge band.** Hides an affordance under content, and hides the preview at its target.
- **`ChildContent` in a host layer of its own.** A placement decision nobody asked for, and it would change how existing host pages paint.
- **Keeping the hit-versus-paint disagreement as a feature.** Nothing is left for it to protect.
- **A `ZIndex` on `Edge` for ordering among edges.** A persisted field and four commands for crossing lines, with a menu row that suggests edges can come above shapes.
