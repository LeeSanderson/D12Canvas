# One menu component, two content sets split on whether the press hit an entity, and rows composed by per-item eligibility

The context menu is a single component handed a computed description of what the press resolved to. Where the press hit an entity it offers commands over the selection; where it hit bare canvas it offers board and view settings. Which rows appear is an output of per-item eligibility rather than a layout written once per context, so the six reachable contexts need six predicates rather than six menus. Unavailable rows are hidden, following ADR 0014 and ADR 0015.

This discharges the composition question ADR 0009 opened and three later ADRs deferred here by name: ADR 0014's "how a menu that gains eight items stays readable", ADR 0015's "how the menu is composed", and ADR 0022's "whether the menu carries an unlock item".

The split follows the reference bar rather than being invented. Miro documents its blank-area menu item by item across three help pages, and the pattern across all four tools is that **the canvas menu carries view and board-level settings while the object menu carries object commands**.

## The two content sets

**Object menu**, in fixed section order:

| Section | Rows |
|---|---|
| Clipboard | Cut, Copy, Paste, Duplicate |
| Destructive | Delete |
| Grouping | Group (2+ top-level entries), Ungroup (a `Group` selected) |
| Arrangement | the align and distribute strip, then Bring to Front, Bring Forward, Send Backward, Send to Back |
| Protection | Lock, or Unlock |
| View | Zoom to Selection |

**Canvas menu**, same section vocabulary with most rows ineligible:

| Section | Rows |
|---|---|
| Clipboard | Paste |
| Selection | Select All |
| View | Zoom to Fit, Zoom to 100% |
| Board settings | Snap to Grid, checked |
| Recovery | Unlock All |

Every row is an action some prior ADR already decided, with exactly one exception noted below. Nothing is invented to fill a menu.

**Paste appears on both**, in the same section in the same position. That is what makes the canvas menu the object menu with rows hidden rather than a second design: the clipboard section is one definition whose first three rows are ineligible with nothing selected.

**The view section is likewise one definition carrying all three of ADR 0015's commands**, with zoom-to-selection ineligible when the selection is empty, which on the canvas menu is by construction. Stating it once removes the asymmetry that would otherwise need a rule: zoom-to-fit is meaningful in both contexts and zoom-to-selection is meaningful in one, and eligibility already expresses that.

**Section order is fixed and a separator renders only when a section renders on both sides of it.** That is a decision rather than implementation detail because the eligibility rules produce sparse contexts — a selected edge is four rows — and naive separators produce a leading rule and doubled rules on exactly those.

## Lock needs no third state, because a mixed selection is unreachable

The row reads **Unlock** when the selection is a single locked entity and **Lock** otherwise.

There is no mixed locked-and-unlocked case to design for, and that falls out of two prior decisions rather than being asserted. ADR 0017 stops a locked entity being primary-clicked or marquee'd; ADR 0022 makes a secondary press the one route that reaches one, and it reaches exactly one at a time and replaces the selection with it. So a selection either contains no locked entity or is precisely one locked entity.

**Unlock All is the only genuinely new action in this ADR.** It is one `CompositeCommand` over a `ChangeLockedCommand` per locked entity, so it introduces no command type, and it is ineligible — therefore invisible — on a board with nothing locked, which is the ordinary case. ADR 0017 gave locking the panel as its unlock route and ADR 0022 added the secondary press, so this is a third; it earns its place as the only one that does not require locating the entity first, and Miro documents exactly this item on its blank-area menu.

## The clipboard rows cannot use ADR 0013's mechanism, and that is a mechanism split rather than a defect

ADR 0013 routes cut, copy and paste through the DOM `copy`/`cut`/`paste` events, on the stated ground that the `paste` event is the only permission-free read path in any engine. That reasoning is correct and it is **keyboard-only by construction**. A click on a menu row fires no clipboard event and nothing can synthesise one, so the menu has only `navigator.clipboard` available.

Three facts were measured in a real browser rather than reasoned about, because Playwright grants `clipboard-read` at the context level and would have hidden the one behaviour that matters. Probe on branch `prototype/clipboard-menu-route`, driven in Chromium:

