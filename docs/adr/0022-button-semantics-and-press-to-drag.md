# The primary button selects and drags, pan moves to the secondary and middle buttons, and the context menu opens on release

A primary press commits to a gesture that acts on board content: a selection box on empty canvas, a move on an instance, a resize on a handle. Pan leaves the primary button entirely and lives on the secondary and middle buttons. A secondary press commits to `Pan` in the `pointing` phase and, if it releases without crossing the drag threshold, resolves the selection and opens the context menu at the press point instead. The threshold is 4 screen pixels for both buttons.

This exists because a press was being interpreted too narrowly in two directions at once. `ComponentContainer.HandleMouseDown` arms its move gesture only when `IsSelected` is already true, so dragging an unselected instance needs a click to select followed by a separate press to drag, which is the whole of the reported "shapes are hard to drag". And `DiagramCanvas.HandleMouseDown` returns immediately on `e.Button != 0`, so the secondary button reaches nothing but a context menu that opens only when something is already selected.

## The primary button is the only one that reads the role

ADR 0017 classifies a press into one of eleven roles. ADR 0018 maps roles onto eight gesture owners but left `canvas` ambiguous between `Pan` and `MarqueeSelect`, deferring the button question here. Filling it in produces a table with a shape worth naming.

| Role | Primary | Secondary | Middle |
|---|---|---|---|
| `canvas` | `MarqueeSelect` | `Pan` | `Pan` |
| `instance`, `selection-bounds` | `MoveSelection` | `Pan` | `Pan` |
| `resize-handle`, `selection-handle` | `ResizeSelection` | `Pan` | `Pan` |
| `port`, `port-strip`, `edge-endpoint` | `DragEdgeEnd` | `Pan` | `Pan` |
| `edge`, `edge-label` | `SelectEdge` | `Pan` | `Pan` |
| `author-content` | `Native` | `Native` | `Pan` |

**The secondary and middle buttons ignore the role, with one exception each way.** So the eleven-way classification is consumed by the primary button alone, and the two pan routes need a single fact the listener already holds. That is not a tidiness observation: it means adding a role later cannot silently change what the pan buttons do, and it means the pan gesture never needs to ask what it was pressed on.

The one exception in each direction is load-bearing. A secondary press on `author-content` stays `Native` because the browser's own text menu is where spellcheck and a textarea's own cut/copy/paste live, and suppressing it would be a straight regression for any author whose component holds an input. A middle press on `author-content` pans, because otherwise an instance that is mostly a control swallows every pan gesture that starts over it. What that costs is middle-click primary-selection paste inside an author's textarea on Linux, which is worth less than a pan route that works everywhere.

## Plain primary drag on empty canvas draws the selection box

Today a primary drag on empty canvas pans and `Shift` makes it a selection box. That inverts.

The reference bar is unanimous: all four tools draw a selection box on a plain primary drag over empty canvas, and put pan on the secondary button, the middle button or held space. Miro's help centre carries the FAQ that is this ticket's own seed note, answering "when I try to pan around the board, I move the board objects instead" with "click the right mouse button and drag".

But consensus is not the reason. The reason is that **`Shift` cannot serve as both the selection modifier and the mode modifier on the same surface.** ADR 0006 gives `Shift` a selection meaning, and spending it on "draw a box at all" is why `UpdateMarqueeSelection` carries the comment "Replaces the selection outright... not additive". There is no way to add to a selection with a box, because the modifier that would mean *add* is already gone. Pan has two other buttons available. The selection box has none.

## Press-to-select-and-drag

A primary press on board content resolves the selection immediately, so the drag it arms has something to move.

| Primary press lands on | At press | On release from `pointing` |
|---|---|---|
| Non-member instance | selection collapses to it | nothing further |
| Member instance, or `selection-bounds` | selection unchanged | selection collapses to the pressed entity |
| Non-member, `Shift` held | appended to the selection | nothing further |
| Member, `Shift` held | selection unchanged | toggled out of the selection |

Membership is tested on the outermost containing entity, which `EffectiveSelectionId` already computes for both a click and a box drag, so a press on a member of a selected `Group` counts as a member. Click-through into a group to address one member stays where ADR 0006 put it, deferred.

**The non-member case has to resolve at press** because the drag must move something and ADR 0018 requires the press-time decision to be complete. Deferring would mean a drag beginning on an unselected shape moves the previous selection. **The member case has to defer** or a multi-selection dies the instant you try to drag it. tldraw states the same split in code, in `PointingShape.onEnter`, recording `didSelectOnEnter = false` and handing the outcome to `onPointerUp`.

**`Shift`'s asymmetry is what makes a shift-drag possible at all**: append at press so the drag carries the enlarged set, toggle at release so a shift-click can still correct a mistake. ADR 0006's toggle survives verbatim.

