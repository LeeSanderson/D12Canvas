# Edges in a multi-selection

Type: grilling
Status: resolved

## Question

Decide whether an `Edge` can be a member of a multi-selection, and what widening `_selectedEdgeId` from an exclusive slot to a set costs.

Graduated from the map's fog now that both its blockers are resolved. Three tickets have hit this from three directions and each worked around it rather than widening the slot:

- **Ticket 09** found that select-all excludes edges, so select-all-then-delete leaves every edge behind, and declined to fix it because widening the slot amends ADR 0006's selection model.
- **Ticket 13** found that zoom-to-selection has to read the exclusive slot *as well as* the instance set, or framing a selected edge silently does nothing — a hole that would have shipped in an instances-only implementation.
- **Ticket 14** shipped edge colour as the fourth settable edge property behind a missing surface, and a property bar acting on several edges at once is meaningless while only one can be selected.

ADR 0018 is what makes this specifiable: `SelectEdge` is now a named pointer gesture with a `pointing` phase, so "shift-press an edge to add it to the selection" has somewhere to live, and `MoveSelection` already operates over the selection rather than over an instance.

Decide:

- **Whether `_selectedInstanceIds` and `_selectedEdgeId` become one heterogeneous selection or two parallel sets.** ADR 0006 is reopenable. A single set of entity ids is the obvious shape but forces every consumer to discriminate; two sets keep the existing reads working and make "is anything selected" a two-part question.
- **What a mixed selection permits.** Delete and (per ADR 0017) lock are meaningful across both kinds; align and distribute are not obviously meaningful on an edge at all, and ADR 0014 computes against selection bounds that an edge would widen. Decide whether a mixed selection narrows the action set, or whether edges are simply skipped by the commands that cannot express them — the latter being ADR 0017's precedent for locked entities.
- **Whether the marquee selects edges.** It currently cannot, and ADR 0017's participation predicate covers edges, so the machinery exists. An edge's hit region is a 20-screen-pixel band but its *bounds* for marquee purposes are its endpoints — decide whether a marquee that crosses an edge selects it, or only one that encloses both endpoints.
- **What `MoveSelection` does to a selected edge.** An edge with both endpoints attached has no independent position; one with floating endpoints does. A mixed selection where a component and an edge attached to it both move would otherwise double-apply the delta.
- **The neighbouring unresolved case the fog attached to this**: `OnDeletePressed` can leave a `Group` referencing deleted members. Decide whether that belongs here or is separable.

Amends ADR 0006, and touches ADR 0013 (select-all) and ADR 0014 (align) where the answer changes what a mixed selection admits.

## Answer

An `Edge` becomes a member of the selection, held in a **second set** beside the instance set, and a mixed selection **skips** what it cannot express. Recorded as **ADR 0037**.

**Two parallel sets, not one collection.** Instance, group and edge ids are all bare `Guid`s and `Board` tells them apart only by which getter returns non-null, an idiom `ExpandInto` already relies on. A flat `HashSet<Guid>` therefore feeds edge ids into group expansion and out into `SelectedComponents`, which returns `Array.Empty` on the first unresolvable id: the hazard ADR 0021 removed for groups, arriving again and emptying the property panel on every mixed selection. Typed refs fix the ambiguity and buy only selection order, which ADR 0014 established is unrecoverable and unused. Two sets is the only shape where an edge id is **structurally unable** to reach group expansion rather than guarded out of it. Cost: "is anything selected" stays two-part (already the status quo in `HasContextMenuEligibleSelection`), and the public surface grows `SelectedEdges`.

**Skip rather than narrow**, following ADR 0017's locked-entity amendment and ADR 0023's per-item eligibility, which already agree. Delete and lock take both kinds; align, distribute, z-order, group and resize read instances only, with an edge contributing nothing to the selection bounds and nothing to ADR 0014's thresholds. Two things fall out of settled rules rather than being chosen: **the property bar goes empty on a mixed selection**, because ADR 0021's role intersection is empty across the four edge roles and the seven author-facing ones, and **ADR 0023's "no mixed locked-and-unlocked selection" survives untouched**, since ADR 0017's participation predicate already covered edges.