- **A button click fires no clipboard event.** Confirmed against document-level `copy`/`cut`/`paste` listeners that log when they fire, with `Ctrl+V` as the control. The trap is real, so the split is forced rather than chosen.
- **User activation survives the Blazor interop hop, and survives a two-second delay on top of it.** This was the fact most likely to have killed the whole approach. A menu row's payload is known in C#, so unlike ADR 0013's keydown path the write cannot happen inside the native event handler; it has to cross out to C# and back into JS. Had activation expired, the write would work on WebAssembly and fail in a Blazor Server host, which is a defect no test in this repo would have caught. The deliberate two-second delay stands in for that round trip and the write still succeeded.
- **Read prompts once per origin and is remembered thereafter.** A prompt on every menu paste would have been a different decision; a one-time grant is a footnote.

So **ADR 0013's mechanism becomes per-surface.** The keydown and clipboard-event path is unchanged and remains the permission-free route. The menu uses `navigator.clipboard.write` and `navigator.clipboard.read`. `read` rather than `readText`, so a foreign bitmap still arrives and ADR 0013's promise that a pasted image becomes an `"image"` instance holds on both routes.

The accepted costs are stated plainly. There is a second read path to test. Chromium asks once. And the async API is absent outside a secure context, so a host serving the canvas over plain HTTP on a LAN address keeps the keyboard route and loses the menu's clipboard rows, where today it keeps both.

**Cut, Copy and Duplicate are ineligible on an edge selection rather than present and inert.** ADR 0013 makes copy on a lone edge a no-op because the payload carries only interior edges and `_selectedEdgeId` is an exclusive slot, and requires cut to be a no-op for the same reason. Duplicate follows them out. Combined with edges having no `ZIndex`, a selected edge's menu is four rows: Paste, Delete, Lock, Zoom to Selection. That is an honest picture of how second-class an `Edge` currently is and it will be visible to users on contact; whether it changes is a separate open question rather than something this ADR papers over.

## The paste anchor gains a third case

ADR 0013 defines the paste anchor as the pointer's board position when the pointer is over the canvas, and the viewport centre otherwise. Read literally that sends a menu paste to the viewport centre, because by the time a Paste row is clicked the pointer is over the menu, which is a sibling of `.diagram-canvas` and not inside it. The content would land tens of pixels from where the user aimed, or halfway across the board.

**A menu paste's anchor is the press point that opened the menu**, which ADR 0022 already stores in order to position it. The rule generalises rather than gaining a special case: the anchor is where the user last *indicated*, which is the pointer for `Ctrl+V`, the viewport centre for a keyboard-only user with no pointer at all, and the opening press for a menu paste.

Each menu paste therefore resets ADR 0013's `+20, +20` cascade, since the menu closes on invocation and re-opening it means re-aiming. That is the correct behaviour and it needs no rule of its own; ADR 0013 already states the cascade over the anchor rather than over the input device, which is what makes this fall out.

## Align and distribute are one strip, not eight rows and not a submenu

Counted flat, the object menu's maximum is 21 rows. ADR 0014's eight actions are most of that.

**The eight render as a single horizontal strip of glyph buttons, one row tall**, as a `role="group"` of `menuitem`s inside the menu, labelled by `title`.

The submenu was the obvious alternative and it costs hover-open and hover-close timing, child positioning and clamping against the same container edge the parent clamps against, `ArrowRight` and `ArrowLeft` to enter and leave, and a rewrite of `focusAdjacentItem`, which assumes one flat list. Against that, the strip wins on four counts:

- **It is what the reference tools do with these eight.** No tool renders "Align left" as a text row; Miro, Figma and tldraw all draw align as a strip of icons, because the icon is a diagram of the action in a way a word is not.
- **ADR 0014's thresholds become a width change.** Six align glyphs appear at two top-level entities, the two distribute glyphs at three. As text rows that is the menu growing and shrinking by eight rows between selections, which is the height-jump objection this ticket was handed. As a strip it widens a row.
- **The glyph vocabulary is a road already taken.** ADR 0021 made a closed role vocabulary whose members each own a glyph, on the reasoning that a glyph beats a word for something judged by eye. Alignment is the same kind of thing.
- **The keyboard cost is smaller.** Left and Right within the strip, Up and Down to leave it. No open state, no timing, no second popover to position.

The accepted cost is that eight unlabelled glyphs are less discoverable than eight words, and this codebase has no tooltip component; `title` supplies the native one rather than building anything.

**The four z-order rows stay flat.** They ship flat today, Bring to Front is high-frequency, and moving already-shipped behaviour one hover deeper is a regression users feel. That leaves a single instance at eleven rows and a multi-selection at thirteen, shorter than Figma's object menu.

## The menu flips; it does not clamp

