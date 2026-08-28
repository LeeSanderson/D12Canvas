# One pointer gesture owns each press from capture to release, and the canvas owns them all

Every pointer press on the board resolves to exactly one **pointer gesture**, chosen at press from the hit target ADR 0017 classifies. That gesture holds the pointer until release and its identity never changes mid-press — only its phase, `pointing → active`. Gestures are objects with `OnMove`/`OnRelease`/`OnCancel`, held one at a time by `DiagramCanvas`, which owns all of them; `ComponentContainer` keeps no gesture state whatsoever. Release is guaranteed by pointer capture on a stable element, with `lostpointercapture` as a loud net.

This exists because arbitration today is an emergent property of four separate mechanisms interacting: `stopPropagation` directives in markup, the bubble ordering between an affordance and the container beneath it, nine boolean flags read in a hand-tuned order across two components, and a parallel `click`/`dblclick` dispatch layer refereed against the first three by a `_dragMoved` flag. Correctness requires reading all four together, which is why `ComponentContainer.HandleMouseDown` carries explanatory comments at all.

## Why an owner rather than better flags

Ticket 04 reproduced all six canvas gestures leaking in a real browser, and the six failures are one failure: **a gesture whose termination depends on where the pointer happens to be at release.** Every gesture terminates through an `@onmouseup` on one element, so releasing anywhere else leaves the flag set. Release over any `ComponentContainer` leaks the marquee *without leaving the canvas*, because that container's own `@onmouseup:stopPropagation` swallows it.

The headline path needed no unusual release location at all: `HandleMouseDown` awaits `getContainerDimensions` before setting `_isPanning`, so a fast enough click has its `mouseup` processed first and the flag is set on a gesture already over. An ordinary click on empty canvas leaves the canvas panning, invisibly, until the pointer next moves. That is unreachable by static reading — it is an ordering hazard, not a missing listener, and no rearrangement of flags fixes a class of bug whose cause is that ownership is established asynchronously.

A single owner makes the failure mode expressible. "Who owns this pointer?" has one answer at all times, and the answer becoming stale is a *detectable event* rather than silent state corruption.

## The closed set of eight

| Pointer gesture | Claimed from role |
|---|---|
| `Pan` | `canvas` |
| `MarqueeSelect` | `canvas` |
| `MoveSelection` | `instance`, `selection-bounds` |
| `ResizeSelection` | `resize-handle`, `selection-handle` |
| `DragEdgeEnd` | `port`, `port-strip`, `edge-endpoint` |
| `SelectEdge` | `edge`, `edge-label` |
| `Native` | `author-content` |
| `MinimapPan` | — (chrome, entered directly, never classified) |

Eight owners against nine flags and eleven roles. Which button or modifier picks between `Pan` and `MarqueeSelect` on the same `canvas` role is the right-button and press-to-drag decision's; this ADR fixes only that they are two owners and that exactly one is chosen synchronously at press.

`DragEdgeEnd` unifies drawing a connector with repositioning an endpoint because that unification is already the status quo rather than a change: `StartPortDrag` and `StartFloatingEndpointDrag` set the same `_isConnectingPort` and both complete through `CompletePortDrag`, and pressing a port that already anchors an edge already starts a reposition.

Two members earn their place by being degenerate:

**`SelectEdge` has no active phase.** Release selects; there is nothing to promote to. Including it rather than treating an edge press as unowned keeps the invariant total — every press has an owner, so capture and release always have something to belong to. Edges gain a drag later by giving an existing owner an active phase, not by changing the model. **Crossing the threshold under a pointing-only owner abandons the gesture**: press an edge, drag 200px, release, nothing happens — which is how a native button behaves, and beats selecting on a release far from what was pressed.

**`Native` is a member, not an absence.** A press on author content resolves to an owner whose behaviour is to relinquish: no capture, no `preventDefault`, nothing tracked. Naming it means the classifier's output maps totally onto the owner set with no role falling off the end into an implicit else-branch.

## Commit at press, promote at threshold

A press on an already-selected instance can end as *select this one* or *move the selection*. The press commits to `MoveSelection` immediately, in the `pointing` phase; crossing the movement threshold promotes it to `active`; releasing from `pointing` runs the click outcome instead.

The alternative — recording candidates and arbitrating at the threshold — leaves a window from press to threshold in which nothing owns the pointer. There is then nothing to guarantee the release *of*, capture must be taken by something that is not yet a gesture, and a cancellation arriving in that window has no owner to ask. Committing at press makes ownership coextensive with the press, so the answer to "who owns this pointer?" is never *nobody yet*.

