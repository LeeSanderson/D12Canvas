# When `contextmenu` fires, and at what

Type: task
Status: resolved
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
- Finished 2026-10-05 by hand with the hand probe on Windows 11, real mouse and keyboard, same three builds. Logs deleted after reading.

## Answer

**Both of ADR 0047's facts hold on Windows, and the second one does not save the keyboard binding.** Real input, Windows 11:

| | Chromium 149 | Firefox 151 | WebKit 26.5 |
|---|---|---|---|
| Right press in A, into B, release | after `pointerup`, target B | after `pointerup`, target B | on press, target A |
| Right-click, no movement | after `pointerup`, target A | after `pointerup`, target A | on press, target A |
| `Shift+F10` | on keydown, focused element | on keydown, focused element | unmeasured |
| `Shift+F10`, keydown prevented | none | **still fires** | unmeasured |
| ContextMenu key | on keyup, focused element | on keyup, focused element | unmeasured |
| ContextMenu key, keydown prevented | **still fires** | **still fires** | unmeasured |

- **Windows ordering: confirmed for both engines that ship on Windows.** `contextmenu` follows `pointerup` and targets the release element, with `buttons=0`. The synthetic Firefox result, on press at A, was an automation artifact, as suspected. WebKit fires on press, which is the macOS behaviour; Playwright's Windows WebKit is not a shipping browser, so that column is a curiosity.
- **Keyboard: `contextmenu` still arrives after a prevented keydown in three of four cases.** Only Chromium's `Shift+F10` is suppressed. So ADR 0026's binding does not own the menu outright on either key, and ADR 0047's keyboard rule applies to both.
- **The ContextMenu key fires on keyup in both engines**, about 100 ms after the keydown ADR 0026 binds. By then the binding has opened the menu and `SelectionContextMenu` has focused its first item, so the late `contextmenu` lands on whatever holds focus at keyup. That is a second menu request the library has not accounted for, and it is now its own ticket, [One menu per keyboard menu request](64-one-menu-per-keyboard-menu-request.md).
- **WebKit's keyboard rows are unmeasured.** Neither a click nor `Tab` focused the probe's `<button>` in Playwright's WebKit, so every key landed on `body`, and no `contextmenu` fired with the prevent boxes on. Safari on macOS has neither key binding by convention, which ADR 0026 already notes.

Recorded on ADR 0047 and ADR 0026 by addendum.
