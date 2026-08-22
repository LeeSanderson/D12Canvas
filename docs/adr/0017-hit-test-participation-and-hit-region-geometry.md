# The DOM resolves one classified hit target, hit regions are elements sized in screen pixels, and locking rides the same seam

A pointer press on the board is classified exactly once, in JavaScript, by walking up from the event's own target to the nearest marked element; C# receives a `(role, entityId, part)` classification and never touches the DOM. Where a hit region differs from the thing you can see, the region is the participating element and the visual is painted by a non-participant. Content is always hittable at every zoom; affordances drop out below a floor. Locking is one flag consulted by the same participation rule.

This exists because D12Canvas has no hit-testing concept at all. `.edge-line` is `stroke-width: 2` in board units with no widened path, so at 0.1x zoom an edge is ~0.2 screen pixels and selecting one is luck — and ADR 0011 made zoom-out unbounded, which turns that from an annoyance into a guarantee.

## Why the DOM, and why the classification happens in JS

Three mechanisms could resolve a press, and two of them are dead ends here.

**Geometry in C#** — hit-test the press in board space against `Board`, the way `FindPortNear` already does for connector drops — is blind inside an author's component. The library knows only `Bounds`, a rect. A sticky note mid-edit, a built-in `Image`, an author's `<button>`: geometry says "instance body" and cannot tell you the press belongs to the author. Because `.container-content` is `overflow: hidden`, the rect *is* already the true region for an instance, so geometry pays that cost and buys nothing back.

**Per-element Blazor handlers** — the status quo — cannot classify in one place, because **Blazor's `MouseEventArgs` carries no event target.** That single omission is the root of today's arrangement: the only component that knows what was hit is the one that was hit, so arbitration is necessarily distributed across `@onmousedown:stopPropagation` directives and nine boolean flags. ADR 0009's interaction surface can be read from the markup; the mechanism cannot.

So the classification happens in JS, where `event.target` exists. Walking up from it inherits the browser's own hit answer — `pointer-events`, stacking contexts, the `scale()` transform, `overflow` clipping — all respected for free and *consistently with what a native click would have done*. `document.elementsFromPoint` is rejected precisely because it is a separate query that can disagree with actual event dispatch, leaving two hit tests that must be kept in agreement by hand.

This is not a new pattern in this codebase. `addKeyboardListener` already classifies by DOM target through `isEditableTarget` and `isComponentContainerTarget` before invoking anything on C#, and the wheel listener is independently moving into that same file because `@onwheel:preventDefault` is a silent no-op on the pinned runtime.

**The listener is `pointerdown` on `.diagram-canvas`.** Pointer Events rather than mouse events, for two reasons that are already needed rather than speculative: `pointerType` is the input the reference bar's device-varied hit sizing consumes, and `setPointerCapture` is the mechanism the six gesture leak paths require. Touch is not being built; this keeps it from being foreclosed, per the standing non-foreclosure constraint.

## One target, carrying its role

JS hands C# a single resolved classification, not a ranked list and not an element identity.

A ranked list plus a precedence table in C# would create **two competing precedence systems** — the DOM's, which decides paint order and is what the user can see, and the table's, which decides what gets hit. Where they disagree the user clicks the thing on top and something beneath it wins, which is worse than today's accident. tldraw does not do this: it makes hit order and emit order the same thing by construction, stating outright that *"vertex handles come first so they win hit-testing against overlapping virtual/create handles."*

An element identity C# must ask follow-up questions about loses the property everything else here depends on: **the synchronous decisions must be made in JS, before the interop hop.** `preventDefault` cannot wait for a round trip. Neither can `setPointerCapture`. This is the same hazard that made `HandleMouseDown` await `getContainerDimensions` before setting `_isPanning`, letting a fast click's `mouseup` overtake it and leaving the canvas panning invisibly.

Eleven roles, a closed set:

| Role | Carries |
|---|---|
| `instance` | entity id |
| `resize-handle` | entity id, direction |
| `port` | entity id, `PortId` or port guid |
| `port-strip` | entity id, side |
| `edge` | edge id |
| `edge-endpoint` | edge id, source/target |
| `edge-label` | edge id |
| `selection-bounds` | — |
| `selection-handle` | direction |
| `author-content` | enclosing entity id |
| `canvas` | — |

Explicit non-participants, staying `pointer-events: none`: `.group-tab-stop`, `.marquee-select`, `.drag-over-affordance`, `.grid-layer`, `.grid-backdrop`.

