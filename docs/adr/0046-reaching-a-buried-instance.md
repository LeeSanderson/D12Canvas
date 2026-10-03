# A click inside the selection box selects what is beneath it, and Alt+click cycles down the stack

An instance can be hidden from the pointer in two different ways, and each gets its own rule. An instance covered only by the multi-selection's box is reached by a plain click: a release from `pointing` on `selection-bounds` treats the topmost entity beneath the box as the pressed entity. An instance covered by another instance is reached by **Alt+click**, which selects the next entity down the `Hit stack` at the press point. There is no menu route and no instance naming.

The keyboard never had this hole. ADR 0010's tab stops follow reading order, not paint order, so Tab already lands on a covered instance. This decision is about the pointer only.

## Two holes

**Under the selection box.** ADR 0017 made `.selection-bounding-box` a solid hit target, so a press anywhere inside a multi-selection's bounds classifies as `selection-bounds` and an unselected instance inside those bounds cannot be hit. Nothing visible covers it. ADR 0022's release table already says a release from `pointing` on `selection-bounds` "collapses to the pressed entity", but `selection-bounds` carries no entity, so the cell had nothing to collapse to.

**Under another instance.** One instance paints over another. This is real occlusion, and it is what ADR 0022 recorded as a hole and ADR 0023 handed on.

Fixing the first with the second's route would make a user hold a modifier to click something they can see. So they are separate.

## The selection box: the entity beneath is the pressed entity, at release only

When a press classifies as `selection-bounds`, the listener also records the topmost entry of the `Hit stack` beneath the box. That entity takes the place of the pressed entity in ADR 0022's release table:

- a plain release from `pointing` collapses the selection to it
- a `Shift` release from `pointing` toggles it, which is how a user adds a boxed-in instance to a selection without redrawing the marquee

Nothing changes at press. A drag from inside the box still moves the existing selection and never picks up the entity beneath, because ADR 0022's member case has to defer or a multi-selection dies the instant you drag it. If nothing is beneath the box at that point, the release collapses nothing and the selection stays.

tldraw does the same: a click inside its selection bounds selects the shape under the pointer.

## Alt+click goes one deeper

ADR 0042 binds Alt only on a `MoveSelection` that has crossed the threshold. An Alt release from `pointing` had no meaning, so Alt+click is free and pre-empts nothing: the click and the clone drag are the pair ADR 0018's threshold already separates. Once a press crosses the threshold it is a `Clone drag` exactly as before.

**Cycling reads the selection from before the press.** If the `Selection snapshot` is a single entity that appears in the `Hit stack` at the press point, Alt+click selects the next entry below it, wrapping from the bottom to the top. Otherwise it selects the entry below the top. With T over M over B, successive Alt+clicks select M, B, T, M. Nothing is stored between clicks: the stack is read fresh at each press and the position comes from the snapshot, so a board that changed between clicks cannot leave a stale cursor behind.

The snapshot is needed because ADR 0022 collapses the selection to a non-member at press, which an Alt-drag on an unselected instance needs so the clone has something to copy. By release the top entry is already selected, and cycling from the live selection would always land one below it. ADR 0031's snapshot already holds the selection from the instant before the press, so this reuses it rather than adding a field.

A stack of one wraps to itself and a stack of none is bare canvas, so both behave as a plain click.

**`Shift`+Alt+click is Alt+click.** Cycling is defined against a single selected entity, and an additive version would need a second rule for where to go next from a multi-selection. The keyboard covers that case with `Ctrl+Tab` and `Space`. The cost is that no pointer route selects two instances both buried at one point.

**Alt on `author-content`.** `StickyNote` and `Text` render their `<textarea>` only while editing, so their bodies at rest classify as `instance` and need nothing. An author component that is mostly a control would otherwise swallow the Alt+click as `Native`. So with Alt held, the primary cell for `author-content` uses the predicate ADR 0023 wrote for the secondary button: `Native` when the target is editable or holds a live text selection, otherwise `instance`. The listener holds the modifiers and the predicate at press, so this adds no synchronous decision. A side effect is that Alt+click on an author's `<a href>` no longer triggers the browser's download.

## The hit stack

Both rules read the same list. `document.elementsFromPoint` at the press point returns elements in paint order, topmost first. ADR 0017's marker walk runs over each, and each result is resolved the way a plain press resolves:

