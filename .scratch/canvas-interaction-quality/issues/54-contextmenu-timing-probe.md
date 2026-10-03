# When `contextmenu` fires, and at what

Type: task
Status: parked
Blocked by:

## Question

Establish two browser facts with an `Interaction probe`, so ADR 0047's `Menu verdict` rests on measurement rather than reasoning:

- **Windows ordering.** On Windows, does `contextmenu` fire after `pointerup`, and is its target the element under the release point rather than the press point? Check Chromium, Firefox and WebKit where the pinned Playwright image allows, with a secondary press that moves 3 pixels between press and release onto a different element.
- **Keyboard menu requests.** Does `preventDefault` on the `Shift+F10` keydown stop the `contextmenu` event from firing, per engine? And does the ContextMenu key fire `contextmenu` on keydown or keyup, targeting the focused element?

Surfaced while resolving [When an author's content owns its own context menu](38-native-menu-inside-author-content.md). ADR 0047 stores the verdict at press because of the first fact, and runs the predicate on a keyboard `contextmenu`'s own target because of the second. If the first is wrong, the stored verdict is still correct and only the stated reason changes. If keyboard `contextmenu` never fires after a prevented keydown, ADR 0026's `Shift+F10` binding owns the menu outright and the keyboard rule in ADR 0047 applies only to the ContextMenu key.

Record the results on ADR 0047 by addendum.

## Comments

- Partly measured 2026-10-03 with [the probe page](../assets/54-contextmenu-probe.html), driven by [an automated script](../assets/54-contextmenu-auto-probe.js) on Windows 11 against Playwright 1.61's builds (Chromium 149, Firefox 151, WebKit 26.5). Synthetic input only.

  | | Chromium | Firefox, synthetic | WebKit, synthetic |
  |---|---|---|---|
  | Right press in A, 3 px into B, release | `contextmenu` after `pointerup`, target B | on press, target A | on press, target A |
  | `Shift+F10` | `contextmenu` on the focused element, before keyup | none | none |
  | `Shift+F10`, keydown prevented | none | none | none |
  | ContextMenu key | `contextmenu` on **keyup**, focused element | none | none |
  | ContextMenu key, keydown prevented | **still fires** on keyup | none | none |

  The Chromium column is credible. Blink makes these decisions in the renderer, which CDP input reaches. It confirms both of ADR 0047's facts for Blink. It also adds one: preventing the ContextMenu keydown does not stop its `contextmenu`, so on Windows that key cannot be suppressed at keydown.

  The Firefox and WebKit columns are not. Playwright's input for those engines skips the OS widget layer, and that layer turns a key into `contextmenu` and applies Firefox's Windows pref `ui.context_menus.after_mouseup`. "None" and "on press" there are probably automation artifacts.
- Parked 2026-10-03, dev on mobile. To finish from a desktop: set `Status: open`, run `node ../assets/54-contextmenu-hand-probe.js` (it opens headed Chromium, Firefox and WebKit on the probe page and writes `54-hand-<engine>.log` beside itself every second), and in each window do the three steps on the page. Real input in those windows goes through the OS, so the logs answer the Firefox and WebKit columns. Then record the answer and the ADR 0047 addendum. Delete the `.log` files before committing.