**`selection-bounds` becomes a real element.** Today the multi-selection box is `pointer-events: none` while its interior is a live hit region, implemented geometrically as `PointIsWithinSelectionBounds` inside `HandleMouseDown`. Keeping that would mean C# running a second, different hit test after receiving JS's classification — so a press classified `canvas` would sometimes secretly be `selection-bounds`, and "one classification, made once" would be false on its first day. It also does not currently work as intended: the geometric test runs only once a press has already fallen through to bare canvas, so a press inside the box but over an unselected instance behaves differently from the same press over empty space, by accident. `.selection-bounding-box` gains a transparent fill and `pointer-events: auto`; `PointIsWithinSelectionBounds` is deleted.

The accepted cost is that the box now consumes presses aimed at whatever sits under it, including unselected instances. That is the correct behaviour — tldraw's `selectionBounds?.containsPoint` branch does exactly this — but it is a behaviour change, and one worth a test shape.

## Author content claims its own presses

`ComponentContainer` declares `@onmousedown:preventDefault="true"` on its root, which suppresses focus. `StickyNote`'s `<textarea>` therefore carries `@onmousedown:stopPropagation` purely to survive its own container. That workaround is invisible to anyone writing a component against ADR 0001's registration contract, and an arbitrary author's `<input>` simply will not focus.

A press is classified `author-content` when it lands on a natively-interactive element — `<input>`, `<textarea>`, `<button>`, `<select>`, `<a href>`, `[contenteditable]`, `[tabindex]` — which is `isEditableTarget`'s existing shape, widened from keyboard to pointer. An explicit opt-in marker covers what inference cannot reach: an author's plain `<div>` with its own click handler.

This pays for itself immediately. The synchronous `preventDefault` decision becomes one line — `author-content` does not preventDefault, so native focus and text selection proceed; everything else does, suppressing text selection during a drag. **That deletes the blanket `preventDefault` and `StickyNote`'s workaround with it.** A model that removes two workarounds rather than adding a concept is the right one.

What this ADR asserts is only *"the press hit author content, inside instance X."* Whether that also selects X is the arbitration model's decision, not this one's.

## Chrome is excluded structurally

Canvas chrome does not participate, because no chrome component has anything to classify. ADR 0015 settled the minimap as navigation only — *"clicking or dragging pans, and never selects or zooms"* — one behaviour regardless of what is under the pointer. The context menu, palette and property panel are native buttons, HTML5 drag-and-drop and form controls.

What ADR 0015 does need from the arbitration model is capture and single-release, which is separable from hit classification and is not what this ADR provides.

Binding the listener to `.diagram-canvas` rather than `.diagram-container` enforces the exclusion by construction: `SelectionContextMenu` and `.drag-over-affordance` are *siblings* of `.diagram-canvas`, so a press on either bubbles container-ward and never reaches the listener, and host-placed chrome is outside `.diagram-container` entirely. No filtering rule exists to get wrong. Content that visually overflows past `.diagram-canvas` still reaches the listener correctly, because bubbling follows DOM ancestry rather than visual containment.

## The hit region is the element; the visual is painted by a non-participant

SVG forces this: `pointer-events: stroke` tests the *rendered* stroke, so the target of a `stroke-width: 2` line cannot be widened on that element. Rather than treat it as a quirk, it is the general rule.

- **Edge** — a transparent wide-stroke `<line>`/`<path>` carries the marker and the events; `.edge-line` becomes `pointer-events: none` and only paints.
- **Port, resize handle, floating endpoint** — the element is the hit region, sized to the target; the visible circle is painted inside it by a pseudo-element.
- Where region and visual coincide, as on an instance body, one element does both.

Beyond edges, the payoff is that precedence becomes DOM order **among hit elements only** — a dozen or so per instance, declared in one place — rather than among everything that paints.

## Screen-constant sizing is one custom property

Every reference tool divides its hit sizes by zoom so they stay constant on screen. D12Canvas divides by nothing: affordances are fixed CSS pixel sizes inside a `transform: scale()`d container.

`ContentStyle` publishes the scale as **`--d12-scale` on `.canvas-content`**, and every hit region sizes itself `calc(Npx / var(--d12-scale))`. It is CSS-only, needs no per-element computation, and rides a string `ContentStyle` already builds and already re-renders on every zoom change. It works for `stroke-width` too, so the edge hit stroke gets it for free. Counter-scaling each affordance with `transform: scale(1/s)` fights the positioning; computing sizes in C# would put eight inline styles per selected instance onto the render path that `d12canvas-next` already had to fix once.

**The edge hit stroke is `calc(20px / var(--d12-scale))`** — plus or minus 10 screen pixels, constant at every zoom, replacing 0.2 screen pixels at 0.1x. This is the one constant here defended by judgement rather than evidence.