This is also what tldraw does with the phase named as a state: `Idle → PointingShape → Translating`, where `PointingShape` is entered at press and has two exits. And it matches ADR 0017's grain — classification already happens once, at press, synchronously. Arbitration that waited would have to stash the classification and re-consult it at a later event, which is the shape `wasPortDragging` has today.

The consequence worth stating: **the press-time decision must be complete.** Everything needed to choose the owner is available synchronously at press, so no owner selection may depend on an interop round trip. That is the direct fix for the `getContainerDimensions`-before-`_isPanning` hazard.

## Ownership lives on the canvas, never in a container

`ComponentContainer` holds zero gesture state. It renders, it declares what is hittable, and it never learns that a drag is happening.

The container currently owns three gestures and half of a fourth, and that is *why* the worst leak is the worst: a dragged instance released past the `overflow: hidden` clip edge both leaks and never fires `OnMoved`, so the move never enters history and Ctrl+Z afterwards deletes the instance. **A container is a box the pointer can leave; a gesture outlives the box.** Any protocol that splits ownership has to hand it across that boundary mid-gesture, which is the bubble hop `_isPortDragging` exists to survive, generalised into the design.

ADR 0017 removed the reason the split existed. Classification resolves in JS at the canvas level and yields an `entityId`, so the container is no longer the only thing that knows it was hit. Keeping gesture state there afterwards would be keeping the workaround after removing its cause.

Two consequences, both taken deliberately:

**`_isMoving`/`_isGroupMoving` and `_isResizing`/`_isGroupResizing` are the same two gestures implemented twice**, once for n=1 and once for n-many, with separate commit paths. They collapse into one mover and one resizer over the selection, because the canvas is the only thing that knows what the selection is. That is the direction ADR 0006's multi-selection-as-single-unit already pointed, and the shape the align and distribute work found again from its own side.

**The legacy `_editMode` gesture pair is deleted.** `_isDragging`/`_isResizing` predate the Board-backed canvas and are reachable only from `/componentcontainer-demo`, a nav-linked page with no visual-test coverage of its own. It is a rival implementation of two gestures this ADR specifies properly. What remains of `ComponentContainer`'s edit mode once it owns no gestures — `_editMode` still gates `ShowSelectionOverlay` and the `edit-mode`/`view-mode` classes — is its own question and not settled here.

## Gestures are objects, not a switch

Each kind is a small class implementing `OnMove` / `OnRelease` / `OnCancel`, constructed at press with a context that gives it the board, the selection, the `ZoomPanTracker` and the commit path. `DiagramCanvas` holds one nullable field and forwards three events without knowing which kind it holds. Instantiated per canvas on the `ZoomPanTracker` precedent — not DI, since two canvases on a page need two, and not a cascaded value, since containers no longer participate.

Data-plus-a-switch would beat nine flags read in order, and would be hard to get wrong. But it keeps the property that makes the current code fragile: **behaviour for one gesture spread across three methods, each of which knows every gesture.** Every downstream interaction decision adds a gesture or changes one, and each would touch all three switches. Under objects, `DiagramCanvas`'s pointer surface is three forwarding calls and "what does a marquee do" is one file.

The decisive gain is that **cancellation is a method you cannot forget to implement.** A gesture that omits its own release does not compile. That is the strongest available structural guarantee that ticket 04's leaks cannot return by accretion.

The cost is the context seam, and it is taken explicitly: gestures receive a purpose-built context rather than a back-reference to `DiagramCanvas`. A back-reference would make "narrow interface" a lie, since it is reach into everything. With an explicit context, a new gesture needing something the context does not offer is a visible decision rather than a silent `_canvas.Whatever`.

## The synchronous half is JavaScript's, and it is bounded by the role

Four things must happen before the interop hop, and every one of them derives from the **role alone** (ADR 0022 adds a fifth, on a different event, see the addendum):

- **`preventDefault`** — not if `author-content`, otherwise yes. Suppresses text selection during a drag and native drag initiation.
- **`setPointerCapture`** — same predicate.
- **Focus transfer** — see below.
- **The movement threshold.**

Because all four read only the role, **nothing about selection state, modifiers or gesture kind is mirrored into JS.** That satisfies the constraint banked while prototyping wheel input — every input to a synchronous decision must be a fact the listener already holds — at a cost of one field. The **kind is therefore chosen in C#**, on receipt, because the mapping needs the selection.