- **through `EffectiveSelectionId`**, so a member of an unentered group becomes its group and a member of the `Entered group` stays itself, with duplicates collapsed. Alt+click never reaches inside a group. Reaching inside is entering (ADR 0044).
- **locked entities skipped**, since a primary press never hits one
- **the selection box skipped**, since it is chrome over content and not an entry
- **edges kept**, in the order the DOM returns them

The order is paint order, which is what the user sees. ADR 0017 fixed hit order so instances always beat edges, so for an instance sent behind an edge the top of the stack differs from what a plain click hits. Wrapping still reaches every entry, so this is accepted rather than patched.

### Why this does not reopen ADR 0017's rejection

ADR 0017 rejected `elementsFromPoint` *for classification*: a second query that can disagree with event dispatch leaves two precedence systems to keep in agreement. Here the role, and every synchronous decision derived from it, still comes from the event's own target. The stack only supplies a click outcome after the gesture is known to be a click. ADR 0017 also rejected a C#-side ranked list over `Bounds` sorted by `ZIndex`, and that stays rejected: the stack is read from the DOM, the same authority that painted the board, so there is no geometry in C#.

It is one call per press on `selection-bounds` and per Alt press, made at press, never per frame.

## Platform

ADR 0042's `Interaction probe` for Alt on Windows gains a second case. It already asks whether releasing Alt after a drag activates the browser's menu bar. It now also asks the same after an Alt+click with no drag, and whether the gesture must prevent the default on Alt's `keyup`.

The Linux window managers that bind Alt+press to window moves, KDE Plasma by default, take the Alt+click before the page sees it, exactly as ADR 0042 recorded for the drag. The keyboard is the way round it.

## What this amends

**ADR 0017 is amended by addendum.** `elementsFromPoint` gains a use beside classification, as the source of the `Hit stack`, and the role is still resolved from the event target alone.

**ADR 0022 is amended in two places.** The `selection-bounds` row of the release table gains its pressed entity. And the primary cell for `author-content` gains an Alt case, using ADR 0023's predicate.

**ADR 0042 is amended by addendum.** Alt gains a meaning on a release from `pointing`, and its probe gains a case.

**ADR 0023's rejection of `Select layer` is confirmed.** It rejected the row expecting a modifier to answer the question more cheaply, and one now does.

**ADR 0001 and ADR 0003 are untouched.** Instances gain no readable identity.

## Considered and rejected

- **One rule for both holes**: a user would need a modifier to click an instance nothing visible covers.
- **A `Select layer` menu row**: shows the user what is down there, but needs submenu machinery and readable instance identity. Three overlapping rectangles all read "Rectangle", because `DisplayName` is on the registration, and fixing that touches ADR 0001 and ADR 0003 or needs a highlight affordance that does not exist.
- **Both the menu and the modifier**: two routes to one question, the objection ADR 0015 and ADR 0022 each used to reject a feature.
- **A repeat press cycling down the stack**: ADR 0044 took press count 2 on grouped content for entering a group, so cycling would start at press count 3 or apply only where nothing at the point is grouped. The same gesture would mean different things depending on what is beneath.
- **Ctrl or Cmd+click**: on macOS Ctrl+primary is the system secondary click, which ADR 0024 honours. A modifier that works on Windows and silently does not on macOS is worse than none.
- **Building nothing, relying on Tab and send-to-back**: a user who can see a corner of something should not have to restack the board to click it.
- **Alt+click always selecting the entry below the top**: the third entry down and below can never be reached by pointer.
- **A remembered cycle cursor**: state that goes stale when the board changes between clicks, where the snapshot gives the position for free.
- **Cycling from the live selection**: the press has already collapsed it to the top entry, so cycling would never get past the second.
- **Additive cycling under `Shift`+Alt**: a second rule for a rare case the keyboard already covers.
- **Leaving Alt on `author-content` as `Native`**: Alt+click on top of an author's control reaches nothing beneath it.
- **Reordering the stack to match hit order**: a second ordering rule to patch one case that wrapping already reaches.

**Amended by ADR 0055:** the caveat that the hit stack's top entry differs from a plain click for an instance sent behind an edge no longer applies. Edges always paint beneath instances, so paint order and ADR 0017's hit order agree, and that case cannot occur.