This ADR supplies the mechanism. The **numbers** for ports and resize handles, and the visual-clamped/hit-unclamped split at extreme zoom-out that tldraw expresses as `Math.max(zoom, 0.25)`, belong with the port affordance work, which already owns hit-target sizing under zoom.

## Content never drops out of the hit set; affordances do

The current code drops the wrong thing, in three places at once.

An instance whose larger dimension times scale falls below `LodSizeThreshold` (32) renders as `.lod-placeholder`, which is `pointer-events: none`, **and** skipped by `UpdateMarqueeSelection`, **and** skipped by `FocusableTabStopIds`. A default 200-wide sticky note crosses that at **0.16x zoom**. Past that point the entire board is unreachable by every input path that exists — pointer, marquee and keyboard alike. Zooming out is precisely when a user wants to grab a cluster and move it, and it is the one moment they cannot.

**ADR 0011 is not reversed by fixing this, and stays settled.** Its decision names the cost driver as *"mounted interactive components"* and describes the swap as replacing *"the full interactive Razor component tree"* with a plain box. That is about mounting. It never says the placeholder cannot be hit; "non-interactive" appears in `CONTEXT.md`'s definition and in a code comment, and that is where the error is.

The rule, split by kind:

- **Entities are never dropped.** An instance is content and must stay reachable at any zoom. The LOD placeholder becomes a hit participant with role `instance`, and all three exclusions go.
- **Affordances are dropped below a floor** — ports, resize handles, port strips — from **render and hit set together, from one place**, which is Excalidraw's `hasBoundingBox` governing both so they cannot drift. `.port-strip` currently renders under the same `ShowSelectionOverlay` gate as the resize handles but is hit-tested independently of them.
- **Entities get a margin, not a floor.** tldraw's `hitTestMargin: 3 / zoom` is a small screen-constant halo, not a minimum size. A floor on dense content would make neighbouring placeholders overlap and fight, which is worse than the problem it solves. Affordances get floors because they are few and separated; entities get margins because they are many and adjacent.

**Every hit region belongs to something visible.** A region may be larger than its visual; nothing is hittable with no visual at all. That is what keeps an invisible halo honest.

## One participation rule, two readers

This canvas asks "what is under here?" two different ways. A press is one point, which the browser answers. A marquee is a rectangle, which it cannot — there is no single element under a rubber band — so the marquee stays a C# loop over `Bounds` intersection.

Nothing currently makes the two agree, which is how the LOD exclusion grew its third head: `!IsBelowLodThreshold` had to be written into the marquee loop by hand, because a marquee never sees `pointer-events: none`.

**A single participation predicate is evaluated once per entity in C#**, and has two consumers: it decides whether the entity's markup carries a hit marker at all, and the marquee asks it directly. C# decides, the DOM reflects. Same move as `hasBoundingBox`, applied at entity level.

They differ in exactly one respect, deliberately: **the press margin does not apply to the marquee.** The margin forgives an imprecise click on a thin target; a rubber band has no aim problem and should select exactly what it covers.

## Locking is that predicate's first real consumer

Locked means *nothing modifies this*. A locked entity cannot be clicked by a primary press (ADR 0022 makes a **secondary** press the one exception, see the addendum), marquee'd, moved, resized, deleted, aligned, nudged or restyled by any route; it stays reachable only enough to find it and unlock it. Pointer exclusion is the mechanism of a lock, not its meaning — a pointer-only lock would still let a user Tab to a background image and nudge it away with an arrow key, which is the exact accident locking exists to prevent. Miro states the same semantic: locked objects "can't be selected, moved, edited, or deleted."

**`Locked` is a bool, defaulting false, on `ComponentInstance` and on `Edge`. Not on `Group`.** ADR 0008 set that precedent by refusing a group a `ZIndex` of its own and making layering a bulk write across members; locking follows it, so "is this group locked?" is derived from its members exactly as its bounds are. Ungrouping a locked group leaves the parts locked, which is the wanted behaviour.

Persistence is one optional field, absent by default, with **no `SchemaVersion` bump** — ADR 0016's precedent, and by that same precedent it does not reopen ADR 0004. An old board deserialises to unlocked, which is correct.

Undo is a `ChangeLockedCommand` per entity inside the existing `CompositeCommand` when several are locked at once, mirroring `ChangeZIndexCommand`. `MutateEntityCommand` cannot carry it — that one swaps opaque `Props`, and `Locked` is a field like `ZIndex`, not business data. **This widens `CONTEXT.md`'s closed command set by one**, stated rather than smuggled.