`MenuStyle` is `left: {X}px; top: {Y}px` with no bounds check and `.diagram-container` declares `overflow: hidden`, so **a right-click near the right or bottom edge cuts items off and makes them unreachable today.** This is a live defect, not a hypothetical, and ADR 0021 spotted the absence and told this decision to adopt its clamp rather than re-derive one.

This diverges, and ADR 0021's own reasoning is what licenses the divergence. It rejected flipping for three reasons: hysteresis at the boundary, it moves the side of the selection the user is reaching for, and it competes with the clamp on cases the clamp already handles. **All three depend on the bar being persistent and re-anchoring as the selection moves.** A context menu is placed once, on open, and dies on the next press. There is no boundary to oscillate across and no target to move out from under a reaching hand.

There is also a reason to prefer flipping that the bar never faces. **Clamping a menu slides it back over the pointer**, putting a row under the cursor so the next click activates something the user did not aim at. Flipping opens the menu away from the edge and keeps the anchor clear, which is why every platform menu on both operating systems does it.

So the menu **chooses its anchor corner so it opens away from the nearest edges**, top-left by default, and clamps only in the residual case where it fits nowhere, which on a canvas smaller than the menu is the honest outcome. The frame is `.diagram-container`, the box `ZoomPanTracker` already measures, so this adds no interop — the same reason ADR 0021's anchoring needed none.

The two surfaces therefore carry deliberately different placement rules, and the difference is persistence rather than taste: the persistent one clamps, the transient one flips.

## The dismissing press is consumed, and only inside the container

The existing dismissal listener is already a capture-phase `mousedown` handler on `document`, with a comment saying it fires before any handler on the clicked element. It moves to `pointerdown` to match ADR 0018's event, and it gains one behaviour: **while a menu is open, a press inside `.diagram-container` closes the menu and goes no further.**

Consuming it is the platform contract. Windows and macOS both swallow the dismissing click, and so do Miro and Figma. Doing it in the capture phase is also what keeps ADR 0018 untouched: the press never reaches the arbitration listener on `.diagram-canvas` at all, so none of its synchronous role-derived decisions need to learn that a menu exists.

**Scoping it to the container is the part that matters for an embeddable library.** Swallowing every outside press would mean the first click after a right-click does nothing on the host's own Save button — a bug report the host cannot diagnose, arriving from a component they think of as a canvas. ADR 0002 already draws the line between what the library owns and what the host does, so this is a second consumer of one principle rather than a new one.

The visible inconsistency it buys, and it is real: pressing on the canvas dismisses only, pressing on the host's toolbar dismisses and activates. The alternative's inconsistency is invisible and lands in someone else's code.

One accepted cost. With pan on the secondary button (ADR 0022), right-dragging while a menu is open produces a dismissal rather than a pan, so the user right-drags twice. Every menu on the machine behaves that way, and the alternative is a menu that does not reliably close.

## `author-content` stops being `Native` wholesale

ADR 0022 made a secondary press on the entire `author-content` role `Native`, reasoning that suppressing the browser's menu would cost an author's `<input>` its spellcheck and its own clipboard items. The reasoning is about editable content and the rule was written against the role, and that gap is larger than it looks.

Built-ins register through the same mechanism as any other component and ADR 0017's classification covers them, naming `StickyNote`'s `stopPropagation` workaround as something it deletes. So under ADR 0022 as written, **a right-click on the body of a sticky note gets the browser's menu and not the object menu** — the single most common object on a whiteboard, plus Text instances and any author component that fills its container. A sticky note outside edit mode is static text, so `Native` there defends a spellcheck menu with nothing to check.

**The exception narrows to where its reasoning bites: `Native` when the press target is editable, or when a text selection is live inside the content. Otherwise `author-content` classifies as `instance` for the secondary button and the object menu opens.** ADR 0018 already carries the editable-target predicate, so no new mechanism appears; this is one cell of ADR 0022's table.

A route did technically exist without this: every role but `author-content` opens the menu on a sub-threshold secondary release, so a selected instance's resize handles reach it. "Right-click the handle, not the shape" is not a thing anyone discovers, which is why it is not the answer.

What the narrowing does **not** cover is an author embedding an `<a>`, an `<img>`, a `<video>` or an `<audio>` element, where the browser's menu is genuinely the useful one and neither test catches it. That is left open rather than guessed at, because the honest options are a closed set of element kinds the framework recognises or an opt-out the author declares, and choosing between them is a contract decision rather than a menu one.

## The snap-to-grid guard is in the wrong place

