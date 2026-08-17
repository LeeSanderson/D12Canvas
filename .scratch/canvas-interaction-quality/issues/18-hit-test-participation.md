# Hit-test participation and hit-region geometry

Type: grilling
Status: resolved

## Question

Decide what participates in pointer hit-testing, in what precedence, and how a target's hit region is decoupled from the thing you can see.

Ticket 03's teardown found this is a first-class concept in all four reference tools and does not exist in D12Canvas at all — an arbitration model that classifies a press has to hit-test first, and where that hit-testing lives is the same question. Ticket 01 arbitrates *after* a hit is resolved, so it needs this seam to exist before it can decide what a press means.

The evidence is concrete rather than theoretical. `.edge-line` is `stroke-width: 2` with `pointer-events: auto` and no widened hit path — and because that width is in board units, an edge is ~0.2 screen pixels at 0.1× zoom, so selecting one at anything but high zoom is luck. ADR 0011 made zoom unbounded in both directions, which turns that from an annoyance into a guarantee of failure.

Decide:

- **Whether hit regions are a separate geometry from rendered geometry**, and where that lives. Excalidraw's `hasBoundingBox` deliberately governs the affordance *and* the hit test from one place so the two cannot drift; edges in every reference tool have a hit stroke wider than their visual stroke.
- **Precedence between overlapping participants.** tldraw hit-tests overlays before shapes and writes its vertex-beats-virtual rule down explicitly rather than inheriting it from paint order, which is exactly what D12Canvas does today via markup order and the stacking tie-break.
- **Whether anything is drawn but not hittable, or hittable but not drawn** — and whether a target too small to hit should be dropped from the set entirely rather than rendered at an unusable size.
- **Whether entities can be removed from pointer hit-testing while staying keyboard- and panel-reachable.** All three tools that document locking implement it exactly this way, so if this seam exists, locking is nearly free — decide whether to take it here or leave it as a later feature built on the seam.
- **How the seam is expressed so ticket 01 can consume it**: does a hit produce a target, a ranked list of candidates, or a classification?

Scope boundaries, so this does not re-litigate its neighbours: the *sizing* of ports and resize handles specifically stays in ticket 06, which already owns hit-target sizing under zoom. The press-becomes-drag movement threshold stays in ticket 07. This ticket owns what is hittable and how the region is derived; those two own what happens once something is hit.

## Answer

Recorded as **ADR 0017**. Three `CONTEXT.md` terms added (`Hit target`, `Hit region`, `Locked`), two corrected (`LOD placeholder`, `Command`).

### The fork the ticket did not name, and the fact that decides it

**Blazor's `MouseEventArgs` carries no event target.** That single omission is the root of everything ticket 01 is trying to clean up: the only component that knows what was hit is the one that was hit, so arbitration is *necessarily* distributed across `stopPropagation` directives and nine boolean flags. There is no tidier arrangement of per-element handlers that produces "classify once, at press" — the information is not there.

So the press is resolved in **JS**, on a `pointerdown` listener on `.diagram-canvas`, walking up from `event.target` to the nearest marked element and handing C# a `(role, entityId, part)` classification. C# never touches the DOM; JS never asks C# a question.

Geometry in C# was the tempting alternative and it is a trap: the library knows only `Bounds`, a rect, so it goes **blind inside an author's component** — a sticky note mid-edit, a built-in `Image`, an author's `<button>` all read as "instance body". And because `.container-content` is `overflow: hidden`, the rect *is already* the true region for an instance, so geometry pays that price and buys nothing back. Walking up from `event.target` instead inherits the browser's own answer — `pointer-events`, stacking contexts, the `scale()` transform, `overflow` clipping — for free and *consistently with what a native click would have done*. `elementsFromPoint` was rejected for the opposite reason: it is a separate query that can disagree with actual dispatch, leaving two hit tests to keep in agreement by hand.

Not a new pattern here — `addKeyboardListener` already classifies by DOM target through `isEditableTarget`/`isComponentContainerTarget`, and ticket 02 is independently moving the wheel listener into the same file.

