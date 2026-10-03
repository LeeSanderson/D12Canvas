# One focus write per press, and nothing follows the selection

Every pointer press moves DOM focus exactly once, to `.diagram-canvas`, which gains `tabindex="-1"` and is focused with `{ preventScroll: true }`. No selection change moves focus anywhere. ADR 0018's separate blur call is removed as redundant.

## The question was miscast, because the write it composes with is being deleted

ADR 0010's name reads one way and its decision reads the other. Its text is "landing focus on an entity selects it outright", `DiagramCanvas.FocusEntity` is the sole entry point, and it is reached only from a tab stop's own `@onfocus`. Focus drives selection. Selection has never driven focus.

The pointer-side write that looks like the reverse is `ComponentContainer.HandleClick`, which calls `focusElement(_containerRef)` on a plain non-shift click when the container is `Focusable`. That is not a selection change: a shift-click, a marquee and a press on a grouped member all skip it. ADR 0018 deletes every `@onclick` binding on board content, and ADR 0035 already recorded that this is `focusElement`'s only caller.

So there is no second write to compose with, only a hole where one used to be, and this decides whether to refill it.

## One write, because a second one cannot be unconditional

ADR 0018 rejected leaving the transfer to focus-follows-selection because "it works for a press that changes the selection and fails for one that does not", and "a rule that holds for some gestures is what this model exists to eliminate". A second, entity-targeted write reintroduces that conditionality one step later. It would have to be skipped for `Pan`; for `MinimapPan`, which has no tab stop to land on at all, ADR 0026 having given it none and marked it `aria-hidden`; for a marquee; for `author-content`, or it takes the caret out of the input the user just clicked into; and for a press on a member of a multi-selection, where ADR 0022 defers the selection decision to release, so at press time there is nothing to follow.

It also breaks ADR 0031. The `Selection snapshot` is taken at press, and a second write arriving through `@onfocus` reaches `FocusEntity`, which hard-selects. Escape would then revert to a selection the user never had.

Two of the ticket's questions therefore dissolve rather than resolving. **A press that changes nothing behaves identically to one that changes everything**, because there is one write and its target does not depend on the outcome. And there is no intermediate state to observe: no focus-ring flicker, no second focus event for assistive technology. The flicker half was moot twice over, since `.component-container:focus` and `.group-tab-stop:focus` both declare `outline: none`. This library draws no focus ring anywhere, which is ADR 0010's "no notion of a focus ring distinct from selection" honoured in CSS.

## The target needs an attribute it does not have

Neither `.diagram-container` nor `.diagram-canvas` renders a `tabindex`, so ADR 0018's "focuses the canvas container" is a **no-op as written**. `.focus()` on a non-focusable element does nothing, the active element keeps focus, and the blur that commits an inline edit never fires either.

`.diagram-canvas` gains `tabindex="-1"`. It is the element ADR 0018 already names as the stable one that outlives every gesture and takes pointer capture, the element the pointer listener is attached to, and the element that calls `preventDefault`. Focus, capture, `preventDefault` and the listener then all act on one element rather than two. It is strictly inside `.diagram-container`, so ADR 0026's guard passes on its primary clause rather than its fallback.

`-1` rather than `0`, because `0` inserts a tab stop ahead of every entity in a tab order ADR 0010 defined as reading order over entities, and a keyboard-reachable stop owes a visible focus indicator under WCAG 2.4.7. A programmatic-only target owes none, which is what makes `outline: none` survivable.

The call is `focus({ preventScroll: true })`, and that belongs in the decision rather than in the implementation. `.diagram-container` is `overflow: hidden`, `.diagram-canvas` does not fill it (ticket 04 found `.canvas-content` overflowing into a 197px strip below it), and an `overflow: hidden` box is still programmatically scrollable. An ordinary `.focus()` can therefore set a `scrollTop` that nothing in this library ever reads, on a canvas whose panning is entirely transform-based. The pixels would desynchronise from `ZoomPanTracker` silently and permanently.

