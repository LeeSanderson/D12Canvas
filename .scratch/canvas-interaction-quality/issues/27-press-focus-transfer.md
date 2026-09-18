# Press-time focus transfer versus focus-follows-selection

Type: grilling
Status: resolved

## Question

Decide how ADR 0018's explicit focus transfer on press composes with ADR 0010's focus-follows-selection, given that they are now two focus writes per press with an interop hop between them.

ADR 0018 found that `preventDefault` on a captured press suppresses the browser's own focus transfer — which is why `ComponentContainer` declares it — and that suppressing focus transfer also suppresses **blur**, which is what commits an inline text edit and any focused chrome input. Its fix is for JS to blur the active element and focus the canvas container synchronously, in the same window as `preventDefault` and `setPointerCapture`.

But ADR 0010 already moves DOM focus when the selection changes, and a press that selects something changes the selection. So an ordinary press on an instance now produces: JS focuses the canvas container (synchronous), then C# receives the press, mutates the selection, and focus-follows-selection focuses that entity's tab stop (after the hop). Two writes, ordered across an async boundary, with a render in between.

Decide:

- **Whether the JS-side write should target the canvas container at all**, or whether blurring alone is sufficient to get the commit-on-blur behaviour ADR 0018 needs. Blur without an explicit focus target leaves focus on `<body>`, which is a real state with its own consequences for the window-level keyboard listener.
- **Whether the two writes can be reduced to one.** The synchronous write exists only to force a blur; the selection-driven write is the one that matters for ADR 0010. If the first can be expressed as "blur, do not focus", the second becomes the sole author of where focus lands.
- **What a press that changes nothing does.** `Pan`, and a re-press on an already-selected instance, produce no selection change and therefore no second write — so whatever the first write leaves behind is final for those presses. This is the case that killed the leave-it-to-selection option, so it needs a stated answer rather than an inherited one.
- **Whether the intermediate state is observable.** Focus moving to the container and then to a tab stop within one gesture may be visible as a focus ring flicker, and it is certainly visible to assistive technology as two focus events. ADR 0010 is reopenable, so if the right answer changes when focus-follows-selection fires, that is legitimately askable.
- **What `Native` does**, since it is uncaptured and therefore takes neither write — the browser's own focus transfer runs normally, which is the point. Confirm that an additive selection change (ADR 0018) does not then trigger focus-follows-selection and yank focus out of the author's control the user just clicked into. This is the sharpest failure candidate in the whole ticket.

Ticket 15 will want a test shape for the last one in particular: clicking an `<input>` inside a registered component must leave the caret in that input.

## Note added by ADR 0031

Two things moved under this ticket while it sat on the frontier.

**`MinimapPan` now takes the focus transfer too.** ADR 0018 described the four synchronous press decisions as deriving "from the role alone", and the minimap is the member with no role. ADR 0031 restates them as following the **press** — role-derived when classified, fixed for the minimap — because without the transfer, a host input outside `.diagram-container` keeps focus and ADR 0026's uniform guard blocks Escape, leaving `MinimapPan` the one gesture that cannot be cancelled. So the minimap joins bullet three's set: it changes no selection, so whatever the first write leaves behind is final for it, and it has no tab stop of its own to land on (ADR 0026 gives it none and marks it `aria-hidden`).

**Bullet two is now load-bearing for cancellability, not just for tidiness.** "Blur, do not focus" leaves focus on `<body>`, which passes ADR 0026's guard only through its *nothing is focused* clause — and that clause is itself qualified, failing when a text selection lives outside the container. Choosing blur-only therefore makes Escape's reachability depend on whether the host page happens to have a selection, on every press that changes no selection. That is not a reason to reject the option, but it is a cost the ticket did not previously carry, and whichever way it goes the answer should be checked against the guard rather than against focus behaviour alone.

## Answer

Recorded as **ADR 0036**. One focus write per press, to `.diagram-canvas`, and nothing follows the selection.

**The ticket's premise was overtaken twice.** ADR 0010's rule runs the opposite way to its name: its text is "landing focus on an entity selects it outright", `FocusEntity` is the sole entry point, and it is reached only from a tab stop's own `@onfocus`. Selection has never driven focus. The pointer-side write that looks like it does is `ComponentContainer.HandleClick` calling `focusElement`, on a plain non-shift click only, which ADR 0018 deletes with every other `@onclick` on board content and which ADR 0035 already recorded as `focusElement`'s only caller. So there were never two writes to compose, and after 0018 there is a hole where one used to be.