`OnToggleSnapToGridPressed` returns early when `EnableSnapToGridShortcut` is false. That parameter exists, by its own comment, to let a host disable the `Ctrl+'` chord independently of the bindable `SnapToGrid` — a host turns it off because the chord collides with one of its own bindings, not because it wants snapping unavailable.

**The guard moves to the keydown call site**, leaving the public method ungated and usable from both surfaces. That also fixes a latent oddity rather than merely enabling a menu row: a host calling that public method directly today gets silently nothing, which cannot be what it wanted.

## What is deliberately not on either menu

**No keyboard shortcut hints, on any row.** ADR 0013's five clipboard chords, ADR 0011's `Ctrl+'` and ADR 0015's three `Shift`+digit bindings are decided, but ADR 0014's eight chords are not, nor is lock, nor unlock-all, nor the keyboard route to the menu itself. Rather than ship a menu where some rows explain themselves and others do not, hints belong entirely to the keyboard-parity decision, which inherits two things with them: any binding it decides has to reach the menu, or the menu becomes where shortcuts go to be forgotten; and a hint is the first thing in this library that must *render* a platform-dependent string, since `(event.ctrlKey || event.metaKey)` is right for matching but a row reading "Ctrl+D" is wrong on a Mac. ADR 0022 already flagged needing a platform check for a pointer modifier, so that fact has two consumers and should be established once. The strip costs nothing here: its chords ride on `title` and need no layout change when they arrive.

**No `WheelDeviceProfile` row**, although a canvas menu carrying board settings looks like its natural home. The line, stated because it is what makes snap-to-grid different rather than merely permitted: **the menu may flip state the library already has its own built-in route to flip, and it does not become the first route to a host-owned preference.** ADR 0011 gave snapping a bindable parameter *and* a chord. Nothing owns a route to the device profile, ADR 0019 assigned its persistence to the host, and a transient popover is a poor place to change a preference that persists somewhere the user cannot see.

**No z-order disambiguation item**, despite the reference-tool teardown assigning Figma's `Select layer` submenu to this decision by name. Handing it back is recorded here rather than left as a silent omission. It needs four things: `elementsFromPoint` plumbing, a highlight affordance reaching from a menu row onto board content, a decision about whether an instance has an identity a user can read — `DisplayName` lives on the registration, so three overlapping rectangles produce three rows reading "Rectangle" — and either the submenu machinery this ADR rejected or a menu whose length depends on how much happens to overlap. ADR 0022 meanwhile recorded press-through-with-a-modifier as the cheap route to the same hole, rejected only because Ctrl is contested with snapping and askable again once that lands. Two candidate answers to one question belong in one decision, and it is not this one.

One finding from that investigation is banked rather than discarded, because it dissolves ADR 0017's objection instead of accepting it. ADR 0017 rejected a C#-side ranked hit list because it would compete with the DOM's own order, so the user clicks the top thing and something beneath wins. That kills a `Board` scan over `Bounds` sorted by `ZIndex`; it does not kill the feature, because `document.elementsFromPoint` returns every element at a point in the DOM's own paint order, and ADR 0017's existing marker walk over that list yields the stack from the same authority that classified the press, with no geometry in C# at all.

**No property rows.** ADR 0021 gives those to the bar and hides the bar while the menu is open, so the two surfaces never compete for the pixels above a selection. Nothing to add here.

## What this amends and what it confirms

**ADR 0009's context-menu paragraph is filled in.** ADR 0022 already replaced it with the trigger semantics and stated that the contents belong here. This supplies them.

**ADR 0013 is amended in two places.** Its mechanism becomes per-surface: the keydown path keeps the DOM clipboard events, the menu uses `navigator.clipboard`. Its paste anchor gains the opening press point as a third case.

**ADR 0014's readability question is discharged**, by a strip rather than the submenu or the hard cap it anticipated, and its hidden-not-disabled rule is followed rather than revisited.

**ADR 0015's composition question is discharged**, and its three commands become one view section shared by both menus.

**ADR 0017 gains an unlock item and an unlock-all action.** Its panel route is untouched and still needs nothing new, so this is a third route rather than a hole being patched.

**ADR 0021 is diverged from once, deliberately and with its own reasoning.** Its clamp does not transfer to a transient surface. Everything else it decided about the two surfaces coexisting — the bar hiding while the menu is open, the menu dying on the next press, the bar not being a canvas press — is confirmed and used.

**ADR 0022 is amended in one cell.** `author-content` on the secondary button is `Native` only where the browser has something to offer.

