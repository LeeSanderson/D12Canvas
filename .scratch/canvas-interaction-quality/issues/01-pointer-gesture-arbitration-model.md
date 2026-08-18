# Pointer gesture arbitration model

Type: grilling
Status: resolved
Blocked by: 02, 03, 04, 18

## Question

Design the model that decides, for any pointer press on the canvas, which single gesture owns it — and holds that ownership unambiguously until release.

There is no such model today. Arbitration is an emergent property of three separate mechanisms interacting: `@onmousedown:stopPropagation` / `@onmouseup:stopPropagation` directives in markup, the bubble ordering between a port or resize handle and the `ComponentContainer` beneath it, and a set of boolean flags (`_isMoving`, `_isResizing`, `_isPortDragging`, `_isDragging` on `ComponentContainer`; `_isPanning`, `_isMarqueeSelecting`, `_isGroupMoving`, `_isGroupResizing`, `_isConnectingPort` on `DiagramCanvas`) read in a specific order inside each handler. Correctness depends on reading all three together — the comments in `ComponentContainer.HandleMouseDown` exist precisely because the ordering is not self-evident.

This is the spine of the map. Most other tickets here either add a gesture to this space or change how an existing one is triggered, and each would otherwise add another flag and another ordering constraint.

Decide:

- What the unit of arbitration is, and what to call it — `CONTEXT.md` has no term for this today. Where does it live: a service, a cascaded value, a type owned by `DiagramCanvas`, something else?
- How a press is classified into exactly one gesture. A fixed priority table? Hit-region ownership? Negotiation between candidates?
- Where ownership lives once claimed, and how a gesture guarantees it is released — see ticket 04 for the concrete leak paths this must close, which is the strongest single argument for a real owner rather than distributed flags.
- How this composes with gestures the canvas does not own: HTML5 drag-and-drop placement (`@ondrop`), inline text editing, and the `isEditableTarget` guard the keyboard shortcuts already use.
- How the model stays expressible over pointer/touch input later without redesign (map Notes: non-foreclosure constraint). Note that button-number and hover concepts, which ticket 07 leans on heavily, have no touch equivalent.
- Whether this supersedes ADR 0009 outright or amends it. ADR 0009 documents the interaction *surface* (what gestures exist) but never the arbitration *mechanism*.

Ticket 02's findings constrain what the browser actually delivers; ticket 03's teardown supplies the reference bar for which gestures should exist at all; ticket 04 supplies the evidence of how the current arrangement fails.

## Answer

Recorded as **ADR 0018**. One `CONTEXT.md` term added (`Pointer gesture`), two widened (`Gesture`, `Hit target`). Addenda on ADRs 0009 and 0017.

### A fourth mechanism the ticket did not count

The ticket names three interacting mechanisms. There are **four**. Alongside the `stopPropagation` directives, the bubble ordering and the nine flags sits a **parallel `click`/`dblclick` dispatch layer**: `@onclick` to `HandleClick` (container), `HandleCanvasClick` (canvas) and `SelectEdge` (edge); `@ondblclick` to `SwitchToEditMode` (container body), `AddCustomPort` (port strip) and `AddEdgeLabel` (edge) — each with its own `stopPropagation` keeping the three double-click meanings from colliding. `_dragMoved` exists for exactly one purpose: to referee that layer against the mousedown layer, so a click fired after a drag does not clear the selection the drag just established.

Counting it changes the answer, because a model that owns only presses would leave half the pointer's meaning smeared across markup. **The model owns click and double-click too**, dispatching them off the press count (`event.detail`) carried on the same classification, and every `@onclick`/`@ondblclick` binding on board content is deleted.

The prize is that **`_dragMoved` disappears rather than being ported.** "Was this a click?" becomes "did the gesture release from the `pointing` phase?" — which the gesture already knows, because that is what a phase is. And it removes a live risk instead of inheriting one: `preventDefault` on `pointerdown` suppresses the compatibility mouse events, and whether `dblclick` survives that uniformly across engines is an untested assumption sitting under three shipped features. Deriving the count ourselves means the question is never asked. **This dissolves the probe rather than surfacing one** — but the finding stands for anything that *does* keep depending on the browser's `dblclick`.

### The shape: one owner, committed at press, phase inside it

Eight **pointer gestures**, a closed set: `Pan`, `MarqueeSelect`, `MoveSelection`, `ResizeSelection`, `DragEdgeEnd`, `SelectEdge`, `Native`, `MinimapPan`. Against nine flags and eleven roles.

