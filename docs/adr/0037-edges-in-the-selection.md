# Edges join the selection as a second set, and a mixed selection skips what it cannot express

An `Edge` becomes a member of the `Selection`. `DiagramCanvas` holds two parallel sets, `_selectedInstanceIds` and a new `_selectedEdgeIds`, replacing the exclusive `_selectedEdgeId` slot. A command that cannot express an edge skips it rather than refusing. The marquee takes an edge by closure over what it already selected, never by geometry. A move translates a selected edge's floating endpoints and never its attached ones.

## Three tickets hit this wall from three directions and each worked around it

ADR 0013 found that select-all excludes edges, so select-all-then-delete does not clear a board, and declined to fix it. ADR 0015 found that frame-selection has to read the exclusive slot as well as the instance set, or framing a selected edge silently does nothing. ADR 0016 shipped edge colour as the fourth settable edge property behind a missing surface. Each declined because widening the slot amends the selection model.

ADR 0021 sharpened the third since that ticket was written. Its role vocabulary gives an edge four of its own, `EdgeRouting`, `EdgeSourceArrow`, `EdgeTargetArrow` and `EdgeColour`, so the property bar now has four controls that can only ever act on one edge at a time. ADR 0030's `Quick create` makes an edge the thing a user produces most often, so the limitation gets worse from here rather than better.

## Two sets, because a bare `Guid` cannot say which collection it came from

Instance ids, group ids and edge ids are all plain `Guid`s drawn from one space, and `Board` tells them apart only by which of `GetComponent`, `GetGroup` and `GetEdge` returns non-null. `ExpandInto` already leans on that: a null from `GetGroup` is how it concludes the id is an instance.

A single flat `HashSet<Guid>` therefore feeds edge ids straight into group expansion, and out the other side into `SelectedComponents`, which returns `Array.Empty` on the **first** unresolvable id. That is the hazard ADR 0021 had to remove for groups, arriving again by a different door, and it would empty the property panel on every mixed selection until each reader learned to probe three collections.

A single set of typed refs fixes the ambiguity and buys nothing else. Every command is already per kind, `RemoveEntityCommand` against `RemoveEdgeCommand` and `MutateEntityCommand` against `ChangeEdgeStyleCommand`, so the discrimination happens at the command whatever the collection holds. The one thing a single ordered collection would add is selection order, and ADR 0014 established that order here is not merely unused but unrecoverable, since the set is a `HashSet` and a marquee would fill any order in hit-test sequence.

Two sets is the only shape where an edge id is **structurally unable** to reach group expansion, rather than guarded out of it. Group flattening is meaningless for an entity that cannot be a group member.

The cost is stated rather than glossed. "Is anything selected" stays a two-part question, which is already the status quo in `HasContextMenuEligibleSelection` and lives in one property. And the host-facing surface grows a `SelectedEdges` beside `SelectedComponents`, which is a genuine addition, not a rename.

ADR 0031's `Selection snapshot` keeps its rule and loses its reason, which is worth separating rather than glossing. It captures both selection fields together, and it still must, but the argument it gave was that the fields are mutually exclusive so restoring one without the other desynchronises them. After this decision they are not exclusive. The rule survives on a stronger footing than the one it was given: both fields have to be captured because both can hold content at once.

## Skip what cannot be expressed, rather than narrowing the action set

This repo has already answered the same question twice. ADR 0017 amended ADRs 0013 and 0014 to **skip** locked entities rather than refuse the command, and ADR 0023 composes menu rows by per-item eligibility rather than writing a menu per context. Narrowing would be a third rule where two agree.

| Action | On a mixed selection |
| --- | --- |
| Delete | Both kinds, one `CompositeCommand` mixing `RemoveEntityCommand` and `RemoveEdgeCommand` |
| Lock, Unlock | Both kinds. ADR 0017 put the flag on instances and edges |
| Align, distribute | Instances only. An edge contributes nothing to the selection bounds and nothing to ADR 0014's 2-and-3 thresholds |
| Z-order | Instances only. An edge has no `ZIndex` |
| Group | Instances only. `Group.MemberIds` feeds `Board.GetBounds`, which needs `Bounds` an edge does not have. The selection collapses to the new group, so a selected edge is dropped from the selection rather than carried into the group |
| Resize | Instances only. An edge does not widen the shared bounding box |
| Copy, cut, paste, duplicate | Both, with the lone-edge case remaining ticket 36's |

Two consequences fall out of existing decisions rather than being chosen here.

**The property bar goes empty on a mixed selection.** ADR 0021 presents a multi-selection as the role intersection, and its four edge roles are deliberately disjoint from the seven author-facing ones. So a shape and an edge selected together show no bar. That follows from a settled rule and needs no exception, but it is a real cost: after this decision a marquee over a connected region is the ordinary way to select, and the bar is reachable only on a same-kind selection.