**Select-all skips locked entities**, amending ADR 0013 — otherwise it hands back a selection on which most commands silently no-op. **Align and distribute skip them too**, amending ADR 0014: a selection containing one locked entity aligns the others rather than refusing.

**The panel is the unlock route.** Tab to a locked entity and it selects; the panel shows it with its editable properties disabled and an unlock control live. That depends on neither the context menu nor new keyboard bindings, both of which are still undecided, and it rests on the separation below.

## Pointer participation and keyboard reachability are separate properties

Nothing may key one off the other. They are welded together today — the LOD placeholder is both unclickable and skipped by Tab, from two hand-written checks that happen to test the same condition — and if locking inherited that habit, a locked object would become invisible to the keyboard, which is the accessibility failure every reference tool avoids and which ADR 0010 would not survive.

Stated as a rule rather than left as a consequence, because it is also the only thing that makes locking recoverable: a locked entity that could only be reached by pointer would be locked forever.

## Precedence: affordances beat content, content follows paint order

Nested participants sort themselves out — walking up finds the deepest marker first, so a port inside a container beats the container with no rule needed. Overlapping *siblings* do not, and today a resize handle beats a port for one reason only: it appears later in `ComponentContainer.razor`.

Front to back:

1. **Affordances** — selection handles, resize handles, ports, edge endpoints, port strips
2. **The `selection-bounds` interior**
3. **Content** — author content, instances by paint order, edge labels, edges
4. **Bare canvas**

Expressed as declared `z-index` values on hit elements in **one CSS block**, so no participant's precedence depends on where it sits in a `.razor` file. That is the teardown's point about tldraw writing its vertex-beats-virtual rule down rather than inheriting it.

**Resize handle beats port**, preserving today's behaviour so nothing regresses, and because resize is the more frequent action. The port affordance work is expected to separate the two geometrically so this rule almost never fires — the reference bar's finding being that no tool solves this collision because none of them creates it.

**Instances always beat edges, regardless of `ZIndex` arithmetic.** ADR 0016 recorded that `PreviousZIndex()` returns `min - 1` while `.edges-layer` is pinned at `z-index: 0`, so send-to-back drops a component beneath every edge. With a 2px line that was cosmetic. With a 20px hit band it would mean a component sent to the back stops being clickable anywhere an edge crosses it. The hit rule is therefore fixed independently of the paint bug, which remains ADR 0008's question.

## Considered and rejected

- **Geometric hit-testing in C# over `Board`** — gives one classification made once, but goes blind inside author components, which for a DOM/CSS canvas is the main event rather than a corner case. `overflow: hidden` means it would not even improve accuracy for instances.
- **Keeping per-element Blazor handlers and only tidying them** — cannot classify in one place while `MouseEventArgs` carries no target, so it hands the arbitration model exactly the shape that model exists to remove.
- **A ranked candidate list from `elementsFromPoint`, filtered in C#** — two precedence systems that must agree, one of which the user can see and one of which they cannot.
- **Returning an element identity for C# to interrogate** — loses the synchronous window in which `preventDefault` and `setPointerCapture` must be decided.
- **`mousedown` rather than `pointerdown`** — no `pointerType`, no pointer capture, and forecloses touch for no gain.
- **Binding the listener to `.diagram-container`** — pulls the context menu and drag affordance into the hit set and requires a filtering rule to push them back out.
- **Chrome declaring hit roles and feeding the same classifier** — no chrome component has anything to classify, and it would put the minimap's one behaviour behind an eleven-way switch.
- **Keeping `PointIsWithinSelectionBounds` as a geometric special case** — breaks "classify once" immediately, and its current fall-through-only evaluation is already a latent inconsistency rather than a design.
- **Requiring authors to opt out of the container's `preventDefault` by hand** — the status quo, which the library's own built-in demonstrates is unworkable for anyone who has not read `ComponentContainer.razor`.
- **Author opt-in markers with no inference** — silently breaks the case an author will hit first, which is an `<input>` inside their component.
- **Counter-scaling affordances with `transform: scale(1/s)`** — fights the absolute positioning the ports and handles rely on.
- **Computing hit sizes in C# as inline styles** — eight more style strings per selected instance on the render path `d12canvas-next` had to fix once already.
- **Flooring entity hit regions at an accessible minimum** — makes dense placeholders overlap and fight at low zoom; a margin achieves the aim without the ambiguity.
- **Leaving the LOD placeholder non-interactive** — the reason for the placeholder is mounting cost, which non-interactivity does not serve, and it makes the board inert below 0.16x zoom.
- **Applying the press margin to the marquee** — a region selection has no aim problem, so the margin would only make it select things it visibly does not cover.
- **A pointer-only lock** — leaves the keyboard route to the exact accident locking exists to prevent.
- **A `Locked` flag on `Group`** — a second source of truth against member flags, and against ADR 0008's precedent of giving a group no state of its own.
- **Locking via `MutateEntityCommand`** — that command swaps opaque `Props`; `Locked` is a field, not business data.
- **Deferring locking to its own later ticket** — the participation predicate makes the semantics nearly free, and splitting them would record the same decision twice.
- **Reaching a locked entity through the context menu** — the natural surface, but it depends on decisions not yet taken, and the panel route needs nothing new. **Reversed by ADR 0022**, which is the decision this was waiting on.