The claim worth banking, because it is what makes this cheap rather than a selection-model rewrite: **every click outcome is unchanged.** `SelectComponent` already collapses to the clicked entity on a plain click and toggles on shift, and both paths were traced against this table. A plain click on a non-member ends in the same state as today, and so does a plain click on a member of a multi-selection. The only behavioural change is that a drag now works from a cold press.

One apparent inconsistency, reconciled rather than left to a reader. ADR 0018 gave `SelectEdge` no active phase and has it select on release, so an edge press resolves later than an instance press. The rule behind both: **selection changes at press only when a gesture has an active phase that would act on the selection.** An edge press has nothing to enable, so there is nothing to decide early.

## `Shift` is a selection modifier and nothing else

`Shift` with a primary drag on `canvas` unions the box's intersection set into the existing selection rather than replacing it. This is the payoff of the inversion above, not a bonus feature.

**Union, not toggle**, diverging from shift-click deliberately. A toggling box deselects whatever it sweeps across, so the result depends on the prior selection in a way the user cannot predict while the band is still moving. Both open-source tools union.

`Shift` gets no in-gesture meaning here. Axis-locked movement, which Excalidraw binds to `Shift` on a drag, is a real candidate and composes with the table above without a clash: below the threshold the press is a selection toggle, above it the `Shift` reads as a constraint and the toggle never happens, because the threshold already separates the two. It is handed to the alignment-guides and object-snapping work anyway, because ticket 03 found `Shift`'s natural partner already claimed. Ctrl is the "escape from automatic help" key in all four reference tools, suspending or inverting snapping, and axis-lock and snapping answer the same user question. Deciding them in two places risks two constrain modifiers that were never designed together.

`Shift` on the wheel means horizontal pan on the `Mouse` profile (ADR 0019). That is a different event and does not contend.

## The secondary button changes nothing until release

A secondary press commits to `Pan` in the `pointing` phase. Crossing the threshold promotes it. Releasing from `pointing` resolves the selection and opens the menu, anchored at the **press** point.

| Secondary press lands on | On release from `pointing` |
|---|---|
| Inside the current selection | selection preserved, menu opens |
| An unselected instance | selection becomes that instance, menu opens |
| An edge | that edge is selected, menu opens |
| Empty canvas | selection cleared, empty-canvas menu opens |

Reduced to one predicate: does the press's entity, resolved outward to its group, belong to the current selection? Yes preserves, no replaces, no entity at all clears.

**The menu moves from press-time to release-time, and that is unavoidable rather than chosen.** tldraw documents the same consequence on its own option: with right-drag-pan on, "a static right-click opens the context menu at the release position", and with it off, "right-click opens the context menu on press". Nothing can know which gesture a press was until the button comes up. It is the same trade the primary button already makes between click-to-select and drag-to-move, and it needs no new mechanism: the menu becomes a click outcome of a `pointing`-phase release, exactly as selection is.

**Anchored at the press point, not the release point**, diverging from tldraw. ADR 0018 takes the classification at press and holds it, so the menu's contents are already decided by what was pressed. Anchoring at the release point lets the anchor and the subject disagree by up to the threshold distance for no gain, since the two points are within 4 pixels by construction.

**The selection resolving at release rather than at press is forced by pan sharing the button.** Resolving at press would mean every right-drag pan wipes the selection as a side effect, and with right-drag now the primary way to move around the board that would happen constantly. Pan does not need a selection and the menu does, so the selection is established at exactly the moment the menu opens. The asymmetry with the primary button is therefore not an inconsistency but the same principle applied to a button whose drag does not act on content.

**A secondary press reaches a locked entity and selects it.** ADR 0017 holds that a locked entity cannot be clicked and rejected the context menu as its unlock route, on the stated grounds that the decision was not yet taken. This is that decision. tldraw makes right-click the one route that hits a locked shape for exactly this reason. ADR 0017's panel route still exists and still needs nothing new, so this is a second route rather than a missing one, but right-click is where a user looks for *unlock*. Whether the menu carries an unlock item is the context-menu decision's; that a secondary press reaches the entity is this one's.

## The browser's menu survives exactly where the canvas does not take the press

`DiagramCanvas.razor` suppresses the native menu conditionally today, via `@oncontextmenu:preventDefault="@HasContextMenuEligibleSelection"`, so an empty selection lets the browser's menu through. That becomes unconditional on board content, and not only for tidiness: an unsuppressed native menu appearing mid-right-drag fires `pointercancel`, which the Pointer Events specification mandates when "the user agent has opened a modal dialog or menu", and ADR 0018 makes `pointercancel` revert the gesture. The native menu would abort the pan it interrupted.

The `author-content` exception needs no mechanism. `Native` takes no `preventDefault` on the press, so `contextmenu` fires there normally and the browser handles it.