**The marquee takes an edge by closure, and all three geometric tests fail.** An endpoint bounding box selects lines it visibly does not cover, which is ADR 0017's own reason for refusing the press margin to the marquee. True line geometry sweeps up every long edge merely passing behind the target cluster, which then joins the delete composite. Geometric enclosure breaks on the case it exists for: a component is selected by *intersection*, so a shape can be selected while its ports sit outside the band, and deleting two shapes selected that way leaves the edge between them dangling. So an edge joins the marquee when **every endpoint is either attached to a component the marquee selected, or floating inside the band** — ADR 0013's `Interior edge` reused verbatim, gaining a second reader, so what a marquee selects and what a copy carries are one set. ADR 0006 is untouched, and its reasoning would not have transferred anyway: it rejected containment because enclosing a *box* precisely is fiddly, and an edge between two enclosed boxes is enclosed for free. Three problems are sidestepped rather than answered (line-against-rect intersection, ADR 0027's orthogonal bulge, LOD). Cost: a band over the middle of an edge selects nothing. Select-all has no band, so ADR 0013 gains a line taking every edge, which is the reported defect closing.

**A move translates floating endpoints only**, and bullet 4's double-apply hazard is **unreachable by construction** — an attached endpoint has no stored coordinate to write. A both-floating edge is not exotic: ADR 0009's palette creates connectors that way, so "edges never move" would leave a fresh connector repositionable one end at a time.

**Found on the way, and it is a live defect rather than a consequence: repositioning an edge endpoint has never entered history.** `ApplyEdgeEndpointEdit` writes `edge.Source`/`edge.Target` directly and its one caller does not wrap it in `_history.Do`. Creating an edge goes through `AddEdgeCommand`; moving one of its ends goes through nothing. `ChangeEdgeEndpointCommand` is therefore new, needed by this decision and closing that hole on the way.

**Bullet 5 is separable and split out** as [A `Group` left referencing deleted members](48-group-referencing-deleted-members.md). Nothing about it changes with edges in the selection, and its edge-shaped sibling is already settled the other way, ADR 0032 having named the dangling-endpoint precedent. Closure makes that precedent ordinary rather than occasional.

**Not in the ticket and now stated precisely: an edge has no tab stop, and ADR 0026 is silent rather than having decided.** `OrderedTabStops` enumerates visible instances and visible groups. After this decision **a mixed selection is constructible only by pointer or by select-all**, because `Ctrl`+`Tab` is the keyboard's additive route and walks a ring edges are not in, while ADR 0021's four edge roles reach the bar through `Ctrl`+`Enter` with no keyboard route to the edge they act on. ADR 0017 holds that pointer participation and keyboard reachability are separate properties, so membership does not decide it; split out as [Keyboard reach for an edge](49-keyboard-reach-for-edges.md) with its full scope written in rather than handed off in a sentence.

**ADR 0031's snapshot keeps its rule and loses its reason.** It covers both selection fields together *because they are mutually exclusive*, and after this decision they are not. The rule holds more firmly than before, on the replacement reason that both fields carry selection at once.

Amends **0006**, **0013**, **0014**, **0020**, **0023**, **0026** and **0031**, with `Selection`, `Edge`, `Interior edge` and `Selection snapshot` widened in `CONTEXT.md`.

**Unblocks [Clipboard and duplication for a lone edge](36-edge-clipboard-and-duplication.md).** Graduated [Where the non-instance layers sit in the z-order](50-non-instance-layers-in-z-order.md) out of the fog: that patch was waiting on whether an edge is a peer of a component in the z-order or a layer beneath it, and the skip rule answers it, not a peer.