**Committed at press, not at the threshold.** A press on a selected instance claims `MoveSelection` immediately in the `pointing` phase; crossing the threshold promotes it to `active`; releasing from `pointing` runs the click outcome. The candidate-set alternative was rejected on ticket 04's evidence rather than on taste: it leaves a press-to-threshold window in which **nothing owns the pointer**, so there is nothing to guarantee the release *of*, capture belongs to something that is not yet a gesture, and a cancellation in that window has no owner to ask. tldraw names the phase as a state and does the same thing — `Idle → PointingShape → Translating`, entered at press with two exits.

Two members are deliberately degenerate, and both earn it. **`SelectEdge` has no active phase** — release selects, nothing to promote to — which keeps "every press has an owner" total, so capture and release always have something to belong to; edges gain a drag later by giving an existing owner an active phase rather than by changing the model. **`Native` is a member, not an absence** — its behaviour is *relinquish*, so the classifier's eleven roles map totally onto the owner set with no role falling off the end into an implicit else-branch. Rider: **crossing the threshold under a pointing-only owner abandons the gesture**, the way a native button does when you slide off it.

`DragEdgeEnd` unifying connector-draw with endpoint-reposition is **not a change** — `StartPortDrag` and `StartFloatingEndpointDrag` already set the same `_isConnectingPort` and both complete through `CompletePortDrag`, and a press on a port that already anchors an edge already starts a reposition.

### The container goes inert, which is where the worst leak came from

**`ComponentContainer` holds zero gesture state.** It renders, declares what is hittable, and never learns a drag is happening.

This is the single highest-value structural claim here, and ticket 04 supplies the argument. The worst leak — a dragged instance released past the `overflow: hidden` clip edge leaking *and* never firing `OnMoved`, so the move never entered history and Ctrl+Z deleted the instance instead — happens **because the container owns the gesture**. A container is a box the pointer can leave; the gesture outlives the box. Any split-ownership protocol has to hand ownership across that boundary mid-gesture, which is `_isPortDragging`'s bubble hop promoted from workaround to architecture.

ADR 0017 had already removed the *reason* for the split: classification resolves in JS at canvas level and yields an `entityId`, so the container is no longer the only thing that knows it was hit.

Two consequences taken deliberately, neither of which the ticket anticipated:

- **`_isMoving`/`_isGroupMoving` and `_isResizing`/`_isGroupResizing` are the same two gestures implemented twice**, once for n=1 and once for n-many, with separate commit paths. They collapse into one mover and one resizer over the selection, since the canvas is the only thing that knows what the selection is. ADR 0006's multi-selection-as-single-unit already pointed here; ticket 12 found the same shape from the align side.
- **The legacy `_editMode` pair is deleted.** `_isDragging`/`_isResizing` predate the Board-backed canvas and are reachable only from `/componentcontainer-demo` — nav-linked, with **no visual-test coverage of its own**. It is a rival implementation of two gestures this ADR specifies properly. What survives of edit mode (it still gates `ShowSelectionOverlay` and the `edit-mode`/`view-mode` classes) became its own ticket.

### Objects, not a switch

One small class per kind with `OnMove`/`OnRelease`/`OnCancel`, constructed at press against an **explicit context** — board, selection, `ZoomPanTracker`, commit path — held in a per-canvas nullable field on the `ZoomPanTracker` precedent (`private readonly … = new()`, public read-only property, no DI since two canvases need two, no cascaded value since containers no longer participate).

Data-plus-a-switch beats nine flags and is hard to get wrong, but it preserves what actually makes today's code fragile: **one gesture's behaviour spread across three methods, each knowing every gesture.** Every downstream ticket on this map adds or changes a gesture, and each would touch all three switches — the flags unsmeared, the smear relocated to the event methods.

The decisive argument is ticket 22's subject: **cancellation becomes a method you cannot forget to implement.** A gesture that omits its own release does not compile. That is the strongest available guarantee that ticket 04's leaks cannot return by accretion. The cost is the context seam, taken explicitly rather than as a back-reference to `DiagramCanvas` — a back-reference is reach into everything, which would make "narrow interface" a claim rather than a fact.

### What is synchronous is bounded by the role — which is why nothing is mirrored

Four things must happen before the interop hop, and **every one derives from the role alone**: `preventDefault` (not if `author-content`), `setPointerCapture` (same predicate), focus transfer, and the movement threshold. That is the finding that makes ticket 17's mirroring constraint cheap — *no selection state, modifiers or gesture kind is copied into JS*, and the **kind is therefore chosen in C#**, on receipt, because that mapping needs the selection.