**A single target, not a ranked list**, because a list plus a C#-side precedence table creates *two* precedence systems — the DOM's, which the user can see, and the table's, which they cannot — and where they disagree you click the thing on top and something underneath wins. And **not an element identity**, because ticket 17 already banked that the `preventDefault` policy must be derived **synchronously in JS**; `setPointerCapture` is the same. That is ticket 04's headline hazard exactly — `HandleMouseDown` awaiting `getContainerDimensions` before setting `_isPanning`, letting a fast click's `mouseup` overtake it.

`pointerdown` rather than `mousedown` is deliberate and already earns itself: `pointerType` is what device-varied sizing consumes, and `setPointerCapture` is what ticket 04's six leaks need.

### Eleven roles, and the hidden participant

`instance`, `resize-handle`, `port`, `port-strip`, `edge`, `edge-endpoint`, `edge-label`, `selection-bounds`, `selection-handle`, `author-content`, `canvas`. Non-participants stay `pointer-events: none`: `.group-tab-stop`, `.marquee-select`, `.drag-over-affordance`, `.grid-layer`, `.grid-backdrop`.

**`.selection-bounding-box` is `pointer-events: none` while its interior is a live hit region** — implemented geometrically as `PointIsWithinSelectionBounds` inside `HandleMouseDown`. It becomes a real element (transparent fill, `pointer-events: auto`) and that method is deleted. Keeping it geometric would mean C# running a second, different hit test *after* receiving JS's classification, so a press classified `canvas` would sometimes secretly be `selection-bounds` and "classify once" would be false on day one. It also does not work as intended today: the geometric test only runs once a press has already fallen through to bare canvas, so a press inside the box but over an *unselected* instance behaves differently from the same press over empty space — by accident, not design. Accepted cost: the box now eats presses aimed at what sits under it, which is correct per tldraw's `selectionBounds?.containsPoint` branch but is a behaviour change ticket 15 wants a test shape for.

### Author content claims its own presses, which deletes two workarounds

`ComponentContainer` declares `@onmousedown:preventDefault="true"`, which suppresses focus — so **`StickyNote`'s `<textarea>` carries `@onmousedown:stopPropagation` purely to survive its own container.** The library's own built-in is the proof that an arbitrary author's `<input>` simply will not focus, which is invisible to anyone writing against ADR 0001.

A press is `author-content` when it lands on a natively-interactive element (`<input>`, `<textarea>`, `<button>`, `<select>`, `<a href>`, `[contenteditable]`, `[tabindex]`) — `isEditableTarget`'s existing shape widened from keyboard to pointer — with an explicit marker as the escape hatch for an author's plain `<div>` with its own handler. The synchronous `preventDefault` decision then becomes one line, and **the blanket `preventDefault` and `StickyNote`'s workaround both go**. A model that removes two workarounds rather than adding a concept is the right one.

Boundary held: this says only *"the press hit author content, inside instance X."* Whether that also **selects** X is ticket 01's, not this ticket's.

### Chrome is excluded by where the listener binds

No chrome component has anything to classify — ADR 0015 settled the minimap as navigation only, one behaviour regardless of what is under the pointer; the rest are native buttons, HTML5 DnD and form controls. What ticket 13 actually needs from ticket 01 is **capture and single-release**, which is separable from hit classification, so that sentence is satisfied without chrome entering this seam.

