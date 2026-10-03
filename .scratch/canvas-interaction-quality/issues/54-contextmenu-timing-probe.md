# When `contextmenu` fires, and at what

Type: task
Status: open
Blocked by:

## Question

Establish two browser facts with an `Interaction probe`, so ADR 0047's `Menu verdict` rests on measurement rather than reasoning:

- **Windows ordering.** On Windows, does `contextmenu` fire after `pointerup`, and is its target the element under the release point rather than the press point? Check Chromium, Firefox and WebKit where the pinned Playwright image allows, with a secondary press that moves 3 pixels between press and release onto a different element.
- **Keyboard menu requests.** Does `preventDefault` on the `Shift+F10` keydown stop the `contextmenu` event from firing, per engine? And does the ContextMenu key fire `contextmenu` on keydown or keyup, targeting the focused element?

Surfaced while resolving [When an author's content owns its own context menu](38-native-menu-inside-author-content.md). ADR 0047 stores the verdict at press because of the first fact, and runs the predicate on a keyboard `contextmenu`'s own target because of the second. If the first is wrong, the stored verdict is still correct and only the stated reason changes. If keyboard `contextmenu` never fires after a prevented keydown, ADR 0026's `Shift+F10` binding owns the menu outright and the keyboard rule in ADR 0047 applies only to the ContextMenu key.

Record the results on ADR 0047 by addendum.
