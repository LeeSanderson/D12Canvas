# Clipboard and duplication for a lone edge

Type: grilling
Status: resolved
Blocked by: 28

## Question

Decide whether a selected edge can be cut, copied or duplicated at all, and what those operations mean when an edge's geometry is derived from what it attaches to.

Surfaced while drawing the object context menu's membership (ticket 10). ADR 0013 makes copy on a lone selected edge a no-op, and requires cut to be one too on the grounds that cut must never destroy content it did not first capture. Duplicate follows them out by the same reasoning. The cause is structural rather than an oversight: the payload carries only **interior** edges, meaning both endpoints resolve to instances inside the copied set, and `_selectedEdgeId` is an exclusive slot, so a lone edge selection has no interior anything.

The consequence ticket 10 has to ship is that Cut, Copy and Duplicate are **hidden** on an edge selection rather than shown and inert, which leaves the object menu four rows deep for an edge against roughly a dozen for an instance. That is visible to users on contact, and it is the sharpest statement yet of how second-class an `Edge` currently is.

Ticket 28 partly dissolves this before it starts, which is why this waits on it: widening `_selectedEdgeId` into the general selection means an edge selected alongside both of its endpoint instances becomes an interior edge and travels for free, with no new rule. What survives that widening is the lone case, where the user has selected an edge and nothing else.

Decide:

- **Whether a lone edge can be copied at all.** ADR 0013 rejected carrying half-attached edges, either floating or with the outside `ComponentId` preserved, and gave reasons that apply twice over to a fully-attached edge copied alone: a floating stub points at nothing, and a preserved `ComponentId` silently rewires the pasted edge to the *original* instances within one board and dangles across boards. If those reasons still hold, the honest answer is that a lone edge is uncopyable by design and the menu rows stay hidden permanently.
- **Whether cut stays a no-op or becomes a delete.** ADR 0013's rule is that cut must not destroy what copy did not capture. If copy stays a no-op then so does cut, and a user who reaches for Ctrl+X on an edge gets nothing, with Delete sitting right beside it doing what they meant.
- **What duplicate means for an edge.** ADR 0013's `+20, +20` rigid-body translate has no referent when both endpoints are attached, because the edge's geometry is derived from its endpoints rather than stored. A duplicate would be a second edge between the same two ports, occupying identical pixels. For an edge with floating endpoints the offset does mean something, so the answer may differ by endpoint kind, which is a shape no other entity has.
- **Whether paste can produce a lone edge.** ADR 0009's palette already creates an `Edge` with both endpoints floating, so a board of edges and no components is legitimate, and ADR 0015's content extent was widened specifically to count them. That is an existing route to the same object, which weakens any argument that a lone edge is not real content.

Not in scope here: whether an edge participates in the z-order at all. That is already in the map's **Not yet specified**, waiting on the same ticket 28, and it is a paint-and-hit-order question rather than a clipboard one.

Does not block ticket 10. The menu hides these rows behind an eligibility predicate, which stays correct under today's model and starts returning true on its own if this ticket adds the operations later.

Amends ADR 0013 if a lone edge becomes copyable.

**From resolving [Alt-drag to duplicate](34-alt-drag-duplicate.md):** this ticket's answer now has a second consumer. ADR 0042 makes a clone drag copy exactly what duplicate copies, so whatever this ticket decides for a selected edge with floating ends applies to Alt-drag as well, with no separate rule.

## Answer

Recorded as [ADR 0045](../../../docs/adr/0045-selected-edges-on-the-clipboard.md).

**The premise had moved.** ADR 0037 replaced the exclusive `_selectedEdgeId` slot with a second set, so the lone edge is one case of a wider question: what a copy carries for a selected edge that is not interior.

**Explicit selection overrides the interior rule for edges.** A copy carries the selected instances closed over interior edges, plus every selected edge. An end on a copied instance stays attached and is remapped, a floating end keeps its coordinate, and an end on an instance not copied becomes floating at its current resolved position. ADR 0013's two reasons for rejecting half-attached edges are about edges swept in by closure, so they still hold there and say nothing about an edge the user picked.

**Duplicate and clone drag build the same payload** and never keep an outside attachment, even though on the same board it would resolve. A second edge on the same two ports draws over the first and looks like nothing happened. A lone attached edge duplicates as a floating line beside the original.

**Cut is copy plus delete with no edge case.** The "cut on a lone selected edge is a no-op" sentence is removed. Cut, Copy and Duplicate become eligible on edge selections, so an edge-only menu is seven rows.

**Paste drops an edge only when an attached end names an instance that did not materialise.** Re-applying the interior rule would have dropped every floating line the copy carried. An end whose instance did not materialise drops its edge rather than floating, since its position would need a port on a component paste could not bind.

**Placement reads ADR 0015's extent**, instances unioned with resolvable edge endpoints, for the paste delta and the `Duplicate run` offset. ADR 0037's "an edge contributes nothing to the selection bounds" is scoped to arrangement. Without this, an edge-only payload had no box to move by.

**Found on the way:** ADR 0037 said the marquee reused `Interior edge` "verbatim" while adding a floating-inside-the-band clause, and `CONTEXT.md` claimed marquee and copy select "the same set by construction". They did not, so a marqueed palette connector was dropped by `Ctrl+C`. Closed by copy carrying the selection. `CONTEXT.md`'s entry now says what each of its three readers does.

Amends ADR 0013, ADR 0023, ADR 0037, ADR 0039 and ADR 0042. No new tickets.