**ADR 0023's "there is no mixed locked-and-unlocked selection" survives intact.** Its argument is that a locked entity cannot be primary-clicked or marquee'd and a secondary press reaches exactly one at a time. Edges joining the marquee touches neither half, because ADR 0017's participation predicate already covers an edge and already excludes a locked one.

## The marquee takes an edge by closure over the selected set, not by geometry

The ticket offered two tests, both geometric, and both fail.

**An endpoint bounding box** fails on ADR 0017's own rejected list, which refuses to apply the press margin to the marquee because it "would only make it select things it visibly does not cover". A diagonal edge's endpoint box is mostly empty space, so a band clipping one corner selects a line it never touched.

**True line or path geometry** avoids that and sweeps up every long edge merely passing behind the cluster being aimed at. Under the skip rule above those go into the same `CompositeCommand` when the user presses Delete.

**Geometric enclosure of both endpoints** breaks on the case it exists for. A component is selected by intersection, so a shape can be in the selection while its ports sit outside the band. Marquee two shapes that way, delete, and both shapes go while the edge between them survives dangling. The geometric test disagrees with the selection it is meant to agree with.

So the test is not geometric. **An edge joins the marquee when every one of its endpoints is either attached to a component the marquee selected, or floating at a point inside the band.**

That is ADR 0013's `Interior edge` reused verbatim, gaining a second reader. What a marquee selects and what a copy carries become the same set, so no reconciliation rule is needed between them, and the predicate is written once. ADR 0006's intersection semantics are untouched, because this is not a geometric test at all.

ADR 0006's reasoning would not have transferred anyway, and checking that rather than assuming it is the point. It rejected full containment because enclosing a small or densely packed **box** precisely is fiddly. An edge between two boxes already enclosed is enclosed for free, so the argument that settled containment for components says nothing about edges. This is ticket 19's check applied again: name the distinction an imported argument turns on, then ask whether this decision acts on it.

Three problems are sidestepped rather than solved: line-against-rectangle intersection, ADR 0027's orthogonal routing bulging outside the band, and whether LOD excludes an edge the way it excludes an instance.

The cost: **a band drawn over the middle of an edge, touching neither of its components, selects nothing.** The edge has to be clicked, which ADR 0017 already gives a 20 screen pixel press band.

Select-all has no band, so ADR 0013's "top-level entities" gains a line taking every edge. That is the originally reported defect closing.

A pointer builds a mixed selection through `SelectEdge`, whose press resolves at **release**, since ADR 0018 gives it no active phase. ADR 0022's press-time collapse therefore does not reach it, and ADR 0006's shift-click toggle applies unchanged.

## A move translates floating endpoints and never attached ones

The ticket's stated hazard, that a component and an edge attached to it both moving would double-apply the delta, is unreachable by construction. An attached endpoint has **no stored coordinate to write**, so there is nothing to double-apply and no guard is needed.

- Edge plus both its components selected: the components move, the attached ends follow, nothing is written for the edge. Rigid.
- Edge with one end on a selected component and one floating: the component moves, that end follows, the floating end takes the same delta. Also rigid, and this is the case a blanket "edges never move" gets wrong.
- Edge with both ends floating: the whole edge moves. Not exotic. ADR 0009's palette creates a connector with both endpoints floating, so under "edges never move" a freshly placed connector could only be repositioned one end at a time.
- Edge with one end on an unselected component: that end stays, the floating end moves, the edge reshapes. Closure means a marquee can never produce this, so it arises only from a deliberate shift-click, where reshaping is the honest reading of what was picked.

**`ChangeEdgeEndpointCommand` is new, and it closes a live defect rather than only serving this decision.** `ApplyEdgeEndpointEdit` writes `edge.Source` or `edge.Target` directly and its one caller does not wrap it in `_history.Do`, so repositioning an edge endpoint has never been undoable. Creating an edge goes through `AddEdgeCommand`; moving one of its ends goes through nothing.

## What this decision deliberately does not settle

**`OnDeletePressed` can leave a `Group` referencing deleted members**, and that is its own ticket. Nothing about it changes with edges in the selection, and its edge-shaped sibling is already settled the other way: ADR 0032 named the dangling-endpoint precedent and relied on it. Closure makes that precedent ordinary rather than occasional, since marqueeing one shape of a connected pair deliberately leaves the edge unselected.

**An edge still has no tab stop**, and that is its own ticket too. `OrderedTabStops` enumerates visible instances and visible groups, and ADR 0026 is silent on edges rather than having decided against them. ADR 0017 already holds that pointer participation and keyboard reachability are separate properties, so membership does not decide reachability. The gap is now precise: **after this decision a mixed selection is constructible only by pointer or by select-all**, because `Ctrl`+`Tab` is the keyboard's additive route and it walks a ring edges are not in. ADR 0021's four edge roles reach the bar through `Ctrl`+`Enter` with no keyboard route to the edge they would act on.

## Amends ADR 0006

