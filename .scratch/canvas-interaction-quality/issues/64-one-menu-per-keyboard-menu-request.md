# One menu per keyboard menu request

Type: grilling
Status: open
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