The separate blur goes. Focusing a focusable element blurs the previously focused one and fires `blur` on it, so the transfer is one call rather than two. ADR 0018 described it as blur-then-focus because it had no focusable target to rely on; with one, the second step is the whole mechanism. Focusing the canvas when it already holds focus is a no-op and fires no blur, which is correct, because there is nothing to commit.

## Blur-only was rejected on Escape, not on tidiness

Blurring with no focus target leaves focus on `<body>` and does get the commit. It fails on ADR 0026, whose guard passes when `activeElement` is inside `.diagram-container`, or when nothing is focused and no text selection lives outside it. On `<body>` only the second clause is available, and that clause is itself qualified. So a user who selects a paragraph on the host page, presses the canvas, and presses Escape cancels nothing. ADR 0026 line 51 states that "ADR 0018's press-time focus transfer means a live gesture implies focus inside the container", and blur-only makes that sentence false. `Tab` after a press would also resume from the top of the document rather than from the board.

## The keyboard's anchor goes null on a press

`_focusedTabStopId` is documented as "whichever entity currently has real DOM focus" and has three readers: `Ctrl+Tab`'s start index, `Space`'s toggle target, and `OnEnterPressed`'s port pick. Once a press focuses the canvas, nothing updates it, and it would hold whatever the last keyboard navigation left there.

It is cleared, by an `@onfocus` on `.diagram-canvas` that nulls `_focusedTabStopId` and `_portFocusInstanceId`. `FocusEntity` already clears the second on every genuine focus change, for the same reason, since a press invalidates an in-progress pick. The field keeps meaning exactly what its comment says, by the mechanism it already uses, and it stays correct for any future route that focuses the canvas instead of being press-specific code.

Setting it from the press classification was rejected because it changes what the field means. It would become "the entity the keyboard acts on next", a second focus concept beside the DOM's, and `Enter` would not honour it: `Enter` is scoped in JS on `isComponentContainerTarget(event.target)`, the real element, so after a press it is out of scope whatever the field holds. Two keys disagreeing about which entity is focused is worse than neither working.

The cost is stated rather than glossed. After clicking a shape, `Space` no longer toggles it out of the selection and `Ctrl+Tab` restarts from the first tab stop. Both are mixed mouse-then-keyboard flows, `Space`'s row exists to pair with `Ctrl+Tab` in a keyboard-only multi-select, and `Ctrl+Tab` is itself carried as suspect.

## `author-content` splits on which branch classified it

ADR 0017 classifies `author-content` two ways: by inference, when the press lands on a natively-interactive element, or by an explicit opt-in marker covering "an author's plain `<div>` with its own click handler". **Every member of the inferred list can take focus. The marked case cannot.**

`Native` skips `preventDefault`, so the browser's own focus transfer runs. On the inferred branch focus lands on the element itself, and `focus` does not bubble, so `.component-container`'s `@onfocus` never fires. On the marked branch there is nothing focusable to land on, so the browser focuses the nearest focusable ancestor, which is `.component-container` with its `tabindex="0"`, firing `@onfocus` into `FocusEntity` and `SelectComponent(id, addToSelection: false)`. A hard single-select, which is precisely what ADR 0018's additive rule exists to prevent: press a marked div on one member of a five-shape selection and the other four are gone.

So the marked branch takes `preventDefault` and the ordinary single write to `.diagram-canvas`, and the inferred branch keeps ADR 0018's treatment unchanged. The discriminator is already computed, because the classifier had to know which branch matched in order to classify at all, so this costs no new state, no new predicate and no new test. The owner is still `Native`: no capture, nothing tracked, selection still additive.

The cost is drag-to-select text inside a marked, non-focusable div. The author's own click handler survives, because `preventDefault` on `pointerdown` does not suppress `click`, and that handler is the stated reason the marker exists.

