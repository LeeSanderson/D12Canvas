# Alt on a drag clones the selection, and the copies are what moves

Holding Alt during a `MoveSelection` turns it into a **clone drag**. The originals stay where they are, and a copy of the selection follows the pointer. At release the copies are added to `Board` and become the selection. Alt is read live, so pressing or releasing it mid-drag switches between moving and cloning, and the release commits whichever the screen last showed.

This binds the one pointer modifier ADR 0018 and ADR 0022 left free. All four reference tools put duplicate-on-drag on Alt. ADR 0039 removed the reason the ticket first gave for building it, since a `Duplicate run` reads its offset from committed bounds and no longer needs a clone gesture to feed it. What is left is enough: it places the copy where the pointer is in one gesture, and without it an Alt-drag here is a plain move, so a user who expects a copy finds they have moved the original.

## The copies move, not the originals

Excalidraw drags the originals and leaves the copies behind, so its preview is an ordinary move. That model does not work here. Every edge attached to a selected instance from outside the selection would travel with the originals, the copy left at the old position would have no connections, and the moved shapes would keep the ids a host may have stored. To the user, the edges jump to the wrong shape.

So the originals keep their ids, their position and their attached edges. The new entities are the ones in the user's hand.

## The preview holds the copies

ADR 0020 requires the commit to write what the preview showed, so the copies have to exist during the drag. The `Gesture preview` gains a **pending fragment** slot: entities that will be added at release and are not in `Board` yet.

On the first `OnMove` with Alt held, the gesture builds the fragment through ADR 0013's duplication path. That regenerates ids across all five references, `PortDef.Id` included, and carries interior edges by closure. The canvas renders the fragment's instances and edges as it renders committed ones. Their in-flight positions go through the existing bounds overrides, keyed by their new ids, so live geometry, attached interior edges and snapping need nothing new.

At release the commit adds exactly those entities, with the ids they already have. Blazor keeps the mounted components, so nothing remounts at the moment of commit.

Consequences:

- **The copies mount while the gesture is live, whatever windowing says.** Windowing reads committed bounds (ADR 0020), and the fragment has none. The copies take the originals' level-of-detail state as frozen at press.
- **Cancel is still free.** Dropping the preview drops the fragment. No command was ever created, so ADR 0031's three steps are unchanged.
- **The originals are snap candidates and the copies are not.** ADR 0024 draws candidates from the viewport query, which reads `Board`. The originals are in it at their committed positions, and the fragment is not in it at all. Alt-dragging along an axis aligns the copy to its own source, with no rule added to say so.
- **Nothing is built below the threshold.** ADR 0018's first `OnPointerMoved` means promoted, so an Alt-click is a click.

A ghost outline with the copies built at release was considered. It needs no new slot, but release swaps an outline for real author components, and the commit builds entities the user never saw. That is the "commit recomputes" shape ADR 0020 removed.

## Alt is live, and the gesture is still `MoveSelection`

tldraw toggles its clone mid-drag and rewinds its history to a mark, because it writes its store during the drag. ADR 0020 never writes `Board` before release, so here a toggle only changes what the preview holds:

- **Alt pressed mid-drag:** the gesture builds the fragment, the bounds overrides move from the originals to the copies, and the originals show at their committed positions.
- **Alt released mid-drag:** the fragment is dropped and the overrides go back onto the originals at the current delta.

The release writes one command either way, a move or an add. Nothing is rewound, and ADR 0007 gains nothing.

The owner, the capture, the claiming button and the press-anchored delta all stay the same through a toggle. Only what the release commits changes. So a clone drag is a mode of `MoveSelection`, the closed set of eight keeps eight members, and ADR 0018's rule that a gesture's identity never changes mid-press is untouched.

Alt reaches the gesture on `OnPointerMoved`, which already carries modifiers for `Ctrl` and `Shift` under ADR 0024. Pressing Alt without moving fires no pointer event, so the toggle shows on the next move. That is the same gap ADR 0024 accepted for `Ctrl`, and no new channel is added.

Each toggle mounts or unmounts the copies' author components. They are fresh copies with no state of their own, so the churn loses nothing.

## The release writes the last published preview

Two cases follow from that rule with no rule of their own:

- **Alt released and then the pointer released, with no move between.** No preview was published after the Alt release, so the commit is a clone, which is what was on screen. Reading Alt from `pointerup` would commit something the user never saw.
- **A copy dragged back onto its source.** ADR 0020 commits only entities whose previewed bounds differ from committed. A fragment entity has no committed bounds, so it always counts as changed, and the copy lands stacked on its source. Refusing it would need a second comparison between each copy and its source, and would refuse a stacked copy placed on purpose.

## Selection

`Selection` stays on the originals until release. It is read by the panel, the menu and every command, and each resolves its ids against `Board`, which the copies are not in yet. Writing the fragment's ids into `Selection` at promotion would put ids that resolve nowhere in front of every one of those readers.

The canvas instead draws the selected look on the fragment, and drops it from the originals, while clone mode is on. This is styling from the preview, as ADR 0020 already does when an edge override draws the dashed stroke. The `Selection snapshot` taken at press restores a selection that never changed.

At release the copies, instances and interior edges, become the selection. That starts a `Duplicate run` with the copies as its output and the originals' committed selection bounds as its source, which is the case ADR 0039 anticipated. `Ctrl+D` straight after a clone drag repeats its offset.

