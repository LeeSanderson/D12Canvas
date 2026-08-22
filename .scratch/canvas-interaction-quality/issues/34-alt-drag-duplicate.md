# Alt-drag to duplicate

Type: grilling
Status: open
Blocked by: 31

## Question

Decide whether holding Alt on a drag duplicates the dragged selection instead of moving it, and if so, what that gesture commits.

Surfaced while resolving [Right-button semantics and press-to-drag](07-right-button-and-press-to-drag-semantics.md). It looked like a row in that ticket's button-and-modifier table and turned out not to be one, which is why it is here instead: the question is not *which gesture a press claims* but *what that gesture writes at release*.

Ticket 03 found it in all four reference tools, with the same binding each time (tldraw clone-on-drag, Excalidraw "Duplicate — Ctrl/Cmd+D, Alt+drag", Miro "duplicate — Alt + drag", Figma the same). It is the fastest route from one shape to several, and ADR 0013's `Ctrl+D` plus ticket 31's chaining only partly covers the same ground: `Ctrl+D` cannot place the copy where the pointer is.

Alt is currently unbound on the pointer. ADR 0010 uses `Alt+Arrow` for keyboard resize, which does not collide. Ticket 03 noted Alt is doubly booked in every reference tool (duplicate-on-drag and resize-from-centre) and unambiguous because the two attach to different gestures rather than different targets.

Decide:

- **Whether it exists at all.** It is not in the seed's ten defects, and ADR 0013 already gives duplication a keyboard route.
- **Whether it is a ninth pointer gesture or a behaviour change inside `MoveSelection`.** ADR 0018 fixed a closed set of eight and forbids a gesture's identity changing mid-press. A clone that is decided at press and never changes is a `MoveSelection` variant; one that can be toggled mid-drag is either a ninth member or a mid-press identity change the ADR does not permit. Ticket 29 raised exactly this fork and could not resolve it without knowing whether the modifier is bound.
- **What it commits, against ADR 0020's invariant.** No pointer gesture creates a command before release, and the `Gesture preview` is written back verbatim on commit. A clone therefore has to preview *new* entities that do not exist in `Board` yet, which the preview's two typed slots (bounds overrides, one pending edge line) cannot currently express. This is the crux: either the preview gains a third slot or the clone previews as a move of the originals and materialises the copies only at release, which shows the user the wrong thing for the whole drag.
- **Whether Alt is latched or live**, and if live, whether undo survives it. tldraw toggles the clone on and off mid-drag and rewinds history to a mark to keep the undo stack clean. Ticket 29 states plainly that this "would be a genuine addition to the history model rather than a use of it" — ADR 0007 has no mark-and-rewind. Latching Alt at press avoids the whole problem and is what Excalidraw does with its own latched modifier, for a related reason.
- **What the copies inherit**, and whether ADR 0013's five id-regeneration references (including the missable `PortDef.Id`) and its interior-edge closure rule apply unchanged. If they do, this is a `CompositeCommand` of existing primitives with no new command type, exactly as paste and duplicate already are.
- **How it composes with ticket 31's remembered offset.** Duplicate chaining tracks an offset from the last duplicate; a drag sets the offset explicitly, so decide whether an Alt-drag seeds or resets that chain.

Blocked by ticket 31, which owns the duplicate-placement model this has to agree with. Blocks [Latched-versus-live modifier semantics](29-latched-versus-live-modifiers.md), which cannot enumerate Alt's behaviour before knowing whether Alt does anything.