**ADR 0018 is untouched.** Consuming the dismissing press in the capture phase means the press never reaches the arbitration listener, so its four synchronous role-derived decisions need no knowledge of the menu.

**ADR 0002 is not reopened.** The menu is canvas-rendered chrome, a category ADR 0021 named and this decision stays inside.

**Nothing here is board state.** No persisted menu position, no visibility flag, no envelope change. Snap-to-grid is already a bindable parameter and locking is already an entity field.

## Considered and rejected

- **Two menu components, one per context** — they share positioning, flipping, the roving-focus keyboard contract, dismissal and the whole theme token block, and differ only in which rows are eligible; and they are not even disjoint in content, since Paste and the view section appear on both.
- **A layout per context, six in all** — makes the sixth context, a locked entity reached by a secondary press, a hand-written case rather than a consequence of the rows that already know they cannot act on it.
- **Disabling rather than hiding ineligible rows** — a greyed row with no tooltip surface explains nothing, per ADR 0014 and ADR 0015. The height-jump cost that motivates the alternative is mostly the eight align rows, which the strip removes.
- **Omitting the clipboard rows entirely** — the smallest surface and no ADR 0013 amendment, but right-click-then-paste on a blank area is how content moves between two boards in every tool this map measures against, and it would leave the canvas menu carrying only settings.
- **Copy and Cut rows without Paste** — honest about the platform, and Figma's web build effectively ships this by degrading its own Paste row, but a menu offering Copy and not Paste reads as broken.
- **`document.execCommand('paste')` as the menu's read path** — probed; no engine allows a page to read the clipboard this way, and it fires no `paste` event either.
- **A permission prompt on every menu paste** — would have been a reason to drop the row, and the probe showed Chromium remembers the grant.
- **Sending a menu paste to the viewport centre**, which is ADR 0013 read literally — puts content halfway across the board from where the user right-clicked.
- **Anchoring a menu paste at the release point rather than the press point** — the same objection ADR 0022 raised for the menu's own position, and here the two can differ by more than the drag threshold, because the pointer travels to the row.
- **A submenu for align and distribute** — hover timing, a second popover to position and clamp against the same edge, `ArrowRight`/`ArrowLeft`, and a rewrite of the flat roving-focus helper, for eight actions every reference tool draws as icons.
- **A submenu for the four z-order rows** — shortens the top level, at the cost of burying already-shipped high-frequency behaviour one hover deeper.
- **A hard cap with an overflow row** — moves the readability problem behind a row labelled "More".
- **Clamping the menu, per ADR 0021** — its three arguments against flipping all rest on a persistent, re-anchoring surface, and clamping slides a transient menu back over the pointer so a row lands under the cursor.
- **Keeping the current unclamped, unflipped `left`/`top`** — the container clips, so items near an edge are already unreachable.
- **Swallowing every press outside the menu** — matches the platform contract exactly and makes the library eat presses on the host's own markup.
- **Not consuming the dismissing press at all** — no change to anything, and a press meant to dismiss also changes the selection and can arm a drag.
- **Treating a second secondary press as a pan rather than a dismissal** — special-cases one button out of the dismissal rule to save a gesture, on a surface whose whole job is to be transient.
- **Leaving `author-content` wholly `Native`, per ADR 0022** — no object menu on sticky notes or text, which is not an edge case but the most common content on the board.
- **Enumerating the element kinds that own their own menu** (`<a>`, `<video>`, and so on) as part of this decision — the framework guessing, wrong in both directions, and a contract question rather than a menu one.
- **A `WheelDeviceProfile` row** — makes a transient popover the first and only route to a preference whose persistence ADR 0019 assigned to the host.
- **Gating the snap-to-grid row on `EnableSnapToGridShortcut`** — that parameter disables a chord, not a capability.
- **Shortcut hints on the rows whose bindings are already decided** — a coherent rule, and it ships a menu where some rows explain themselves and others silently do not, on a table the keyboard-parity decision is about to reconcile anyway.
- **Figma's `Select layer` z-order submenu** — the teardown assigned it here; it needs a highlight affordance, a decision about instance identity, and the submenu machinery, while a modifier answers the same question far more cheaply once Ctrl is settled.
- **A `Board` scan over `Bounds` sorted by `ZIndex`** as the hit stack for that item — exactly the C#-side ranked list ADR 0017 rejected, and it disagrees with what the DOM classified the press as.
- **An `Edit` or `Add label` row for the double-click meanings** ADR 0018 folded into its press count — the port half belongs to the port affordance decision, and adding rows for gestures whose affordances are undecided commits to them early.
