# Edges in a multi-selection

Type: grilling
Status: open

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