## Addendum (surfaced while resolving the pointer gesture arbitration ticket)

The classification is **wider than the `(role, entityId, part)` triple above**. ADR 0018 needs the press to carry **press count** (from `event.detail`), **`pointerType`**, **buttons** and **modifiers** as well: click and double-click dispatch off the classification rather than off the browser's own `click`/`dblclick` events, the movement threshold varies by `pointerType`, and the press-to-gesture mapping reads buttons and modifiers. Recorded here explicitly so the triple is not read as the whole payload.

Everything else holds unchanged, and two of this ADR's rules turned out to be load-bearing in ways it did not state. First, the four synchronous decisions ADR 0018 makes in JS — `preventDefault`, `setPointerCapture`, focus transfer and the threshold — all derive from the **role alone**, which is what keeps selection state out of JS entirely. Second, `author-content` presses select their enclosing instance **additively** (never removing anything from the selection), which is the question this ADR explicitly deferred.

**Addendum (surfaced while resolving the right-button and press-to-drag ticket):** ADR 0022 amends the locking section above in one place, and it is a genuine reversal rather than a clarification. **A secondary press reaches a locked entity and selects it.** The unlock route through the context menu, rejected above on the stated grounds that it "depends on decisions not yet taken", is the decision ADR 0022 took; tldraw makes right-click the one route that hits a locked shape for the same reason. The panel route stays exactly as specified and still needs nothing new, so this is a second route rather than a hole being patched, but right-click is where a user looks for *unlock*. Whether the menu carries an unlock item is the context-menu decision's; that the press reaches the entity is ADR 0022's.

Nothing else about locking moves. `Locked` stays a bool on `ComponentInstance` and `Edge` and off `Group`, the `ChangeLockedCommand` and the `SchemaVersion` decision are untouched, and the rule that **pointer participation and keyboard reachability are separate properties** is unaffected: this widens pointer reachability by one button without touching keyboard reachability at all.

**Addendum (surfaced while resolving the context menu enrichment ticket):** ADR 0023 answers the question ADR 0022's addendum left open and adds one action.

**The menu carries an unlock item**, as a single row reading Unlock when the selection is a single locked entity and Lock otherwise. There is no mixed locked-and-unlocked state to design for, and that falls out of this ADR plus ADR 0022 rather than being asserted: a locked entity cannot be primary-clicked or marquee'd, and a secondary press reaches exactly one at a time and replaces the selection with it. The panel route above is untouched and still needs nothing new, so this is a third route rather than a hole being patched.

**The canvas menu also carries Unlock All**, which is the one genuinely new action in ADR 0023. It is one `CompositeCommand` over a `ChangeLockedCommand` per locked entity, so the closed command set this ADR widened by one is not widened again, and the row is ineligible and therefore invisible on a board with nothing locked. It earns a third route by being the only one that does not require locating the entity first. Miro documents exactly this item on its blank-area menu.

Separately, ADR 0023 investigated a z-order disambiguation item and handed it to a ticket of its own, banking one finding against **this ADR's rejection of a C#-side ranked hit list**. That rejection stands as stated: a `Board` scan over `Bounds` sorted by `ZIndex` competes with the DOM's own order, so the user clicks the top thing and something beneath wins. It does not rule out exposing a stack, because `document.elementsFromPoint` returns every element at a point in the DOM's own paint order, and the marker walk this ADR already specifies over that list yields the stack from the same authority that classified the press, with no geometry in C# at all.

Two further notes from the same decision. The eleven roles above are consumed by the **primary button alone**: ADR 0022's secondary and middle buttons both map to `Pan` regardless of role, with `author-content` the one exception each way. And the role-derived synchronous decisions listed just above gain a **fifth** member there, native context-menu suppression, which is the first one on an event other than `pointerdown`.