The ticket called the opposite case the sharpest failure candidate, an additive selection change yanking focus out of the control the user just clicked into. **It cannot occur, because no selection change moves focus.** Clicking an `<input>` inside a registered component leaves the caret there by construction. The test shape ticket 15 wanted for it stands, joined by a second: pressing marked non-focusable content must not collapse a multi-selection.

## One browser fact carries the whole model and must be probed

ADR 0018 assumes `preventDefault` on `pointerdown` suppresses the browser's focus transfer. Today's code prevents on `mousedown` (`ComponentContainer.razor` line 13), so the assumption has never been exercised on the event that will carry it.

If it does not carry, every captured press still focuses `.component-container` natively, `@onfocus` hard-selects, and ADR 0022's press-time selection rules are overwritten on **every** press rather than in one corner. An `Interaction probe` must assert it in a real browser, and a bUnit test must not be accepted as evidence, because a bUnit test supplies the focus the browser was supposed to deliver. That is this map's recurring trap at a fourth instance, after ticket 04's drag tests, ADR 0026's `Ctrl+Tab` file and `ComponentContainer_ClickOutside_ExitsEditMode`.

## Focus moves on three occasions

Stated positively, because every answer above is a removal and the next reader needs the rule rather than its complement:

1. **Native traversal.** `Tab`, `Shift+Tab`, and the browser's own transfer on an uncaptured press.
2. **The press transfer.** Once, to `.diagram-canvas`.
3. **A command handoff that names its target.**

The third exists so the rule does not read as forbidding what is already decided. ADR 0030's `Quick create` hands focus to the new instance, `Ctrl+G` hands it to the group's tab stop through `focusGroupTabStop`, keyboard placement hands it through `focusTabStopAt`, and `BeginEdit` hands it into the editor through `FocusAsync`. A command naming where focus goes is a different thing from focus chasing a selection. Quick create needs no special case: the press focuses the canvas, the release hands focus to the new instance, and the hard-select its `@onfocus` performs is correct there, because the new instance is the sole selection.

## Amends ADR 0010

One amendment, and the decision is not reopened. Focus-follows-selection is sound and nothing here changes it. What is corrected is that **its name reads as the opposite of its text**, and two ADRs have now built on the misreading. ADR 0026 line 51 leans on the press transfer landing focus inside the container, which is true only after this decision. ADR 0030 says "ADR 0010 settled focus-follows-selection, so this is one behaviour rather than two", where ADR 0010 settled the other direction entirely: 0030's behaviour is right and its mechanism exists, so nothing it decided moves, only its stated reason.

A consequence worth carrying to [Whether `Ctrl+Tab` survives the browser](../../.scratch/canvas-interaction-quality/issues/41-ctrl-tab-browser-reservation.md), whose fallback if no chord is free is reopening ADR 0010's welding of focus to selection: after this decision, `.component-container`'s `@onfocus` is the single route by which focus causes selection, and native traversal is the only thing that reaches it. The weld is narrower and more defensible than it was.

## Amends ADR 0018

**The focus transfer is one call to a target that must be made focusable.** Line 129 describes JS blurring the active element and focusing the canvas container. The blur is redundant given a focusable target, and the target is not focusable today, so the sentence as written does nothing at all.

**`Native` no longer means "never `preventDefault`".** Line 105 and the role table read as though the `author-content` row is uniform. It splits on which branch of ADR 0017's classification matched, per the section above. Capture, tracking and the additive selection are unchanged, so the closed set of eight is untouched and ADR 0025's release-reliability `[Theory]` gains no case.

## Amends ADR 0017

The synchronous decisions are described as deriving from the role alone, restated by ADR 0031 as following the press. The focus transfer and `preventDefault` now additionally read **which branch classified an `author-content` press**. That is still a fact the listener holds at press with no interop hop and no selection state, so the constraint those ADRs were protecting is intact; the wording that makes the role the sole input is not.

## Consequences