**JS owns the threshold and does not call C# until it is crossed**, so promotion is implicit — the first `OnPointerMoved` *means* promoted, and a click costs exactly two hops rather than two plus every pixel of hand tremor. C# gains the invariant that any move it receives is a real drag, where the alternative has every `OnMove` open with a threshold check, or forget to. The threshold cannot then vary per gesture kind, which is all that C#-side ownership would have bought; no reference tool varies it that way, and the dimension they *do* vary it by — `pointerType`, coarse pointers getting a bigger threshold — is JS's own datum and ADR 0017's stated reason for choosing Pointer Events. Ticket 07 still picks the number, or one per `pointerType`.

Rider fixed here: **the delta measures from the press point, not the promotion point.** Content jumps by the threshold at promotion, as in tldraw and Excalidraw, and in exchange `final delta = release − press` holds exactly — which the map's fog needs intact for remembered-offset duplicate chaining. Measuring from promotion makes content trail the cursor forever and breaks that identity.

**Four `[JSInvokable]` methods**, deliberately *not* `addKeyboardListener`'s pattern — that listener fans out into **21** methods because each key is a distinct intent; the pointer path is four events carrying data.

**One reusable listener, two configurations**: `addPointerListener(element, dotnetRef, { classify })`. `classify: true` on `.diagram-canvas`; `classify: false` on the minimap root, reporting a fixed role. Both capture, both `preventDefault`, both send the same four events — so ADR 0015's minimap inherits release guarantees, cancellation and pointer identity instead of hand-rolling a mouse-up and becoming **leak path number seven, written after we had catalogued the first six**. Entry is `internal`; **no public observation surface**, because adding public API later is additive while exposing a live gesture object now would fix its shape before tickets 05 and 22 — the two most likely to change it — have run.

### Release, guaranteed, with a loud net

`pointerup` commits. `pointercancel` reverts (ticket 22 owns what revert *means*; this fixes only that the channel exists and is distinct). **`lostpointercapture` with a gesture still live reverts loudly, and should never fire** — capture is released implicitly when the element leaves the document, so even the unanticipated case arrives as an event rather than a stuck flag. That condition *is* ticket 04's six leaks, converted from invisible state corruption into one observable event.

**Capture goes on the stable element, not the hit element.** Non-obvious, and load-bearing: affordances are precisely the elements most likely to be re-rendered away mid-gesture, since `ShowSelectionOverlay` gates the resize handles and port strip on `(IsSelected && !IsMultiSelected) || _editMode` — so a selection change during a resize removes the handle being dragged. `.diagram-canvas` outlives every gesture. This is also *why* today's leaks are so varied: ownership is currently anchored to transient elements.

**`Native` takes no capture, and that is consistent rather than an exception** — capture on `.diagram-canvas` would retarget events away from an author's `<input>` and break text selection inside it. The one gesture with no release guarantee is the one holding no state: every gesture that can leak is captured, and the one that is not, cannot.

**Ownership keyed by `pointerId`**, other pointers dropped — worth having with touch permanently off the table, since a second button mid-drag or a stylus touching during a mouse drag currently walks straight into the same flags.

**Escape routes to the active gesture's cancel before falling through.** `OnEscapePressed` clearing only the connector drag is a shortcut table naming one specific gesture because there was no owner to name instead.

### Composition: two free, one real gap

**HTML5 drag-and-drop never contends.** A palette drag starts on chrome, outside `.diagram-canvas`, so its `pointerdown` never reaches the classifier, and it arrives as a different event stream. If a native drag ever begins under a captured gesture the browser fires `pointercancel` — so **DnD interruption is not a special case, it is a cancel.** No rule needed.

**Inline text editing composes via `Native`** — the same mechanism that deletes `StickyNote`'s `stopPropagation` workaround.

**The gap: `preventDefault` suppresses blur, and blur is what commits an edit.** `preventDefault` on a press suppresses the browser's focus transfer — that is *why* `ComponentContainer` declares it — so as designed, editing a sticky note and then pressing the canvas to pan would leave the textarea unblurred and the edit uncommitted (`CONTEXT.md`: *"a prop edit committed on blur"*). Fixed by **JS transferring focus explicitly on every captured press** — blur the active element, focus the canvas container — in the same synchronous window, using the `focusElement` that already exists there. Leaving it to ADR 0010's focus-follows-selection was rejected because it works for a press that changes the selection and fails for one that does not, so `Pan` and a re-press on an already-selected instance both strand stale focus. That is now two focus writes per press with an interop hop between them, which became its own ticket.

