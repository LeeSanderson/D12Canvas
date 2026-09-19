# Where the non-instance layers sit in the z-order

Type: grilling
Status: open

## Question

Decide how the layers that carry no `ZIndex` relate to ADR 0008's `ZIndex` arithmetic over component instances, now that there are two of them and a live defect between them.

Graduated from the map's fog by [Edges in a multi-selection](28-edges-in-multi-selection.md). It was not specifiable while the answer plausibly turned on whether an edge is a peer of a component in the z-order or a layer beneath it. ADR 0037 answers that: the arrangement commands read instances only and skip an edge, so an edge is **not a peer**. What remains is sharp.

The defect, found by ticket 14 and still live: `PreviousZIndex()` returns `min - 1` while `.edges-layer` is pinned at `z-index: 0`, so **send-to-back drops a component beneath every edge**, after which edges paint across its fill. That is the one case ADR 0016's contrast reasoning explicitly excludes. The same arithmetic ties the *first* placed component with the edges layer at 0 and leaves DOM order to break it.

Decide:

- **Whether the non-instance layers are inside the `ZIndex` space or outside it.** ADR 0008 settled layering as arithmetic over component instances and never contemplated a non-instance layer competing in the same stacking context. ADR 0008 is reopenable. Decide whether the layers are given reserved values, moved into their own stacking contexts, or whether the arithmetic gains a floor.
- **Cover both layers, not just edges.** ADR 0024 added the `Alignment guide` layer above content and below selection chrome, which ends the reading that `.edges-layer` is a one-off. Two layers competing with arithmetic that never contemplated them is a pattern, and whatever rule settles this should cover both rather than special-casing edges.
- **What the deliberate hit-order-versus-paint-order disagreement costs.** ADR 0017 fixed hit order independently, instances always beat edges, and widened an edge's hit region to a 20 screen pixel band. So a component sent to the back is *painted under* an edge while still *winning the press* wherever that edge crosses it. Decide whether that disagreement is correct and stays, or is the thing the paint fix removes.
- **Whether a selected edge gets an arrangement surface at all.** ADR 0023 made the gap user-visible: an edge has no `ZIndex`, so the object menu shows a selected edge no arrangement section. A user who restacks a shape and then looks for the same control on the edge crossing it finds nothing to click. ADR 0037 confirms the commands skip edges, so this is now a question about whether the *absence* is explained rather than about whether the commands apply.

Reopens ADR 0008 in one place at most, and touches ADR 0016 (contrast against what an edge crosses), ADR 0017 (hit order) and ADR 0024 (the guide layer).