**JS owns the threshold, and does not call C# until it is crossed.** Promotion is implicit: the first `OnPointerMoved` *means* promoted, so a press that never moves far enough produces exactly two calls. This gives C# an invariant worth having — any `OnPointerMoved` received is a real drag — where the alternative has every gesture's `OnMove` begin with a threshold check, or forget to. The threshold cannot then vary per gesture kind, which is the only thing C#-side ownership would buy; no reference tool varies it that way, and the dimension they *do* vary it by is `pointerType`, which is JS's own datum and ADR 0017's stated reason for choosing Pointer Events.

**The delta measures from the press point, not the promotion point.** At promotion the content jumps by the threshold distance, as it does in tldraw and Excalidraw, and in exchange `final delta = release point − press point` holds exactly. Measuring from the promotion point would make content trail the cursor by the threshold for the remainder of the drag and would break that identity.

**Four `[JSInvokable]` methods** — `OnPointerPressed`, `OnPointerMoved`, `OnPointerReleased`, `OnPointerCancelled` — deliberately not the keyboard listener's pattern of one method per meaning. That listener fans out into 21 methods because each key carries a distinct intent; the pointer path has four events carrying data.

**One reusable listener, two configurations.** `addPointerListener(element, dotnetRef, { classify })`. With `classify: true` on `.diagram-canvas` it walks up to the nearest marked element per ADR 0017. With `classify: false` on the minimap root it skips the walk and reports a fixed role. Both capture, both `preventDefault`, both send the same four events — so `MinimapPan` inherits release guarantees, cancellation and pointer identity rather than hand-rolling them, and ADR 0015's minimap becomes the first test of arbitration over chrome instead of leak path number seven.

**Entry is `internal` and there is no public observation surface.** `ZoomPanTracker` is public because ADR 0015 gave hosts viewport commands to call; nothing asks a host to inspect or drive a pointer gesture, and ADR 0002's chrome components share the assembly. Adding public API later is additive; exposing a live gesture object now would commit to its shape before the live-geometry and cancellation decisions that are most likely to change it.

## Release is guaranteed by capture, and the net is loud

- **`pointerup`** — the gesture commits.
- **`pointercancel`** — the gesture reverts. What "revert" means per gesture belongs to the cancellation decision; this fixes only that the channel exists and is distinct from commit.
- **`lostpointercapture` with a gesture still active** — revert, loudly. This should never fire.

Capture is preferred over window-level listeners precisely for that third path: capture is released implicitly when the element leaves the document, so even the case nobody anticipated arrives as an event rather than as a stuck flag. On a normal release the order is `pointerup` then `lostpointercapture`, so the net finds an already-cleared gesture. A net that finds a live one is by definition a bug — that condition *is* ticket 04's six leaks, converted from invisible state corruption into a single observable event.

**Capture goes on the stable element, not the hit element.** Capture could sit on whatever the press landed on and events would still bubble to the listener, but affordances are exactly the elements most likely to be re-rendered away mid-gesture: `ShowSelectionOverlay` gates the resize handles and port strip on `(IsSelected && !IsMultiSelected) || _editMode`, so a selection change during a resize removes the handle being dragged. `.diagram-canvas` outlives every gesture. This is also why today's leaks are so varied — ownership is currently anchored to transient elements.

**`Native` takes no capture, and that is consistent rather than an exception.** Capture on `.diagram-canvas` would retarget pointer events away from an author's `<input>` and break text selection inside it. So the one gesture with no release guarantee is the one gesture holding no state: every gesture that can leak is captured, and the one that is not, cannot.

**Ownership is keyed by `pointerId`** (widened to `pointerId` *and button* by ADR 0022, see the addendum), and events from any other pointer are dropped while a gesture is live. One field, worth having with touch permanently off the table — a second button pressed mid-drag or a stylus touching during a mouse drag currently walks straight into the same flags.

**Escape routes to the active gesture's cancel before falling through.** Today `OnEscapePressed` clears only the connector drag, a table entry naming one specific gesture because there is no owner to name instead. It becomes "cancel the active pointer gesture, otherwise clear the selection" — the same behaviour, generalised.

## Click and double-click dispatch from the same classification

The classifier reports the press count from `event.detail` alongside the role, and click and double-click actions dispatch off that. **Every `@onclick` and `@ondblclick` binding on board content is deleted** — three double-click meanings (`SwitchToEditMode`, `AddCustomPort`, `AddEdgeLabel`) currently kept apart by their own `stopPropagation` flags, and three click bindings refereed against the mousedown layer by `_dragMoved`.