Suppression elsewhere is added to the JS listener, classifying from `event.target` on the `contextmenu` event itself rather than consulting a stashed press classification, which is the shape `wasPortDragging` has today and which ADR 0017 refused on principle. **This makes role-derived suppression the fifth synchronous decision in ADR 0018's list and the first on an event other than `pointerdown`**, declared here rather than left for a later reader to find in code.

Whether `contextmenu` fires at all after a prevented `pointerdown` is engine-dependent, in exactly the way ADR 0018 flagged for `dblclick`. The uncertainty is harmless in this direction: if it fires the listener suppresses it, and if it does not there was nothing to suppress. No probe is needed, unlike the `dblclick` case, because nothing depends on it firing.

## Four screen pixels, one number, no timer

**4 screen pixels**, for both buttons and every role. Screen pixels rather than board units, so the same physical hand movement crosses it at 0.05x and at 8x.

4 is Windows' own `SM_CXDRAG` default and tldraw's `dragDistanceSquared`. It is the number a user's hand is already calibrated to from every other application on the machine. Excalidraw's 10 is the outlier and publishes no reasoning. Microsoft's reason for the mechanism existing at all is the one that matters here: so a user "can click and release the mouse button easily without unintentionally starting a drag operation".

The accepted cost, stated because it is a real one and new: a slightly shaky click on an instance now commits a 4-pixel move, and under ADR 0020 that is a genuine history entry. Accidental micro-nudges become possible where before a cold press did nothing at all. Whether a sub-threshold commit should be a no-op is a commit-shape question, not an arbitration one, and is left where it belongs.

**One number for both buttons.** A larger secondary-button threshold is available, on the argument that losing a context menu is more annoying than a small wobble, and is rejected: Windows uses one number for both in its own shell, and nothing in the reference bar varies it by button. The dimension the reference tools do vary it by is pointer type, which ADR 0018 already established as JS's own datum.

**No time threshold.** tldraw promotes a held press to a drag after 500ms, for touch users who cannot hold perfectly still. Skipped, and the slot deliberately left empty: Miro binds long-press to an opt-in containment selection, and long-press is the standard touch route to a context menu, which this ADR has just moved onto a button touch does not have.

The threshold applies to the canvas and the minimap, the two surfaces ADR 0018's listener covers. tldraw's much larger chrome threshold, 25 pixels against accidental toolbar drags, has no counterpart here: ADR 0018 established that a palette drag arrives through the browser's own drag-and-drop and never reaches the classifier at all.

## Ownership keys on the button, not just the pointer

ADR 0018 keys ownership by `pointerId` and drops events from any other pointer. That covers a stylus touching mid-drag. It does not cover a second **button** going down on the same pointer, which is the same `pointerId`, and which with pan on the secondary button and the selection box on the primary is something a user will do by accident.

The release side is the sharper half: `pointerup` fires per button, so press primary, press secondary, release primary yields a `pointerup` while a button is still held, followed by a second one later. Something has to say which ends the gesture.

**A gesture records the button that claimed it, and only that button's release ends it.** Any other button going down or up while a gesture is live is dropped and the active gesture keeps running. The gesture needs to know its own button regardless, since that is what decides whether its `pointing`-phase release opens a menu or collapses a selection.

Dropping rather than treating a secondary press as a cancel: ADR 0018 already routes Escape to the active gesture's cancel, and a second abort route with no way to tell which one a user meant is worse than none.

**A middle press that never crosses the threshold does nothing.** Its only gesture is a pan, and a pan of zero distance has no click outcome to define.

## Touch inverts one row and nothing else

ADR 0018 predicted this table and pre-blessed the consequence, rejecting the constraint that every gesture be reachable by a plain primary press precisely because "if left-drag on empty canvas becomes marquee and pan moves to right, middle or space, pan has no plain-primary route, which on touch is exactly backwards, since one-finger drag should pan and marquee is the gesture that needs a mode".

So the touch table, when one exists, **inverts the `canvas` row**: one finger dragging empty space pans, and the selection box becomes the gesture that needs a mode. Every other row transfers unchanged. Two tables, which ADR 0018 already accepted as costing nothing.

Nothing extra is built. The mapping already takes `pointerType` as an input, so a touch table adds rows rather than changing the arbiter's shape, and the drag threshold is already a lookup with only the fine-pointer entry populated.

The one thing this ADR takes away from touch is the context menu, which now lives on a button touch does not have. Leaving long-press unbound above is what keeps the replacement cheap.

## What this amends

