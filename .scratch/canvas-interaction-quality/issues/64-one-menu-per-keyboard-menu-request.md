# One menu per keyboard menu request

Type: grilling
Status: resolved
Blocked by:

## Question

One keyboard menu request must open exactly one menu. Decide which handler acts on it.

[When `contextmenu` fires, and at what](54-contextmenu-timing-probe.md) found that ADR 0026's keydown binding and the browser's own `contextmenu` both fire for one keypress on Windows:

- `Shift+F10` in Firefox: `contextmenu` arrives about 1 ms after the keydown, even with the keydown prevented. Chromium suppresses it.
- The ContextMenu key in Chromium and Firefox: `contextmenu` arrives on keyup, about 100 ms later, prevented or not. By then the binding has opened the menu and `SelectionContextMenu` has focused its first item, so the event targets a menu item. ADR 0047's five rules would then run on that item, and ADR 0023's dismissal listener may see it too.

Options to weigh, without treating this list as the whole space:

- The keydown binding opens the menu, and a keyboard `contextmenu` that follows it is consumed. What says "follows it": a flag cleared on keyup, the event's `button` (Chromium sends `-1` and Firefox `0` for a keyboard `contextmenu`, while a mouse sends `2`), or the target being inside the open menu.
- Drop the keydown binding and let the `contextmenu` event open the menu on both keys, using ADR 0047's keyboard rule. It works only if every engine fires `contextmenu` for both keys when the keydown is left alone, which Chromium and Firefox do on Windows. macOS needs checking first. ADR 0026 calls `Shift+F10` the binding that serves both platforms, and `Shift+F10` there may not fire `contextmenu` at all.
- Whether the ContextMenu key should act on keydown at all, given that every Windows engine measured treats keyup as the request.

ADR 0026's `event.code` matching, its focus guard and its focus-return rule all touch the answer. Any rule about the browser here needs an `Interaction probe` driving the real key path (ADR 0025), since a test that calls the handler can't see the second event.

## Answer

**The keydown owns the request and writes ADR 0047's `Menu verdict`, and the browser's trailing `contextmenu` uses it up.** Recorded as [ADR 0067](../../../docs/adr/0067-one-menu-per-keyboard-menu-request.md), with addenda on ADRs 0047, 0026 and 0022 and an updated `Menu verdict` entry in `CONTEXT.md`.

- **On `browser`** the keydown is left alone and the native menu shows. **On `canvas`** the keydown is prevented, the menu opens, and the next `contextmenu`, whether 1 ms later in Firefox or on keyup for the ContextMenu key, is prevented without running any rules. It never classifies the menu item it now targets.
- **The slot is written and cleared in capture phase.** A capture-phase `window` `keydown` listener writes on a menu chord and clears on every other key. Every non-secondary `pointerdown` clears it. Repeats do neither. Without this, Chromium's unused `Shift+F10` verdict outlives itself, and a later `Shift+F10` inside a sticky note editor, whose keydown the editor stops, gets no menu at all. Clearing on `keyup` was rejected because the order of the ContextMenu key's `keyup` and `contextmenu` is unmeasured.
- **The ContextMenu key acts on keydown**, like every other row. Windows' keyup convention is accepted as a 100 ms difference.
- **Outside `author-content`** the keydown uses rule 2 alone: editable or live selection gives `browser`, anything else in the container gives `canvas`, and focus outside the container gives no verdict. Rule 2 replaces the row's typing guard. Focus returns to the element focused at the keydown.
- **Dropping the keydown binding** for `contextmenu` alone was rejected because macOS has no keyboard menu convention and the menu is the guaranteed unlock route. A platform split was rejected for the same reason ticket 56 shipped without one.
- **Verified by three Chromium `Interaction probe`s**: the ContextMenu key on an instance, the ContextMenu key in an editing sticky note, and the stale-verdict sequence. There is no Firefox probe, because Playwright's Firefox produces no second event, and no probe dispatches a synthetic `contextmenu`.