**Two target predicates, deliberately different widths — do not share the function.** ADR 0017 describes the pointer predicate as *"`isEditableTarget`'s existing shape, widened from keyboard to pointer"*, and reading that as *share it* regresses the keyboard: `isEditableTarget` covers `HTMLInputElement`, `HTMLTextAreaElement` and `isContentEditable` **and nothing else**, while `author-content` adds `<button>`, `<select>`, `<a href>`, `[tabindex]`. The comment at `DiagramCanvas.razor.js:79` spells out the dependency — `Enter` on a focused palette button must reach native activation, which requires the keyboard guard *not* to match buttons. Two functions, adjacent, with the reason recorded: the keyboard guard avoids stealing **typing**, the pointer guard avoids stealing **focus and activation**.

### Author content selects additively

The question ADR 0017 explicitly deferred here. A press classified `author-content` **selects the enclosing instance if the selection does not already contain it, and never removes anything from the selection.**

Selecting nothing leaves an author whose component is mostly an `<input>` or `<button>` with no route to move or restyle it without hunting for a non-interactive strip — ticket 07's "shapes are hard to drag" complaint arriving by another road. Always collapsing to that instance discards a multi-selection with no warning and breaks a cross-type multi-selection edit on first contact with a control. Additive-only is safe both ways: the first press selects *and* focuses, since nothing is prevented and both can happen.

**This is the one decision in the press path allowed to be asynchronous** — it lands in C# after the hop and nothing depends on the synchronous window. Stated so that "everything synchronous lives in JS" is not read as "everything lives in JS". `Native` being uncaptured, there is no release to hook: the selection change happens on press and that is the whole gesture.

### Touch: two commitments, and a constraint deliberately not adopted

Non-foreclosure costs **ownership keyed by `pointerId`** (so pinch is a second pointer *joining* an existing gesture, not a restructuring of ownership) and **`pointerType` as an input to the press-to-kind mapping** (so a touch table can exist without the arbiter changing shape). Nothing else.

The stronger reading — *no gesture reachable only via a non-primary button or modifier* — is **rejected**, because it would quietly bind ticket 07: if left-drag on empty canvas becomes marquee and pan moves to right/middle/space, pan has no plain-primary route, which on touch is exactly backwards (one-finger drag should pan; marquee is the gesture needing a mode). Admitting two tables costs nothing and leaves ticket 07's mouse table free.

Explicitly out: **hover is outside this model** (no press, no owner — ticket 06 keeps that problem); **`pointercancel` needs no touch work**, being already the revert channel and merely rare on mouse; **`touch-action: none` is not set**, as it would strip a touch user's native page scrolling without giving them a working gesture in exchange. One declaration, when a touch effort exists.

### ADR disposition

**New ADR 0018, superseding nothing.** ADR 0009 documents the interaction *surface*; this is the mechanism beneath it, and "no persistent tool modes" is *strengthened* — a pointer gesture cannot outlive a press, so it holds by construction rather than convention. **ADR 0009 amended in one narrow place**, the Escape row. **ADR 0017 amended in one place**: the classification carries press count, `pointerType`, buttons and modifiers as well as `(role, entityId, part)` — declared rather than smuggled, because this repo has twice had a *summary* read stricter than its decision (ticket 14 on ADR 0012, ticket 18 on ADR 0011), and a three-field triple in the ADR against a seven-field payload in the code is a third of the same. **ADR 0007 confirmed, not amended** — commitment at release, in one place, in the owner. **ADR 0006 and ADR 0010's semantics untouched**; ADR 0017's rule that pointer participation and keyboard reachability are separate properties is restated rather than assumed inherited.

### Unblocks and surfaces

Unblocks [Live gesture geometry](05-live-gesture-geometry.md), [Right-button semantics and press-to-drag](07-right-button-and-press-to-drag-semantics.md) and [Gesture cancellation and revert semantics](22-gesture-cancellation-semantics.md).

Handed to ticket 05: the **gesture context's membership** is 05's to fix, since what a gesture may reach is the same question as where in-flight geometry lives. Handed to ticket 07: the **threshold number**, and the button/modifier mapping between `Pan` and `MarqueeSelect`.

Surfaced [What remains of `ComponentContainer`'s edit mode](26-component-container-edit-mode-remnant.md) and [Press-time focus transfer versus focus-follows-selection](27-press-focus-transfer.md). Graduated from the fog: [Edges in a multi-selection](28-edges-in-multi-selection.md), whose two blockers (01, 18) are now both resolved, and [Latched-versus-live modifier semantics](29-latched-versus-live-modifiers.md), which the fog recorded as unphraseable before this model existed.

One incidental defect found while reading, not owned here: **`RefreshConnectContainerOrigin` is fire-and-forget *after* `_isConnectingPort` is set**, so `_connectContainerOrigin` is stale for the first frames of every connector drag. Same family as ticket 04's hazard with the opposite ordering — the flag wins the race instead of losing it, so it mis-positions rather than leaks.