Binding to `.diagram-canvas` rather than `.diagram-container` enforces it structurally: `SelectionContextMenu` and `.drag-over-affordance` are *siblings* of `.diagram-canvas`, so a press on either bubbles container-ward and never reaches the listener; host-placed chrome is outside `.diagram-container` entirely. No filtering rule exists to get wrong. Content overflowing past `.diagram-canvas` (ticket 04's live 197px strip) still reaches it, because bubbling follows DOM ancestry not visual containment.

**Handed to ticket 17:** ticket 02 specified the wheel listener on "the container element". If wheel sits on `.diagram-container` while pointer sits on `.diagram-canvas`, a wheel over the context menu zooms the board and a press does not. Both should be on `.diagram-canvas`; 17 owns the call.

### The region is the element; the visual is painted by a non-participant

SVG forces it — `pointer-events: stroke` tests the *rendered* stroke, so a `stroke-width: 2` line's target cannot be widened on that element. Generalised: the edge gets a transparent wide-stroke sibling carrying the marker while `.edge-line` becomes `pointer-events: none` and only paints; ports, handles and endpoints become elements sized to their target with the visible circle painted inside by a pseudo-element. Payoff beyond edges: precedence becomes DOM order **among hit elements only**.

**`ContentStyle` publishes `--d12-scale` on `.canvas-content`; regions size `calc(Npx / var(--d12-scale))`.** CSS-only, no per-element computation, riding a string that already re-renders on every zoom change — tldraw's `/ zoom` house idiom expressed in the medium this canvas actually uses. Works for `stroke-width` too. Counter-scaling each affordance fights the absolute positioning; computing sizes in C# would put eight inline styles per selected instance back on the render path `d12canvas-next` already had to fix once.

**Edge hit stroke at `calc(20px / var(--d12-scale))`** — ±10 screen px at every zoom, replacing 0.2 screen px at 0.1×. The one constant here defended by judgement rather than evidence, flagged the way ticket 13 flagged its 250ms.

**Ticket 06 keeps the numbers.** This ticket hands it a `--d12-scale` to divide by; 06 picks port and handle sizes and owns the visual-clamped/hit-unclamped split (`Math.max(zoom, 0.25)`) the map's fog already assigns to it.

### The headline defect: content drops out of the hit set, affordances do not

`LodSizeThreshold = 32`, so an instance below 32 screen px renders as `.lod-placeholder` — which is `pointer-events: none`, **and** skipped by `UpdateMarqueeSelection`, **and** skipped by `FocusableTabStopIds`. Three exclusions, each separately reasoned in a comment. A default 200-wide sticky note crosses at **0.16× zoom**, past which **the entire board is unreachable by every input path that exists**. Select-all would be the escape and ADR 0013 decided it, but it is not built. Zooming out is precisely when you want to grab a cluster and move it, and it is the one moment you cannot.

**ADR 0011 stays settled and is not reversed by fixing this** — its decision names the cost driver as *"mounted interactive components"* and the swap as replacing *"the full interactive Razor component tree"*. That is about mounting; it never says the placeholder cannot be hit. "Non-interactive" lives in `CONTEXT.md`'s definition and a code comment, and that is where the error was. Same shape as ticket 14's finding about ADR 0012.

The rule, split by kind:

- **Entities are never dropped.** The LOD placeholder becomes a `Hit target` with role `instance`; all three exclusions go.
- **Affordances drop below a floor** — from **render and hit set together, from one place**, which is Excalidraw's `hasBoundingBox` governing both so they cannot drift. `.port-strip` currently renders under the same `ShowSelectionOverlay` gate as the resize handles but is hit-tested independently of them.
- **Entities get a margin, not a floor.** tldraw's `hitTestMargin: 3 / zoom` is a halo, not a minimum size; a floor on dense content makes neighbouring placeholders overlap and fight, which is worse than the problem.

**Every hit region belongs to something visible** — a region may exceed its visual, nothing is hittable with no visual at all. That is what keeps an invisible halo honest.

### One participation rule, two readers

A press is a point the browser answers; a marquee is a rectangle it cannot — there is no single element under a rubber band — so the marquee stays a C# `Bounds` intersection loop. Nothing makes the two agree today, which is exactly how the LOD exclusion grew its third head: `!IsBelowLodThreshold` had to be hand-written into the marquee loop because a marquee never sees `pointer-events: none`.

**One predicate, evaluated once per entity in C#**, with two consumers: it decides whether the markup carries a hit marker at all, and the marquee asks it directly. C# decides, the DOM reflects.

They differ in exactly one respect, deliberately: **the press margin does not apply to the marquee.** The margin forgives an imprecise click on a thin target; a region selection has no aim problem and should select exactly what it covers. Written down so nobody "fixes" it later.

### Locking taken here, not left as a seam

Directed to take it now rather than graduate the fog patch. **Locked means nothing modifies this** — not clickable, marquee-able, movable, resizable, deletable, alignable, nudgeable or restylable by any route; reachable only enough to find it and unlock it. A pointer-only lock buys nothing: you would Tab to the background image and nudge it away with an arrow key, the exact accident locking exists to prevent. Miro states the same semantic.

- **`Locked` bool on `ComponentInstance` and `Edge`, not on `Group`** — ADR 0008's precedent (a group has no `ZIndex` of its own; layering is a bulk write). "Is this group locked?" derives from members exactly as its bounds do, and ungrouping a locked group leaves the parts locked.
- **Persistence:** one optional field, no `SchemaVersion` bump — ADR 0016's precedent, and by that same precedent it does **not** reopen ADR 0004.
- **Undo:** `ChangeLockedCommand` per entity inside the existing `CompositeCommand`, mirroring `ChangeZIndexCommand`. `MutateEntityCommand` cannot carry it — that swaps opaque `Props`, and `Locked` is a field. **Widens `CONTEXT.md`'s closed command set by one**, stated not smuggled.
- **Amends ADR 0013** (select-all skips locked, or it returns a selection most commands no-op on) **and ADR 0014** (align/distribute skip them — a mixed selection aligns the rest rather than refusing). This is the cross-cutting cost, accepted knowingly.
- **The panel is the unlock route** — Tab to it, it selects, fields disabled, unlock control live. Depends on neither ticket 10's context menu nor ticket 16's bindings, both still blocked. Same split ticket 12 used: semantics and command here, surfaces there.

**The guarantee that makes it safe, and that today's code violates: pointer participation and keyboard reachability are separate properties, and nothing may key one off the other.** They are welded together now — the LOD placeholder is unclickable *and* Tab-skipped from two hand-written checks testing the same condition. Had locking inherited that habit, a locked object would vanish from the keyboard too, which ADR 0010 would not survive, and which would make a locked entity unrecoverable.

**Known rough edge, not solved here:** clicking a locked object does nothing *visible* — the press falls through to `canvas` and starts a pan, with no feedback about why. Every reference tool fixes this with a lock badge on hover. Handed to the map's *cursor and micro-feedback vocabulary* patch.

### Precedence

Nested participants sort themselves out — the walk finds the deepest marker first, so a port beats its container with no rule. Overlapping **siblings** do not, and today a resize handle beats a port for one reason only: it appears later in `ComponentContainer.razor`.

Front to back: **affordances** (selection handles → resize handles → ports → edge endpoints → port strips) → **`selection-bounds` interior** → **content** (author content → instances by paint order → edge labels → edges) → **bare canvas**. Declared as `z-index` values in **one CSS block**, so no participant's precedence depends on where it sits in a `.razor` file — the teardown's point about tldraw writing vertex-beats-virtual down rather than inheriting it.

**Resize handle beats port**, preserving today's behaviour so nothing regresses, and because resize is more frequent. Ticket 06 is expected to separate them geometrically so the rule almost never fires — no reference tool solves this collision because none of them creates it.

**Instances always beat edges regardless of `ZIndex` arithmetic.** Ticket 14 found `PreviousZIndex()` returns `min - 1` while `.edges-layer` is pinned at `z-index: 0`, so send-to-back drops a component beneath every edge. With a 2px line that was cosmetic; with a 20px hit band a sent-to-back component stops being clickable wherever an edge crosses it. The hit rule is fixed independently of the paint bug — which stays fog, and got **sharper**, not resolved.

### Unblocks the spine

Ticket 01's blockers were 02, 03, 04 and 18. All four are now resolved, so **[Pointer gesture arbitration model](01-pointer-gesture-arbitration-model.md) is takeable** — and it inherits a concrete seam rather than a question: a classification arriving from JS, synchronously actionable before the interop hop.