**ADR 0009 is amended.** Its context-menu paragraph is replaced: the menu opens on release rather than press, resolves the selection at that moment, and exists on empty canvas (whose premise ADR 0015's addendum had already retired). Its core claim is confirmed and strengthened rather than touched, since this ADR rejects both a held space quasimode and a Hand tool, so "no persistent tool modes" now holds with no exception at all.

**ADR 0006 is amended in one place**, gaining the additive selection box under `Shift`. Its shift-click toggle is confirmed word for word, and its deferral of click-through into a group is left standing.

**ADR 0017 is amended.** Its text states that a locked entity cannot be clicked, and its rejected list rules out reaching one through the context menu. A secondary press now reaches a locked entity and selects it. The ADR's own wording changes rather than being reinterpreted, because on this map a summary reading stricter than its decision has caused three separate corrections already.

**ADR 0018 is amended in two places.** Its list of synchronous role-derived decisions gains a fifth item, native-menu suppression, which is also the first on an event other than `pointerdown`. Ownership keys on the pointer **and the button** rather than the pointer alone. Its deferred `Pan`-versus-`MarqueeSelect` question is discharged here.

**ADR 0019 is confirmed, untouched.** `Shift` means horizontal pan on the wheel and additive select on the pointer, which are different events. Leaving Ctrl unbound on the pointer means its wheel meaning has nothing to contend with.

**ADR 0010 is untouched**, and inherits one job: the context menu has moved onto a button, so a keyboard route to it is now missing, which the keyboard-parity work owns.

## Considered and rejected

- **Keeping pan on the primary button with the selection box on `Shift`** — leaves `Shift` unable to mean *add* on a box drag, which is the one selection operation the current model cannot express, and leaves pan holding a button two other buttons could serve.
- **Held space as a third pan route** — a pan quasimode every reference tool ships, and the cleanest possible example of the held-versus-entered restatement ticket 03 wants for ADR 0009. Rejected because it costs a `preventDefault` on the space keydown (so space stops scrolling a host page whenever the canvas holds focus), a guard keeping it away from a focused palette entry and a live text edit, and a cursor change to be discoverable at all, for a gesture that already has two routes.
- **A Hand tool as an entered mode with an `H` binding** — all four reference tools ship one, and all four have a toolbar to put it in. D12Canvas has a palette of component types, so this would introduce the first entered mode in the product and the chrome to host it, for a gesture with two routes already.
- **Pressing through content with the platform accel modifier** — tldraw turns an accel-drag on a shape into a selection box and Excalidraw refuses to drag under it, both because on a dense board the thing under the pointer is usually not the thing you want to grab. It is a genuine hole: a selection box can only start on empty canvas, and ADR 0017 widened the problem by making the multi-selection box a solid hit target that consumes presses aimed beneath it. Rejected here only because Ctrl is the reference bar's snapping-suppression key and the alignment-guides work has the better claim on it. Askable again once that decision lands. Note that it could not have been bound to Ctrl anyway without a platform check, since Ctrl+click is the macOS secondary click: `DiagramCanvas.razor.js`'s existing `(event.ctrlKey || event.metaKey)` is correct for keyboard shortcuts and wrong for a pointer modifier, the same shape of split ADR 0018 already made for its two target guards.
- **`Shift` as an in-gesture axis lock** — composes cleanly with shift-to-toggle because the threshold separates them, and still handed to the alignment-guides work so the whole constrain vocabulary is decided at once.
- **Alt-drag to duplicate** — universal across the reference bar, and not a button-arbitration question in disguise. It is either a ninth member of ADR 0018's closed set or a change to what `MoveSelection` commits, and tldraw's mid-drag toggle needs history that can rewind a partly-committed gesture, which ADR 0007 does not currently provide. It belongs with ADR 0013's duplicate model. Alt stays unbound on the pointer.
- **A larger threshold for the secondary button** — Windows uses one number for both, and no reference tool varies it by button.
- **A 500ms long-press promoting a stationary press to a drag** — spends a slot Miro uses for containment selection and touch uses for the context menu, to solve a problem only touch has.
- **10 pixels rather than 4** — fewer accidental micro-nudges, at the cost of drags that feel sticky to start and a divergence from the platform default every other application on the machine follows.
- **A secondary press mid-drag cancelling the active gesture** — a second abort route alongside Escape's, with nothing to tell a user which one they invoked.
- **Resolving the secondary button's selection at press** — makes every right-drag pan wipe the selection as a side effect, on the button that is now the primary way to navigate.
- **Anchoring the menu at the release point**, as tldraw does — lets the menu's position and its subject disagree, for no gain over a press-point anchor within 4 pixels of it.
- **Letting the browser's menu through on bare canvas**, as today's conditional `preventDefault` does — it now aborts the pan it interrupts, via the `pointercancel` the specification mandates when a menu opens.
- **Suppressing the native menu on `author-content`** — costs an author's `<input>` its spellcheck and its own clipboard items, which is the class of workaround ADR 0017 was pleased to delete rather than add.
- **`author-content` swallowing all three buttons uniformly** — leaves an instance that is mostly a control with no pan route starting over it.