**One write, on ADR 0018's own reasoning.** It rejected focus-follows-selection as the transfer because a rule that holds for some gestures is what the model exists to eliminate; a second entity-targeted write reintroduces that conditionality one step later, needing exclusions for `Pan`, `MinimapPan` (no tab stop at all), the marquee, `author-content`, and a press on a member of a multi-selection where ADR 0022 defers selection to release. It also breaks ADR 0031, whose `Selection snapshot` is taken at press: a write arriving through `@onfocus` hard-selects, so Escape would revert to a selection the user never had. **Bullets 3 and 4 dissolve** — a press that changes nothing behaves identically to one that changes everything, so there is no intermediate state, and the flicker half was moot twice over since `.component-container:focus` and `.group-tab-stop:focus` both declare `outline: none`.

**The target needs an attribute it does not have.** Neither `.diagram-container` nor `.diagram-canvas` renders a `tabindex`, so ADR 0018's "focuses the canvas container" is a no-op as written and the blur never fires either. `.diagram-canvas` gains `tabindex="-1"`, putting focus, capture, `preventDefault` and the listener on one element. `-1` rather than `0` because a keyboard-reachable stop would sit ahead of every entity in reading order and would owe a focus indicator this library does not draw. The call must be `focus({ preventScroll: true })`: `.diagram-container` is `overflow: hidden`, `.diagram-canvas` does not fill it, and a `scrollTop` nothing reads would desynchronise the pixels from `ZoomPanTracker` permanently. The explicit blur goes, since focusing a focusable element blurs the previous one by itself.

**Blur-only was rejected on Escape.** It leaves focus on `<body>`, where ADR 0026's guard passes only through the *nothing is focused* clause, which itself fails when a text selection lives outside the container. ADR 0026 line 51 asserts a live gesture implies focus inside the container, and blur-only makes that sentence false.

**`_focusedTabStopId` goes null on a press**, cleared by an `@onfocus` on `.diagram-canvas` along with any in-progress port pick, so the field keeps meaning what its comment says. Setting it from the press classification was rejected for making it a second focus concept that `Enter` would not honour, since `Enter` scopes on `isComponentContainerTarget(event.target)`. Cost stated: after a click, `Space` no longer toggles that shape and `Ctrl+Tab` restarts from the first stop.

**Bullet 5's sharpest failure candidate cannot occur, and its mirror is real.** No selection change moves focus, so clicking an `<input>` leaves the caret there by construction. But `Native` skips `preventDefault`, and ADR 0017 classifies `author-content` two ways: inferred from a natively-interactive element, which can take focus, or matched by an opt-in marker on "an author's plain `<div>`", which cannot. On the marked branch the browser focuses the nearest focusable ancestor, `.component-container` with its `tabindex="0"`, and `@onfocus` hard-selects, destroying the multi-selection the additive rule protects. Fixed at the branch: the marked case prevents and takes the ordinary write, the inferred case is unchanged. The discriminator is already computed. Cost is drag-to-select inside a marked non-focusable div; the author's click handler survives. Suppressing the select with an ADR 0018-style flag was rejected because on the inferred branch the flag is never consumed and sits armed until a later `Tab` silently fails to select.

**One browser fact carries the whole model**: that `preventDefault` on `pointerdown` suppresses the focus transfer. Today's code prevents on `mousedown`, so it has never been exercised on the event that will carry it, and if it does not carry, every press hard-selects through `@onfocus` and ADR 0022's rules are overwritten board-wide. An `Interaction probe` must assert it, never a bUnit test — this map's recurring trap at a fourth instance.

**Focus moves on three occasions**: native traversal, the press transfer, and a `Command` that names its target. The third keeps `Quick create`, `Ctrl+G`, keyboard placement and `BeginEdit` legal without a special case.

Amends **ADR 0010** (the name reads as the opposite of the decision, and ADR 0030 cited it for a rule it never made — right in behaviour, wrong in reason), **ADR 0018** in two places, and **ADR 0017** in one. `Focus transfer` added to `CONTEXT.md`, `Hit target` widened.

**Handed on to [Edit-on-create for a quick-created instance](44-edit-on-create.md)**, found while checking `BeginEdit` composes and not a focus question at all: ADR 0035 spared `StickyNote`'s and `Text`'s `@ondblclick="BeginEdit"` on the ground that `author-content` skips `preventDefault`, but the binding sits on a plain `<p>`, which ADR 0017's walk-up passes, classifying the press `instance`. Double-click to edit may have no pointer route after ADR 0018 at all, and marking the paragraph `author-content` cannot repair it, since that is the region a sticky note is dragged by.

No new tickets surfaced and no fog graduated.