Leaving that layer alone would make arbitration authoritative for drags and not for clicks — a single owner for one half of the pointer's meaning, with the other half smeared across markup and separated by propagation flags rather than by precedence declared in one place, which ADR 0017 already refused for hit precedence.

**`_dragMoved` disappears rather than being ported.** "Was this a click?" becomes "did the gesture release from the `pointing` phase?", which the gesture already knows, because that is what a phase is. The flag is not replaced with a better flag; the question stops needing a separate answer.

This also removes a compatibility risk rather than inheriting it. `preventDefault` on `pointerdown` suppresses the compatibility mouse events; `click` should still fire, but whether `dblclick` survives uniformly across engines is not something to assume — and all three of today's double-click features depend on the browser's own `dblclick`. Deriving the press count in our own listener means the question is never asked. Anything that continues to depend on the browser's `dblclick` after `pointerdown` is being prevented needs a probe before it is trusted.

## Composition with what the canvas does not own

**HTML5 drag-and-drop placement composes by construction.** A palette drag starts on the palette — chrome, outside `.diagram-canvas`, so its `pointerdown` never reaches the classifier — and arrives as `dragenter`/`dragover`/`drop`, a different event stream entirely. The two never contend. If a native drag ever begins under a captured gesture, the browser fires `pointercancel`, which already reverts: **DnD interruption is not a special case, it is a cancel.**

**Inline text editing composes via `Native`.** A press on the editor's `<textarea>` classifies `author-content`, takes no capture, does not `preventDefault`, and the canvas never learns of it — the same mechanism that deletes `StickyNote`'s `stopPropagation` workaround.

**Focus is transferred explicitly on every captured press.** This is the one gap the model does not close for free. `preventDefault` suppresses the browser's own focus transfer — that is *why* `ComponentContainer` declares it — and suppressing focus transfer also suppresses **blur**, which is what commits an inline edit or a focused chrome input. Without an explicit transfer, editing a sticky note and then pressing the canvas to pan leaves the textarea unblurred and the edit uncommitted. So JS blurs the active element and focuses the canvas container in the same synchronous window, alongside `preventDefault` and `setPointerCapture`.

Leaving this to ADR 0010's focus-follows-selection was rejected: it works for a press that changes the selection and fails for one that does not, so `Pan` and a re-press on an already-selected instance would both leave stale focus in place. A rule that holds for some gestures is what this model exists to eliminate.

**Two target predicates, deliberately different widths.** ADR 0017 describes the pointer predicate as `isEditableTarget`'s shape widened from keyboard to pointer, and reading that as *share the function* would regress the keyboard. `isEditableTarget` covers `HTMLInputElement`, `HTMLTextAreaElement` and `isContentEditable` and nothing else; `author-content` adds `<button>`, `<select>`, `<a href>`, `[tabindex]`. The keyboard listener depends on its guard **not** matching buttons, because `Enter` on a focused palette entry must reach the browser's native activation. The two live adjacent in the same file with the reason recorded: the keyboard guard exists to avoid stealing *typing*, so it covers text entry; the pointer guard exists to avoid stealing *focus and activation*, so it covers everything natively interactive.

## Author content selects its instance, additively

A press classified `author-content` selects the enclosing instance **if the selection does not already contain it, and never removes anything from the selection.**

Selecting nothing would mean an author whose component is mostly an `<input>` or a `<button>` gives the user no way to move or restyle it without hunting for a non-interactive strip — the same "shapes are hard to drag" complaint arriving by another route. Always collapsing the selection to that one instance would throw away a multi-selection with no warning, and would break a cross-type multi-selection edit the moment a control inside one member is touched. Additive-only is safe in both directions: the first press selects the instance *and* focuses the control, since nothing is prevented and both can happen; later presses only interact.

**This is the one decision in the press path allowed to be asynchronous.** It happens in C# after the interop hop, and nothing depends on the synchronous window — worth stating so that "everything synchronous lives in JS" is not read as "everything lives in JS". Because `Native` is not captured there is no release to hook: the selection change happens on press, and that is the whole gesture.

## Touch is not built, and staying open costs two things

The non-foreclosure constraint is satisfied by exactly two commitments, and nothing else is done:

