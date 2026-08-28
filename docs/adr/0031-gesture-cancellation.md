# Escape cancels the pointer gesture that owns the press, restoring geometry and selection but never the viewport

Cancelling a pointer gesture is three steps, all owned by `DiagramCanvas`: drop the `Gesture preview`, restore the `Selection snapshot` taken at press, and mark the gesture `cancelled`. A cancelled gesture keeps the pointer until its claiming button comes up, so the release still reaches an owner and that owner does nothing.

No gesture implements any of this. `OnCancel` leaves ADR 0018's interface.

## Most of this question was answered before it was reached

Ticket 22 was written after ticket 04 reproduced six leaking gestures, and three of its five bullets have since been decided elsewhere.

The **cancel input** is Escape, routed to the active gesture (ADR 0018, amending ADR 0009's Escape row; ADR 0026 carries the table row). The ticket asks for the collision with a secondary press mid-drag to be resolved, and ADR 0022 already resolved it the other way, rejecting that route because two abort paths give a user no way to know which one they invoked.

**Revert versus commit** and the **history question** are both answered by ADR 0020's invariant that no pointer gesture creates a command before release. Committed state is never touched mid-gesture, so there is no inverse to apply and no snapshot to restore, and a cancelled gesture is zero history entries by construction rather than one that undoes itself. `_history` never has to express a mark. tldraw needs one because it writes through; this model does not.

What is left is the residue ADR 0020 named and handed here: *"Selection revert therefore needs a pre-press snapshot, and belongs to the cancellation decision. Geometry revert is free; selection revert is not."*

## The viewport is the one thing cancel does not restore

Three kinds of state can have changed by the time Escape arrives. Geometry lives in the preview and is free to discard. Selection and the viewport do not, and they look alike: ADR 0007 excludes selection from undo explicitly, and never mentions the viewport at all, so neither has a history route.

They differ on the only thing that matters, which is whether the user can get it back by hand. Rebuilding a twelve-shape `Shift`-click selection is expensive and nothing in the product will do it for you. Panning back is one drag and you can see where you are going while you make it. Navigation is not an edit; there is nothing to regret.

So cancel restores geometry and selection, and leaves `ZoomPanTracker` alone. A cancelled `Pan` or `MinimapPan` restores nothing and only stops panning. The alternative — springing the viewport home — would be the only place in the product where Escape moves the camera, bought with an animation nobody asked for.

## Selection revert reaches the press, not just the drag

The marquee is the easy half. `MarqueeSelect` replaces the selection on every tick, so cancelling a band dragged across the board would otherwise leave the user holding whatever it swept.

The contested half is ADR 0022's press-time collapse, which drops the selection the moment a press lands on a non-member, because the drag has to move something. Twelve shapes selected, press a thirteenth, hit Escape: the twelve come back.

The asymmetry decides it, and it is the same one that settled the viewport. Restoring when the user wanted the thirteenth costs one click. Not restoring when they wanted the twelve costs a rebuild with no undo route. ADR 0022's own justification for the collapse is that *the drag* must move something, and Escape says there is no drag, so the reason for the collapse leaves with it. "Escape forgets this press happened" is also a rule a user can hold; "Escape forgets the part after the threshold" is not.

Two consequences follow rather than being chosen. **Every gesture takes a snapshot**, not just the three that visibly need one, because any gesture can carry a press-time collapse and a per-gesture rule about who needs one is what the ninth gesture forgets. The cost is copying a set of ids once per press. And the snapshot covers **both selection fields together**: ticket 08 found `_selectedEdgeId` is exclusive with `_selectedInstanceIds`, with `SelectedComponents` emptying whenever the edge id is set, so restoring one without the other desynchronises them.

The snapshot is taken by the canvas at press, before the press-time selection decision runs. It sits where ownership is established rather than inside the gesture, for the same reason the rest of cancel does.

## Cancel ends the gesture's effects, not its ownership

`pointing → active` gains a third state. A cancelled gesture holds capture until its claiming button releases, and that release is a no-op.

ADR 0030 supplies the victim of the alternative. A plain click on a port span quick-creates a duplicate instance and an edge. Press a port span, see it is the wrong side, hit Escape, let go: the gesture never crossed the threshold, so it is still `pointing` and has no preview to discard. If cancel means only "drop the preview", Escape does nothing and the release creates the instance and edge it was pressed to prevent. Today's code gets this right for the one gesture that has it, because `CancelPortDrag` clears `_isConnectingPort` and the mouseup path is guarded by the same flag — one flag read twice. With `OnCancel` and `OnRelease` as separate methods, nothing stops both running unless the model says so.

Keeping capture rather than releasing it is the other half. Dropping capture with the button still down sends the eventual `pointerup` to whatever is under the cursor, so cancelling a drag can end in an accidental click on author content, a chrome button or the palette. It would also have to clear the gesture before releasing capture or ADR 0018's `lostpointercapture` net fires on a live gesture and writes the `console.error` ADR 0025 fails the suite on.

Holding it means ownership is coextensive with the press rather than with the gesture's usefulness, which is ADR 0018's own argument against arbitrating at the threshold applied to the other end of the press. A user who hits Escape and keeps dragging by reflex gets silence instead of a second gesture starting from nowhere.

`cancelled` rather than `abandoned`, even though ticket 22 uses that word, because ADR 0018 already spends it on a threshold crossing under a pointing-only owner.

## Interruption is a cancel, and the channel for it does not exist yet

ADR 0018 has two interruption channels, `pointercancel` and the `lostpointercapture` net. Neither fires when the window loses focus with a button held: pointer capture survives it.

So the live hazard is drag a shape, `Alt`+`Tab`, release the button over another application, come back. The page never saw `pointerup`, the gesture is still live, capture is still held, and the next pointer move continues a drag the user finished minutes ago. That is leak path seven, through the one door ADR 0018 left unwatched. `DiagramCanvas.razor.js` has no `blur` or `visibilitychange` listener at all; its only window listeners are `keydown` and `keyup`.

A **window `blur` listener becomes the third channel**, and it cancels.

Commit is the tempting reading, because a long careful drag springing home after checking a reference is annoying. It means writing a history entry at a coordinate the user never chose, for a gesture they abandoned, while they cannot see the canvas — the coordinate being wherever the pointer happened to be when focus left. Cancel costs redoing a drag. A silent commit costs a `Ctrl`+`Z` the user has to notice they need. Cancel also matches what `pointercancel` already means, so the three channels agree rather than the newest one being the odd case.

**One word means two events, and the ADRs must not be read as though it meant one.** ADR 0007 records history "on gesture commit (pointer-up/blur)", and that blur is *element* blur committing an inline or prop edit — ADR 0018 says so when it explains that suppressing focus transfer suppresses the blur that commits a sticky note. It is not window blur and does not pre-authorise commit-on-interruption.

Verification splits along ADR 0025's own line. The plumbing is assertable: `window.dispatchEvent(new Event('blur'))` with a live gesture reverts it. Whether a real browser delivers that event on `Alt`+`Tab` with a button held is unmeasured here, and it is the class of fact ADR 0029 had to check by hand because Playwright dispatches synthetic input — device physics, which ADR 0025 already puts out of the suite's reach.

## Escape has three rungs, and is spent for the rest of the press

ADR 0018 writes the fall-through as two rungs, "cancel the active pointer gesture, otherwise clear selection". Today's `OnEscapePressed` handles three states, and the middle one has no owner in ADR 0018's set: it clears `_portFocusInstanceId` and `_pendingConnectorSource`, the **keyboard** connector pick, which ADR 0027 depends on when it notes that auto is reachable only by pressing Escape and starting the pick again.

The chain is **pointer gesture, then keyboard connector pick, then selection**. This is the map's usual reading-failure family running the other way: a rule narrower than the world it governs rather than wider than its own argument.

**A second Escape does nothing.** Because a cancelled gesture still owns the pointer, "active" could be read either way, and the reading matters: if a cancelled gesture stopped counting, the second press would fall through and clear the selection the first press just restored. A user tapping Escape twice because they were not sure the first took would destroy exactly what this decision exists to save. Gating the fall-through on **who owns the pointer** rather than on whether the gesture is still doing anything introduces no second notion of "active", and clearing the selection mid-press is a capability nothing needs.

**Staging Escape at all is a behaviour change.** Today one press does everything at once — cancel the connector drag, clear the keyboard pick, clear the selection, close the context menu — and its comment describes that as deliberate, "a single, full reset rather than a staged one". Under this decision Escape during a connector drag no longer also clears the selection. That is right, since restoring the selection and clearing it in the same press would be absurd, but it is a change users will feel rather than a generalisation of what they have.

## `OnCancel` has no work to do, so it goes

Walking the closed set of eight against the three steps leaves nothing per-gesture:

| Pointer gesture | What cancel undoes | Who owns it |
|---|---|---|
| `MoveSelection`, `ResizeSelection` | preview bounds overrides | the preview, canvas-level |
| `DragEdgeEnd` | the pending-line slot, and ADR 0028's lit drop target | the preview; the highlight derives from a live gesture |
| `MarqueeSelect` | the band, and the selection | the band is gesture-private and dies with the gesture; the selection is the snapshot |
| `Pan`, `MinimapPan` | nothing — the viewport is not restored | — |
| `SelectEdge` | nothing; it has no active phase | — |
| `Native` | nothing; ADR 0018 gives it no capture and tracks nothing, so it is never held and Escape falls to the next rung | — |

So the interface loses the method, and **an argument ADR 0018 made for gesture objects was hollow**. It chose polymorphic objects over a switch partly on this ticket's ground, that "cancellation becomes a method you cannot forget to implement, so the leaks cannot return by accretion". A method with no body guarantees nothing. The decision survives on `OnMove` and `OnRelease`, which genuinely differ per gesture, and the sentence naming cancellation as the reason is corrected rather than left standing.

Where the guarantee actually lands is ADR 0025's `[Theory]` over the closed eight, which gains **a cancel case per member** beside its release-reliability case. That is the third load-bearing use of that closure, and it means a ninth gesture fails the suite until someone has decided what cancelling it means.

The cost is real and is accepted. ADR 0025's cheapest drive point is gesture objects driven directly over a fake context, and deleting the method puts the cancel path beyond it, leaving the canvas-level drive points. Cancel is not gesture logic, so asserting it at canvas level is asserting it where it lives.

## The minimap could not hear Escape

ADR 0026 gives Escape no guard of its own, on this reasoning: *"ADR 0018's press-time focus transfer means a live gesture implies focus inside the container, so there is no reachable state where it needs to fire from outside."* Its uniform guard passes when `document.activeElement` is inside `.diagram-container`, or when nothing is focused and no text selection lives outside.

There is one counterexample, and it is the gesture whose press does not land on `.diagram-canvas`. ADR 0018 lists the press-time focus transfer among four decisions that "derive from the role alone", and `MinimapPan` is the member with no role — entered directly, never classified. The minimap is canvas-rendered chrome, a sibling of `.diagram-canvas` inside `.diagram-container`, and ADR 0026 gives it no tab stop and marks it `aria-hidden`, so it cannot hold focus itself.

The state that bites is an embedded canvas with a host input beside it. Type in the host's field, then drag the minimap. Focus is on the host input, outside `.diagram-container`, the guard fails, and Escape never arrives. It only bites when the press suppresses the browser's own focus change, which is `preventDefault` — the same suppression ADR 0018 already compensates for with an explicit transfer.

**The four synchronous decisions follow the press, not the role**: role-derived for a classified press, fixed for the minimap. This makes an existing exception honest rather than adding one, since ADR 0018 already gives the minimap `setPointerCapture` so it inherits release guarantees, which the role-derived framing does not explain either.

Declaring `MinimapPan` uncancellable was the alternative and is defensible on its own terms, since cancel restores no viewport and buys only stopping the pan a moment early. What is wrong is not the minimap but ADR 0026's guard resting on a claim with a counterexample. The next chrome gesture inherits the bug rather than the exception, and the fog already names a candidate: the bidirectional numeric scrub, described there as a ninth pointer gesture on chrome that ADR 0018's closed set does not contain.

## Cancel is silent, and the cursor is less blocked than the map has been assuming

Two states show nothing. Cancelling a live `Pan` moves nothing back and then swallows further movement until release, so a user who hits Escape and keeps dragging sees a canvas that looks frozen and then works again. Cancelling from `pointing` shows nothing at any point, which is correct and indistinguishable from Escape having done nothing.

This is accepted and handed to the **Cursor and micro-feedback vocabulary** fog patch, which is already collecting cases of this shape — `Ctrl`'s silent suppression, `Axis lock`, the pin-versus-auto drop.

One correction goes with it. That patch has been arguing most of its mid-gesture cases are unreachable because pointer capture freezes the cursor for the duration of a gesture. True, and the conclusion drawn is too strong: the cursor showing mid-gesture **is the capture element's own**, and ADR 0018 puts capture on a stable element the library controls. Restyling that element mid-gesture is the one feedback channel that stays open while a gesture is live, and a cancelled gesture is the first case with a concrete reason to use it. Three of the patch's dependants move from proven-unreachable back to undecided.

Making `Pan` and `MinimapPan` immune to Escape would remove the frozen state, and creates a worse one: Escape would fall to the selection rung mid-pan and clear the selection as a surprise. "Stop panning" is a coherent thing for Escape to mean, and a user who presses it and keeps dragging has contradicted themselves, which is rarer than the mistake that swap would introduce.

## What this amends

**ADR 0018 in three places.** `OnCancel` leaves the gesture interface. The sentence citing cancellation as the reason for polymorphic gesture objects is corrected — the decision stands on `OnMove` and `OnRelease`. And the four synchronous press-time decisions are restated as following the press rather than the role, which gives `MinimapPan` the focus transfer it was missing. The phase model gains `cancelled`.

**ADR 0026 in one place.** "A live gesture implies focus inside the container" gains its counterexample and is repaired by the amendment above rather than by a new clause on the guard.

**ADR 0025 in one place.** The `[Theory]` over the closed eight gains a cancel case per member.

**ADR 0009's Escape row, again.** ADR 0018 made it two rungs; it is three, the middle being ADR 0027's keyboard connector pick.

## What this confirms

**ADR 0020 throughout, and its handed-on question is answered as it expected.** Geometry revert is discarding the preview; the residue it named is the selection, and it needed the snapshot it predicted. Its nomination of cancel as the likely answer for the committed-state-under-a-live-gesture ticket still stands and now inherits a fuller definition, since cancel also restores selection.

**ADR 0022's rejection of a secondary press as a cancel.** Escape remains the only cancel input, and this decision gives it more to do rather than less.

**ADR 0007.** Selection stays out of history; this puts nothing new in it. Cancel is zero entries because of where state lives, not because of a rule anyone follows.

## Considered and rejected

- **Restoring the viewport on cancel**, for uniformity. Buys a spring-back animation and makes Escape the only thing in the product that moves the camera, to undo something the user can undo by dragging.
- **Leaving the press-time selection collapse standing**, on the grounds that the user did point at that shape deliberately. Cheap to be wrong about in one direction and expensive in the other, and it splits Escape's meaning across the threshold.
- **Snapshotting only the gestures that visibly change the selection.** A rule about who needs one is a rule the ninth gesture forgets, and the thing it saves is one set copy per press.
- **Releasing capture on cancel.** Sends the eventual `pointerup` to whatever is under the cursor, so cancelling a drag can end in a click somewhere else.
- **A second Escape falling through to clear the selection.** Turns an innocent double-tap into the exact loss the snapshot exists to prevent.
- **Committing an interrupted gesture** rather than cancelling it. Writes history at a coordinate nobody chose, off screen, needing a `Ctrl`+`Z` the user must first notice.
- **Leaving the interruption channel unwatched**, which is today's behaviour and is leak path seven.
- **Keeping an empty `OnCancel`** as a hook for a gesture that might one day carry residue, and to keep the cancel path reachable from ADR 0025's fake-context drive point. Rejected because cancel is not gesture logic, and a method can be added the day something needs one.
- **Declaring `MinimapPan` uncancellable.** Coherent, since cancel restores nothing for it, and it leaves ADR 0026's guard resting on a false claim for the next chrome gesture to inherit.
- **Making `Pan` immune to Escape** to remove the frozen-canvas state. Trades a rare confusion for a surprise selection wipe mid-pan.
- **Feedback that a cancel happened**, built here. It belongs with the rest of the micro-feedback vocabulary, which this decision hands a mechanism rather than another dependant.