## A clone copies exactly what duplicate copies

The fragment is what `Ctrl+D` would build from the same selection. Only the placement differs. The commit is a `CompositeCommand` of existing primitives with no new command type, as paste, duplicate and `Quick create` are.

- **Groups** copy as groups, with new group ids over the copied members.
- **Edges that cross the selection boundary** are not copied. They stay on the originals.
- **Selected edges with floating ends** (ADR 0037) copy however duplicate copies them. That is open in [Clipboard and duplication for a lone edge](../../.scratch/canvas-interaction-quality/issues/36-edge-clipboard-and-duplication.md), and a clone drag takes its answer.
- **The copies go on top**, new-on-top as for duplicate, during the drag as well as after it, so the copy in the user's hand is never painted under its source.
- **Locked entities cannot take part.** A primary press never hits one (ADR 0017), and ADR 0023 shows a selection containing a locked entity is that one entity alone.

A clone-specific rule, such as copying crossing edges and re-attaching them to the copies, would give two duplication gestures two different results.

## Alt elsewhere

Alt binds only on `MoveSelection`. `Pan`, `MarqueeSelect`, `DragEdgeEnd`, `SelectEdge`, `Native` and `MinimapPan` ignore it.

`ResizeSelection` has an open claim. All four reference tools use Alt on a resize to resize from the centre, and nothing on this map has decided it. It is neither part of this decision nor covered by the out-of-scope ruling on aspect-lock, so it has its own ticket.

## Keyboard and platform

ADR 0026 adds a chord only where nothing else reaches. `Ctrl+D` followed by arrow-key nudges reaches the same result, a placed copy that starts a run, so this adds no chord and changes nothing in that table.

Two browser and platform facts are asserted by an `Interaction probe` (ADR 0025), not assumed:

- On Windows, releasing Alt after a drag can activate the browser's menu bar in some engines. The probe establishes whether the intervening pointer press suppresses that, and whether the gesture needs to prevent the default on Alt's `keyup` while it is live.
- On Linux window managers that bind Alt+drag to window moves, KDE Plasma 5 by default, the page never receives the press. Nothing a page does reclaims it. This is documented, and the keyboard route is the way round it.

## What this amends

**ADR 0020 is amended**: the `Gesture preview` gains a pending fragment slot. The invariant that forced ADR 0037's moved-endpoints slot forces this one too, since an entity the preview cannot express is one the commit cannot write.

**ADR 0018 is amended by addendum**: Alt is bound on the pointer, as a live mode of `MoveSelection`, with no new member.

**ADR 0039 is confirmed**: its "if Alt-drag duplication ships" case now applies as written.

**ADR 0013 is reused, not amended**: a clone drag is a third consumer of the duplication path.

**ADR 0007 is untouched.** One gesture, one entry, and no mark-and-rewind.

## Considered and rejected

- **Declining it**: leaves a shortcut every reference tool shares doing something else here.
- **Moving the originals and leaving copies behind**: edges attached from outside the selection travel with the originals, and the moved shapes keep ids a host may have stored.
- **A ghost outline, with the copies built at release**: the release swaps an outline for real components, and the commit builds entities the user never saw.
- **Latching Alt at press**: avoids a history rewind this model never needs, and gives up starting a drag and then reaching for Alt.
- **A ninth pointer gesture**: nothing about ownership, capture or the delta differs from `MoveSelection`.
- **Writing the copies' ids into `Selection` mid-drag**: every `Selection` reader would see ids that resolve nowhere.
- **Reading Alt from `pointerup`**: commits something the user never saw.
- **Dropping a clone released on its source**: a second comparison, and it refuses a stacked copy placed on purpose.
- **A clone-specific copy rule**: two duplication gestures with two different results.
- **A keyboard chord for clone**: duplicate-then-nudge already reaches it.

## Addendum (surfaced while resolving the latched-versus-live modifiers ticket)

ADR 0043 amends two statements above, and leaves the rule that the release writes the last published preview as it is.

**An Alt toggle shows at once.** "Pressing Alt without moving fires no pointer event, so the toggle shows on the next move" no longer holds. While a gesture holds capture, JavaScript re-sends the last pointer position as a move whenever modifier state changes, so the copies appear or go away the moment Alt does.

**Alt released and then the pointer released, with no move between, commits a move.** The Alt release publishes a preview of a move, so the last published preview is a move. Before the re-send it was still a clone, and the commit was a clone after the user had let go of the key that means clone.

## Addendum (surfaced while resolving the lone-edge clipboard ticket)

ADR 0045 answers the open bullet on selected edges with floating ends, and clone drag takes the answer with no rule of its own. Every selected edge is in the fragment. An end on a copied instance is attached to the copy, a floating end moves with the delta, and an end on an instance not in the selection becomes floating. The bullet's sibling still holds: an edge that crosses the selection boundary and is not itself selected is not copied.

## Addendum (surfaced while resolving the buried-instance ticket)

ADR 0046 gives Alt a meaning on a release from `pointing`, which this ADR left free. Alt+click selects the next entity down the `Hit stack`. A press that crosses the threshold is still a clone drag, so "an Alt-click is a click" holds and the click now does something. The Windows probe above gains a case: whether releasing Alt after an Alt+click with no drag activates the browser's menu bar.