1. **Ownership keyed by `pointerId`** (above), which makes pinch a matter of a second pointer *joining* the active gesture rather than a restructuring of who owns what.
2. **The press-to-kind mapping takes `pointerType` as an input**, so a touch table can exist later without the arbiter changing shape.

The stronger-sounding constraint — that no gesture may be reachable only via a non-primary button or modifier — is deliberately **not** adopted. It would quietly bind the right-button decision: if left-drag on empty canvas becomes marquee and pan moves to right, middle or space, pan has no plain-primary route, which on touch is exactly backwards, since one-finger drag should pan and marquee is the gesture that needs a mode. Admitting two tables costs nothing and leaves the mouse table free.

Explicitly out:

- **Hover is outside this model** — no press, no owner. Whatever the port affordance work does about hover having no touch equivalent is not inherited here.
- **`pointercancel` needs no touch-specific work.** It is already the revert channel; it is merely rare on mouse and common on touch, so the touch path exercises a route already built.
- **`touch-action: none` is not set.** It is the one declaration a touch effort adds. Setting it now would strip a touch user's native page scrolling over the canvas without giving them a working gesture in exchange.

## What this amends

**ADR 0009 is amended, not superseded.** It documents the interaction *surface*; this is the mechanism beneath it. "No persistent tool modes" survives intact and in fact strengthens — a pointer gesture is momentary by construction, since it cannot outlive a press. The one narrow amendment is the **Escape row**: "clear selection, or cancel an in-progress connector drag" becomes "cancel the active pointer gesture, otherwise clear selection." The interaction table's button semantics are the right-button decision's to amend, not this one's.

**ADR 0017 is amended in one place**: the classification carries **press count, `pointerType`, buttons and modifiers** in addition to `(role, entityId, part)`. Click dispatch, the `pointerType`-varied threshold and the kind mapping each need part of that. Recorded explicitly rather than left to a future reader who would find a three-field triple in the ADR and a seven-field payload in the code — this repo has twice been bitten by a summary being stricter than the decision it summarised.

**ADR 0007 is confirmed, not amended.** One history entry per gesture holds and gets easier: commitment happens at release, in one place, inside the owner.

**Neither ADR 0006 nor ADR 0010's semantics are touched.** Selection semantics on press belong to the right-button and press-to-drag decision. ADR 0017's rule that **pointer participation and keyboard reachability are separate properties** binds here too and is restated rather than assumed inherited: no gesture may key keyboard reachability off its own participation.

## Considered and rejected

- **A candidate set arbitrated at the movement threshold** — leaves a press-to-threshold window in which nothing owns the pointer, so capture belongs to something that is not a gesture and a cancellation has no owner to ask.
- **Split ownership between canvas and container** — requires handing ownership across a boundary the pointer can leave mid-gesture, which is `_isPortDragging`'s bubble hop promoted to architecture, and it is the direct cause of the leak where a move never enters history.
- **Keeping the legacy `_editMode` drag/resize pair** — a second implementation of two gestures this ADR specifies, kept alive by one untested demo page.
- **Data plus a switch on gesture kind** — better bookkeeping than nine flags, but keeps each gesture's behaviour spread across three methods that each know every gesture, and cannot make cancellation unforgettable.
- **A back-reference to `DiagramCanvas` as the gesture context** — reach into everything, which makes the narrow interface a claim rather than a fact.
- **A DI-registered arbitration service** — two canvases on one page need two arbiters.
- **A cascaded arbitration value** — only needed if containers participated, and they no longer do.
- **C# owning the movement threshold** — buys a per-kind threshold no reference tool wants, at the cost of an interop hop per sub-threshold pixel and a threshold check at the top of every `OnMove`.
- **Measuring the drag delta from the promotion point** — content trails the cursor by the threshold for the rest of the gesture and `final delta = release − press` stops holding.
- **One `[JSInvokable]` per pointer intent**, mirroring `addKeyboardListener` — the pointer path has four events carrying data, not 21 named intents.
- **Window-level `pointerup` listeners instead of capture** — closes the ordinary leaks but not element-removal, and provides no single event meaning "ownership ended".
- **Capturing on the pressed element** — affordances are precisely the elements re-rendered away mid-gesture.
- **Capturing for `author-content`** — retargets events away from an author's `<input>` and breaks text selection inside it.
- **A public observation surface for the active gesture** — commits to a shape before the live-geometry and cancellation decisions that will change it, and can be added later without a break.
- **Leaving `click`/`dblclick` as a separate dispatch layer** — makes arbitration authoritative for half the pointer's meaning and leaves three double-click meanings separated by propagation flags rather than declared precedence.
- **Relying on the browser's `dblclick` after preventing `pointerdown`** — an untested cross-engine assumption underneath three shipped features.
- **Leaving focus transfer to ADR 0010's focus-follows-selection** — silently fails for every press that does not change the selection.
- **Sharing one target predicate between the keyboard and pointer paths** — widening it to ADR 0017's list would swallow `Enter` on a focused palette entry.
- **Author content selecting nothing** — leaves an author whose component is mostly a control with no route to move or restyle it.
- **Author content always collapsing the selection to its instance** — discards a multi-selection with no warning, and breaks cross-type multi-selection editing on first contact with a control.
- **Requiring every gesture to be reachable by a plain primary press** — sounds like the non-foreclosure constraint but forces the mouse table to double as a touch table, where the correct answers differ.
- **Setting `touch-action: none` now** — removes native scrolling from touch users without giving them a gesture in return.