`.diagram-canvas` gains a `tabindex` attribute and an `@onfocus` binding, so every `.verified.html` baseline moves. The `.png` baselines should not, since neither attribute paints.

`ComponentContainer.HandleClick`, `focusElement` and `ComponentContainer.razor.js`'s remaining export all go with ADR 0018's deletion of the click bindings, which discharges the trigger ADR 0035 handed on: with `OnClickOutside` and `focusElement` both gone, the container needs no JavaScript module, no `_dotNetRef` and no `IAsyncDisposable`.

`DiagramCanvasFocusFollowsSelectionTests` asserts `focusElement` invocation counts in four places. Those assertions do not survive, and the file's subject narrows to what remains true: native traversal selects, and `Ctrl+Tab` moves without selecting.

Two interaction probes are owed, not one. That `preventDefault` on `pointerdown` suppresses the focus transfer, and that a press on marked non-focusable author content leaves a multi-selection intact.

Handed on to [Edit-on-create for a quick-created instance](../../.scratch/canvas-interaction-quality/issues/44-edit-on-create.md), found while checking that `BeginEdit` still composes and not a focus question at all: **ADR 0018 keeps `StickyNote`'s and `Text`'s `@ondblclick="BeginEdit"` on a justification that does not hold for the element carrying it.** ADR 0035 narrowed line 111 to spare that binding, reasoning that `author-content` is the one role skipping `preventDefault` so the browser's own `dblclick` still fires. But the binding sits on `<p class="d12-sticky-note-text">`, a plain paragraph: not natively interactive, not opt-in marked, so ADR 0017's walk-up passes it and classifies the press `instance`, which does `preventDefault`. Marking it `author-content` cannot repair it either, since that is the same region a sticky note is dragged by. So double-click to edit may have no pointer route after ADR 0018 at all, which makes that ticket's question wider than edit-on-create.

## Amended by ADR 0059

A command handoff, the third occasion above, ends `Additive traversal` before it moves focus, so its target's `@onfocus` still hard-selects. "This library draws no focus ring anywhere" no longer holds: a focused stop that is not selected draws a dashed accent outline. The pointer press's `@onfocus` on `.diagram-canvas` also clears the mode flag.

## Considered and rejected

- **Blur with no focus target.** Gets the commit and nothing else. Leaves focus on `<body>`, where ADR 0026's guard passes only through a clause that fails whenever a text selection lives outside the container, so Escape's reachability after a press depends on what the host page happens to have selected.
- **A second entity-targeted write after the hop, suppressed with an ADR 0018-style flag.** The flag is set on an `author-content` press and consumed by the next `FocusEntity`. On the inferred branch the container's `@onfocus` never fires, so the flag is never consumed and sits armed until some later `Tab` traversal eats it and silently fails to select. That is ticket 04's leak family, in the one part of the model with no release to clear itself on.
- **`tabindex="0"` on the focus target.** Adds a tab stop ahead of every entity and owes a visible focus indicator this library does not draw.
- **`.diagram-container` as the target.** Passes the guard equally and is the element `ContainerElement` already references. Rejected for splitting the four synchronous press mechanics across two elements when ADR 0018 chose `.diagram-canvas` as the stable one for capture.
- **Setting `_focusedTabStopId` from the press classification.** Buys `Space` and `Ctrl+Tab` a pointer-driven anchor, at the cost of a second focus concept that `Enter` would not honour, because `Enter` scopes on the DOM element.
- **Reading `:focus-visible` in the focus handler** to tell keyboard focus from pointer focus, which is exactly the discriminator and is computed by the browser. It needs the focus handling moved out of Blazor's `@onfocus` into the JS listener, since `FocusEventArgs` carries nothing, and it buys only what the classification branch already answers for free.
- **Reopening ADR 0010.** Its decision survives this ticket intact and is narrowed rather than contradicted. Its name is the problem, and a name is not worth a supersession.
