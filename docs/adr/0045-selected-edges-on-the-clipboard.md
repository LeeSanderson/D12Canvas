# A selected edge always travels, and an end it cannot keep becomes floating

A copy carries the selected instances closed over interior edges, as ADR 0013 says, **plus every selected edge**. Duplicate and clone drag build the same thing. Cut is copy plus delete with no exception for edges. Paste drops an edge only when an attached end names an instance that did not materialise. Placement reads ADR 0015's extent, which counts edge endpoints, rather than a bounding box over instances.

## The ticket's premise had moved

The question was written when `_selectedEdgeId` was an exclusive slot, so a selected edge was always alone and never interior. ADR 0037 replaced the slot with a second set. The lone edge is now one case of a wider one: an edge the user selected whose endpoints are not all on copied instances. That covers a lone attached edge, a palette connector with floating ends, and an edge shift-clicked in beside one of its two shapes.

## Explicit selection overrides the interior rule for edges

ADR 0013 rejected carrying half-attached edges for two reasons. A floating stub "points at nothing in particular", and a kept outside `ComponentId` wires the pasted edge back to the original instance. Both reasons are about an edge **swept in**: the user copied two shapes and an edge to a third came along unasked. They say nothing about an edge the user picked. For that edge, a line that points at nothing in particular is what was asked for.

So each end of a selected edge goes into the payload by one table:

| End | In the payload |
| --- | --- |
| Attached to a copied instance | Attached, remapped to the new id |
| Floating | Floating, at its coordinate, moving with the payload |
| Attached to an instance not copied | Floating, at the end's current resolved position |

Swept-in edges keep ADR 0013's rule unchanged. An edge that nobody selected travels only when it is interior.

Excalidraw and tldraw both paste an arrow copied without its shapes as an unbound arrow of the same shape. That is from knowledge of those tools rather than from ticket 03's pinned teardown, which does not cover it.

## Duplicate never keeps an outside attachment

Duplicate is the one operation where keeping an outside `ComponentId` would resolve, because the original instances are on the same board. It does not keep it. A second edge between the same two ports draws over the first, so the duplicate looks like it did nothing, which is the invisible pile ADR 0013's cascade exists to prevent. And it would make duplicate and paste give different results from one selection, which ADR 0042 refused for clone drag and ADR 0030's `Quick create` depends on not happening.

So duplicate and clone drag build exactly the copy payload and place it by their own rules: `+20, +20`, then the `Duplicate run` offset (ADR 0039), or the pointer delta (ADR 0042). A lone attached edge duplicates as a floating line beside the original. A parallel connection is still one drag from the port.

An edge from a selected shape to an unselected one, shift-clicked in, needs nothing extra. The copied shape keeps the copied edge's source end. The other end floats at the unselected shape's port position plus the same offset, so the copy leans the way the original does.

## Cut has no edge case

Cut stays the clipboard write plus the delete composite, in one history entry. ADR 0013's guard that cut never destroys what copy did not capture still holds, and no longer fires on an edge, since every selected edge is captured. `Ctrl+X` on a lone edge removes it, and it pastes back as a floating line. Undo restores the edge with its attachments while the clipboard keeps the floating copy, which is ADR 0013's existing rule that undo does not un-write the clipboard.

Cut, Copy and Duplicate become eligible on any selection holding an edge, by ADR 0023's existing per-item eligibility. An edge-only menu goes from four rows to seven: Cut, Copy, Paste, Duplicate, Delete, Lock, Zoom to Selection.

The chords need focus inside the canvas. A pointer press on an edge puts it there. A keyboard user still has no route to an edge, which is [Keyboard reach for an edge](../../.scratch/canvas-interaction-quality/issues/49-keyboard-reach-for-edges.md) and unchanged here.

## Paste drops an edge only for a missing attached end

ADR 0013 has paste re-apply the interior rule to what materialised. The payload does not record which edges were selected, so that re-check would drop every floating line this decision carries.

Paste instead drops an edge when **an attached end names an instance that did not materialise**. A floating end always passes. After the copy table, every attached end in a payload names an instance in the same payload, so in practice this drops only edges whose instance had an unknown `ComponentTypeKey`. The rule reads the payload, not the selection it came from, so it also covers a saved board file pasted in.

Such an end is dropped with its edge rather than floated. Floating it needs a position, and the position comes from a port on a component paste could not bind. `ParseEntries` already turns an unbindable entity into one warning and one dropped entity.

An edge-only payload still has a `Components` array, because `BoardJsonSerializer` writes it empty rather than leaving it out, so paste's structural recognition accepts it.

## Placement reads the extent, arrangement reads instances

Two placement rules read a bounding box. ADR 0013's paste moves the payload by one delta from "its own bounding box", and ADR 0039's run measures "the selection bounds' top-left". ADR 0037 says an edge contributes nothing to the selection bounds. On an edge-only selection both boxes are empty.

ADR 0015 already defines the box that is not: every instance's bounds unioned with every resolvable edge endpoint, restricted to a set for zoom to selection. **Placement reads that extent.** The paste delta reads the payload's extent, and the run captures and measures the selection's extent. ADR 0037's rule stays, scoped to arrangement (align, distribute, resize, z-order), which is what its argument was about, since those write instance `Bounds`.

On a selection with no edges the two boxes are the same, so nothing shipped moves. When a selected edge's floating end sticks out past the shapes, that end sets the top-left. Source and copy share geometry, so the measured offset still equals the visible step.

## `Interior edge` has three readers that differ

ADR 0037 said the marquee reused `Interior edge` "verbatim" and that "the definition is unchanged". It added a clause: an end floating inside the band counts. `CONTEXT.md` then said a marquee and a copy select "the same set by construction". Under ADR 0013's copy rule they did not. A marqueed palette connector was selected and then dropped by `Ctrl+C`.

After this decision the readers say what each does:

- **Copy** carries interior edges plus every selected edge.
- **The marquee** takes an edge when every end is attached to a component it selected or floating inside the band.
- **Paste** keeps an edge when every attached end resolves to a materialised instance.

The mismatch is closed by copy carrying the selection, not by the three sharing one predicate.

## What this amends

**ADR 0013**: the copy rule, the removed cut sentence, the paste re-check and the placement box.

**ADR 0023**: Cut, Copy and Duplicate eligible on an edge selection, seven rows.

**ADR 0037**: "contributes nothing to the selection bounds" is scoped to arrangement, and "reused verbatim" is corrected.

**ADR 0039**: the run measures the selection's extent.

**ADR 0042**: its open bullet on selected edges with floating ends has its answer, with no change to clone drag.

**ADR 0003, ADR 0004 and ADR 0007 are untouched.** The envelope is unchanged and no command type is added.

## Considered and rejected

- **A lone edge uncopyable by design**: ADR 0013's reasons are about edges the user did not select, and the menu stays four rows on the object a user makes most often.
- **Fixing the marquee mismatch by adding the floating clause to copy's interior test**: carries a palette connector, still drops a selected attached edge, and leaves cut on it a no-op.
- **Keeping outside attachments on duplicate**: a second edge over the first, invisible, and a result different from paste.
- **Keeping the outside `ComponentId` in the payload**: rewires a same-board paste to the original shapes and dangles across boards, as ADR 0013 found.
- **Cut as delete without capture on an edge**: a second rule where copy-then-delete now covers it.
- **Re-applying the interior test on paste**: drops every floating line the copy carried.
- **Floating an end whose instance did not materialise**: needs a port position from a component paste could not bind.
- **Instance bounds for placement, with a special case for edge-only selections**: a second box rule where ADR 0015's extent already counts edges.