## Addendum (surfaced while resolving the right-button and press-to-drag ticket)

ADR 0022 discharges the question this ADR deferred: **which button picks between `Pan` and `MarqueeSelect` on the `canvas` role.** A plain primary drag marquees, and pan moves to the secondary and middle buttons. The closed set of eight is unchanged, and so is every other row of the role-to-owner table.

Two amendments to this ADR's own mechanism.

**The synchronous role-derived list gains a fifth member**: suppressing the browser's native context menu. It is the first such decision on an event other than `pointerdown`, classifying from `event.target` on the `contextmenu` event itself rather than consulting the stashed press classification, which would be `wasPortDragging`'s shape. It suppresses everywhere except `author-content`, where `Native`'s existing refusal to `preventDefault` already lets the browser's own text menu through with no mechanism at all. This matters more than tidiness: an unsuppressed native menu opening mid-right-drag fires the `pointercancel` the specification mandates for an opened menu, which reverts the pan it interrupted.

**Ownership keys on the pointer and the button.** Keying on `pointerId` alone covers a stylus touching mid-drag but not a second *button* going down on the same pointer, which is the same `pointerId` and which right-button pan makes an everyday accident. The release side is sharper: `pointerup` fires per button, so pressing primary, pressing secondary, then releasing primary delivers a `pointerup` while a button is still held. A gesture therefore records the button that claimed it and only that button's release ends it; any other button's down or up is dropped and the live gesture keeps running. Dropping rather than treating a secondary press as a cancel, because Escape already routes to the active gesture's cancel and two abort routes give a user no way to know which one they invoked.

Three of this ADR's claims are confirmed by being used rather than merely surviving. **Committing at press with the phase inside the gesture** is what lets the secondary button mean both pan and menu, the menu becoming a `pointing`-phase release outcome with no new mechanism. **JS owning the threshold** is what fixes it at one number, 4 screen pixels, for both buttons. And the rejection of *requiring every gesture to be reachable by a plain primary press* was written against exactly the table ADR 0022 landed on, so the touch story needs nothing beyond inverting the `canvas` row.

One thing this ADR left open stays open by choice: `Shift` gets no in-gesture meaning, and Alt stays unbound on the pointer, so the latched-versus-live modifier question inherits a smaller table than it expected.

## Addendum (surfaced while resolving the create-adjacent-and-connect ticket)

ADR 0030 fills in a `pointing`-phase release outcome this ADR defined and left unassigned, and does so **without adding a member to the closed set of eight**. `Quick create` is `DragEdgeEnd` releasing below the threshold on the `port` or `port-strip` role, so ADR 0025's release-reliability `[Theory]` gains no case and the role-to-owner table, the synchronous decisions and the capture rules are all untouched.

This is the second time the closure has paid off in the way it was meant to. ADR 0022 made the context menu a `pointing`-phase release on the secondary button with no new mechanism; this makes the most-repeated gesture in a diagramming tool one on the primary button, on a role that already had an owner.

**The three double-click meanings become two.** `AddCustomPort` loses its double-click to ADR 0030, leaving `SwitchToEditMode` and `AddEdgeLabel`. The mechanism is unchanged and the reason is this ADR's own: press count comes from `event.detail`, so a press-count-1 outcome commits before a press-count-2 outcome can be known to exist. Three meanings on one target are expressible here only when at most one of them is a click, which is a property of the dispatch this ADR chose rather than a limitation discovered later. Worth writing down, because the table reads as though count is a free discriminator and it is free only in one direction.
