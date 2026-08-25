# Keyboard parity for new gestures

Type: grilling
Status: open
Blocked by: 01, 07, 09

## Question

Decide what keyboard access this effort's new gestures require, and reconcile the resulting shortcut table.

ADR 0010 established a full keyboard-accessible interaction model: reading-order tab stops with a `Group` collapsing to one stop, focus-follows-selection, zoom-relative arrow-key move, `Alt+Arrow` per-axis resize, `Ctrl+Tab` plus `Space` multi-select, and mouse-free placement and connector attachment. ADR 0011 then amended ADR 0009's shortcut table with `Ctrl+'` for snap-to-grid. Every shortcut passes an `isEditableTarget` guard so it does not fire during inline text editing.

This effort adds gestures that have no keyboard equivalent yet, and changes triggers that ADR 0010 assumed.

Decide:

- Which new capabilities need keyboard equivalents and which legitimately do not. Clipboard and select-all (09) plainly do. Align and distribute (12) plainly do. Alignment guides (11) are a during-drag affordance with no obvious keyboard form — decide whether keyboard nudging should snap at all.
- **The minimap (13) is interactive chrome with no keyboard form at all.** ADR 0015 gave it click-to-jump and rect-drag, and deliberately left keyboard access here. The likely answer is that it needs none — `Shift+1`/`Shift+2`/`Shift+0` already make navigation keyboard-complete, so the minimap is a redundant pointer surface rather than a gap, which would make it correctly non-focusable and `aria-hidden`. But that has to be *decided* rather than defaulted into, because it is the first interactive chrome in the library, and "mouse-only control" is precisely what ADR 0010 set out to eliminate. Whichever way it goes, ADR 0010's tab-stop model says nothing about chrome at all, which is the same gap the property bar hits below.
- The reconciled shortcut table, including collisions with the browser's own bindings and with what ADRs 0009/0010/0011 already claim. `Ctrl+C`/`Ctrl+V`/`Ctrl+D`/`Ctrl+A` are the obvious pressure points. **ADR 0015 already bound `Shift+1`/`Shift+2`/`Shift+0`** for the three viewport commands, so those rows are settled — but it bound them behind a *focus-within-the-canvas-container* guard rather than the table's `isEditableTarget`, which is the same stricter guard ADR 0013's clipboard rows use. That makes three separate guard regimes now live in one table, and reconciling them is this ticket's.
- How the context menu (10) is opened from the keyboard — the Menu key and `Shift+F10` are the conventions, and neither is handled today.
- **Whether menu rows display their shortcuts, which ticket 10 handed here rather than deciding.** The menu ships with no hints at all, so this owns adding them. Two things fall out of that. Every binding this ticket decides has to reach the menu, or the menu becomes the place shortcuts go to be forgotten. And a hint is the first thing in the library that must *render* a platform-dependent string: `(event.ctrlKey || event.metaKey)` is right for matching but a row reading "Ctrl+D" is wrong on a Mac, so this needs a one-time cached "is this an Apple platform" fact from JS. ADR 0022 already flagged needing a platform check for a pointer modifier, so that fact has two consumers and should be established once. Note that ADR 0014's eight align and distribute actions render as a glyph strip rather than eight rows, so their chords ride on a `title` attribute and need no layout change when they arrive.
- How the selection-anchored property bar (08) participates in the focus order, and whether reaching it is a tab stop, a shortcut, or both.
- What the port affordance decision (06) means for keyboard users. If ports become visible on focus rather than hover, that is arguably an accessibility *improvement* over the current hover-dependent affordance — confirm and record it, since ADR 0010's mouse-free connector attachment already depends on ports being reachable without a pointer.
- Whether the press-to-drag change (07) has any keyboard consequence, or is purely a pointer concern.

Amends ADR 0010 and whatever shortcut table ends up superseding ADR 0009's.

**Update from ADR 0024 (ticket 11 resolved):** bullet 1's question is now two questions, and one of them has a deadline attached.

**The nudge *step*.** `Snap-to-grid` is **on by default** as of ADR 0024, so a board's content sits on grid lines out of the box while `NudgeCommand` still steps by a zoom-relative amount that has nothing to do with the grid. A single arrow press therefore takes a grid-aligned entity *off* the grid by default, where before it only did so for the minority who opted in. tldraw makes the nudge step become the grid size whenever grid mode is on, which is the obvious candidate. This is now the more urgent half.

**Whether nudging snaps to *objects* at all.** ADR 0024 gave keyboard nudge nothing, and deliberately: ADR 0020 states outright that keyboard work must not reach for the `Gesture preview`, because a keypress's result is fully determined the moment it is pressed and there is nothing provisional to show, whereas `NudgeCommand` writes through and extends in place. So an object snap on a nudge cannot be built the way the pointer's is. Decide whether it should exist by some other route or not at all, noting that an `Alignment guide` with no gesture to live inside would have to appear and expire on a timer, which nothing in this library currently does.

Two smaller inheritances. ADR 0024 declined a chord for the object-snapping toggle, so the table gains a `Ctrl+'` sibling in the context menu but **not** on the keyboard, and bullet 6's menu-hint work should show the menu row without a hint rather than inventing one. And the platform fact bullet 6 wants established once now has its second consumer built rather than merely anticipated: ADR 0024 puts a real platform check on the pointer path, for the macOS `Ctrl`+click secondary press.