Edges participate in a multi-selection. The clipboard addendum's sentence "**Edges cannot participate in a multi-selection at all**, because an edge occupies its own exclusive selection slot" is the limitation this replaces, and that addendum recorded it as "recorded, not fixed". Marquee intersection, shift-click toggle and ADR 0022's additive band are all unchanged for instances, and gain no geometric counterpart for edges, because an edge enters the marquee by closure.

## Amends ADR 0013

Select-all takes every edge in addition to top-level entities, which is what makes select-all-then-delete clear a board.

`Interior edge` gains a second reader. It was written as the test for what a copy carries; it is now also the test for what a marquee selects. The definition is unchanged and the two consumers agree by construction rather than by a rule.

## Amends ADR 0014

The align and distribute actions read instances only. A selected edge contributes nothing to the selection bounds and is not counted toward the thresholds of 2 and 3, so a selection of one component and one edge is below threshold and the actions stay hidden.

## Amends ADR 0020

The `Gesture preview` gains a slot for moved endpoints. Its second typed slot is "at most one pending edge line", shaped for a connector drag, and a move of several floating-ended edges needs a position per endpoint. The invariant that the preview is the truth and the commit writes it verbatim is what forces this: the preview has to be able to express what the commit will write.

## Amends ADR 0023

The arrangement section reads instances only, so it appears whenever the selection holds at least one instance and is absent on an edge-only selection, which is what that ADR already observed. Lock and Unlock read both kinds. The eligibility mechanism is unchanged and this adds no row.

## Amends ADR 0031

The `Selection snapshot` still covers both selection fields together and the rule does not move. Its stated reason does. That reason was exclusivity, "restoring one without the other desynchronises them", which is false once both fields can hold content at once. The replacement reason is simply that both fields carry selection now, which is why the rule holds more firmly than before rather than less.

## Amends ADR 0026

The arrow-key pan fallback narrows to an **empty** selection. A non-empty selection that writes nothing becomes a no-op rather than panning.

Today an edge-only selection takes the pan fallback because an edge had no `Bounds` to nudge. Under this decision an edge-only selection sometimes has a floating endpoint to nudge and sometimes does not, so keeping the fallback makes one keypress mean two different things with nothing on screen distinguishing them. Narrowing it is ADR 0026's own stated reason, "with nothing selected there's no instance to nudge", read literally. It is a change to shipped behaviour.

## Consequences

`SelectedComponents` loses its `_selectedEdgeId is not null` short-circuit, or a mixed selection shows an empty property panel.

`DiagramCanvasEdgeSelectionTests`, `DiagramCanvasArrowKeyMoveTests` and `DiagramCanvasConnectorPaletteTests` each carry a comment asserting that an edge is never mixed into the instance selection. Those comments are now wrong and the assertions behind two of them describe behaviour that is being replaced.

The `ChangeEdgeEndpointCommand` above is the first command for an edge's geometry rather than its style, which means the closed command set `CONTEXT.md` lists grows by one. ADR 0028 already handed on that the listed set was wrong by five.

## Considered and rejected

- **One flat `HashSet<Guid>`.** Cannot say which collection an id came from, feeds edge ids into `ExpandInto`, and empties `SelectedComponents` on the first unresolvable one.
- **One set of typed selection refs.** Removes the ambiguity, then pays a type for ordering that ADR 0014 established is unrecoverable and unused, while the per-kind discrimination still happens at every command.
- **Narrowing the action set on a mixed selection.** A third rule where ADR 0017's locked-entity skip and ADR 0023's per-item eligibility already agree.
- **Marquee by endpoint bounding box.** Selects edges it visibly does not cover, which is exactly why ADR 0017 refused the press margin for the marquee.
- **Marquee by true line or path geometry.** Sweeps up long edges merely passing behind the target cluster, and they then join the delete composite.
- **Marquee by geometric enclosure of both endpoints.** Disagrees with the intersection-selected components it is meant to accompany, leaving a dangling edge after deleting both its shapes.
- **Edges never move.** Leaves a palette-created connector, which ADR 0009 creates with both ends floating, repositionable only one end at a time.
- **Resolving the `Group`-with-deleted-members case here.** Unchanged by edges in the selection, and it runs into ADR 0003's flat model and ADR 0007's undo symmetry, which is a different argument set.
- **Giving edges tab stops here.** Its real question is ring length and whether an edge's reading-order position means anything when it spans the board, neither of which is about selection membership.

## Amended by ADR 0045

**"An edge contributes nothing to the selection bounds" is scoped to arrangement**: align, distribute, resize and z-order, which write instance `Bounds`. Placement, meaning the paste delta and the `Duplicate run` offset, reads ADR 0015's extent, which counts edge endpoints.

**The marquee did not reuse `Interior edge` verbatim.** The clause admitting an end floating inside the band was new, and ADR 0013's copy test had no such clause, so a marqueed palette connector was selected and then dropped by a copy. ADR 0045 closes that by having copy carry every selected edge, not by giving the readers one predicate. The table's "lone-edge case remaining ticket 36's" is answered there.
