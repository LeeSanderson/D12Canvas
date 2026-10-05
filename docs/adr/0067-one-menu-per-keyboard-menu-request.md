# The menu keydown writes the menu verdict, and the browser's trailing `contextmenu` uses it up

ADR 0026 binds `Shift+F10` and the ContextMenu key to open the menu on keydown. ADR 0047 has a keyboard `contextmenu` run its five rules on the focused element. Ticket 54 measured that on Windows both happen for one keypress. Firefox fires `contextmenu` about 1 ms after a prevented `Shift+F10` keydown. Chromium and Firefox both fire it on keyup for the ContextMenu key, prevented or not, by which time the binding has opened the menu and focus is on its first item. Only Chromium's prevented `Shift+F10` sends nothing.

**The keydown owns the request. It computes the `Menu verdict` and stores it in ADR 0047's slot, the same slot a secondary press writes. The first `contextmenu` after it uses the verdict up and runs no rules of its own.**

- **`browser`**: the keydown is left alone and opens nothing. The browser's `contextmenu` follows, uses up `browser`, and the native menu shows.
- **`canvas`**: the keydown is prevented and the menu opens, per ADR 0026's selection split. Whichever `contextmenu` follows uses up `canvas` and is prevented. It is never classified, so it never runs the rules on the menu item it now targets.
- **No `contextmenu` follows** (Chromium's prevented `Shift+F10`, and probably macOS): the verdict stays in the slot until the next writer replaces it, as the next section makes certain.

A `contextmenu` with no press or menu keydown in front of it keeps ADR 0047's fallback and runs the rules on its own target.

## The slot is written and cleared in capture phase

ADR 0047 lets only a secondary press replace the slot. A keyboard verdict needs more than that. In Chromium, `Shift+F10` on an instance stores `canvas` and nothing uses it up. If the user then edits a sticky note and presses `Shift+F10` for spellcheck, `StickyNote.razor` stops the keydown's propagation, so a bubbling listener never sees it, and the browser's `contextmenu` uses up the stale `canvas`. The user gets no menu at all.

So the slot has these writers:

- **A `window` `keydown` listener in capture phase**, which runs before any author's or built-in's `stopPropagation`. A menu chord writes its verdict. Any other key clears the slot. A repeat keydown (`event.repeat`) does neither, so a held key cannot erase its own verdict before its keyup `contextmenu`.
- **Every `pointerdown`.** A secondary press writes its verdict, and any other button clears the slot. A second button pressed during a chord fires `pointermove`, not `pointerdown`, so this cannot wipe a live secondary press's verdict before its `contextmenu`.

The capture listener only writes the verdict. Opening the menu stays in ADR 0026's guarded binding, which is why a keydown the editor stops still records `browser`.

Clearing on the menu key's `keyup` was rejected. Ticket 54 says the ContextMenu key's `contextmenu` fires "on keyup", but not whether before or after the `keyup` DOM event, and the wrong guess breaks that key in both engines. It would also leave the `stopPropagation` case unfixed.

## What the keydown classifies

The keydown classifies whatever holds focus, and ADR 0026's guard admits anything inside `.diagram-container`. ADR 0047's rules are written for `author-content`, so the keydown verdict is:

- **Focus inside an instance's `author-content`**: the five rules, unchanged.
- **Focus elsewhere in the container**: rule 2 alone. An editable element or a live text selection gives `browser`, and everything else gives `canvas`. A property bar text input keeps its native menu, and a bar button, an instance tab stop or an edge stop opens the object or canvas menu.
- **Focus outside the container**: no verdict. The slot clears, the guard rejects the binding, and the host page's `contextmenu` never reaches the canvas listener.

Rule 2 does the job of the per-row typing guard for this row, so the menu row carries no `isEditableTarget` check of its own.

A `canvas` verdict whose row is a no-op in the current state, such as during port placement or a live pointer gesture, still prevents the keydown and still uses up the trailing `contextmenu`. That is ADR 0026's Firefox rule: a row past the focus and typing guards calls `preventDefault` even when its state guard does nothing.

## The ContextMenu key acts on keydown

Windows treats keyup as the request for this key, and every engine measured follows it. The binding still acts on keydown, like `Shift+F10` and every other row in ADR 0026's table. The menu opens about 100 ms sooner, which no user reads as a breach of convention. Acting on keyup would put the binding and the browser's `contextmenu` on the same keystroke in an order nobody has measured.

## Focus return

ADR 0026 returns focus to "the tab stop that opened the menu". With the keyboard verdict defined for anything in the container, that becomes **the element focused at the keydown**. `Shift+F10` on a property bar button opens the object menu, and closing it puts focus back on that button.

## Verification

Three `Interaction probe`s, Chromium only, driving the real key path per ADR 0025. Ticket 54's synthetic Chromium column matched real input on every keyboard row, including the ContextMenu key's `contextmenu` on keyup after a prevented keydown. Each probe reads `event.defaultPrevented` in a `contextmenu` listener registered last on `window`, because a native menu cannot be seen in the DOM.

1. The ContextMenu key on a focused instance: exactly one object menu opens, focus is on its first item, and the trailing `contextmenu` is prevented.
2. The ContextMenu key in an editing sticky note: no object menu opens, and the `contextmenu` is not prevented.
3. `Shift+F10` on an instance, so Chromium leaves a `canvas` verdict unused, then `Escape`, then editing a sticky note, then the ContextMenu key: the `contextmenu` is not prevented.

There is no Firefox probe. Playwright's Firefox input skips the OS layer that turns a key into `contextmenu`, so it produces no second event to test against. The handling is the same whether the trailing event comes 1 ms or 100 ms later, and Firefox's sequence rests on ticket 54's hand measurement. No probe dispatches a synthetic `contextmenu`, because a test that supplies the browser's step can only prove the C#.

## What this amends

**ADR 0047 is amended by addendum.** The slot has two more writers, the menu keydown and every non-secondary `pointerdown`, and it is cleared by every other key. Its keyboard rule becomes the fallback for a `contextmenu` with no request in front of it.

**ADR 0026 is amended by addendum.** The menu row's verdict comes from the keydown, the row has no separate typing guard, and focus returns to the element focused at the keydown.

**ADR 0022 is amended by addendum.** Outside `author-content`, a keyboard menu request classifies by rule 2 alone. The pointer path is unchanged: the secondary button still ignores every role but `author-content`.

## Considered and rejected

- **Only `contextmenu` opens the menu, and the keydown binding goes.** This needs one classifier and handles Windows in both engines. But ADR 0026 notes macOS has no keyboard menu convention, and `Shift+F10` there may fire no `contextmenu`. The menu is the library's guaranteed unlock route on a host without a property panel, so losing it on a Mac reopens ADR 0017's locked-forever hole, and no Mac is available to check.
- **A platform split**: keydown on macOS, `contextmenu` elsewhere. It needs reliable platform detection, and [Chord survival on macOS and in Firefox](../../.scratch/canvas-interaction-quality/issues/56-chord-survival-macos-firefox.md) has just shipped with no split.
- **Telling a keyboard `contextmenu` from a mouse one by `button`.** Chromium sends `-1` and Firefox `0`, against `2` for a mouse. That is an engine detail to depend on, where the slot already says what came before.
- **Treating a `contextmenu` that targets the open menu as the follow-up.** This works for the ContextMenu key and fails for Firefox's `Shift+F10`, whose `contextmenu` arrives before focus has moved.
- **Clearing on the menu key's `keyup`**: depends on an unmeasured order, and misses the `stopPropagation` case.
- **Acting on the ContextMenu key's keyup**: the same unmeasured order, for a 100 ms difference.
