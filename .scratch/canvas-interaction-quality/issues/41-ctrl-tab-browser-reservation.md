# Whether Ctrl+Tab and Ctrl+Arrow survive the browser

Type: task
Status: open

## Question

Establish whether ADR 0010's `Ctrl+Tab` multi-select binding and ADR 0030's `Ctrl+Arrow` quick-create binding reach the page at all, and report what each browser does with each.

ADR 0010 rejected `Shift+Tab` and `Alt+Tab` because both are "captured by browser/OS convention before a page-level handler would ever see them", and chose `Ctrl+Tab` instead. That rejection appears to apply to its own choice. `Ctrl+Tab` is the tab-switching chord in Chrome, Edge and Firefox, handled above the page. And because every binding in ADR 0026's table accepts `metaKey` alongside `ctrlKey`, the Mac reading of the row is `Cmd+Tab` — the OS application switcher, which never reaches the browser at all.

Nothing in the repo can see this. `DiagramCanvasCtrlTabSpaceMultiSelectTests` calls `OnCtrlTabPressed()` directly, so it proves the C# is correct and says nothing about whether the chord arrives — ADR 0025's plumbing-not-magnitudes rule pointing at a hole rather than confirming a fix.

**A `task` rather than a `research` or `prototype` ticket, and the reason bounds ADR 0025.** The observable is whether the *browser* switches tabs, which is invisible from inside the page: Playwright drives the page, not the browser UI, so a probe would confirm the handler fires and miss the failure entirely. ADR 0025 bounded Playwright's reach at device physics; this is a second boundary, browser-chrome-level bindings, and it needs a human pressing keys.

For each of Chrome, Firefox and Safari, on Windows and macOS as available, with a board open and focus on an instance tab stop:

- Does `keydown` for `Ctrl+Tab` reach the page listener at all?
- Does `preventDefault()` on it suppress the browser's tab switch?
- Does the browser switch tabs regardless?
- On macOS, the same three for `Cmd+Tab`, and separately for `Ctrl+Tab`.

**Widened by ADR 0030, which binds `Ctrl+Arrow` to quick-create and hits the same wall on the same map.** This ticket's own replacement-candidate list already names it: "`Ctrl+Arrow` is Spaces and Mission Control on macOS". The chord is contested in both of its readings there, since every binding accepts `metaKey` alongside `ctrlKey` — `Cmd+Left` and `Cmd+Right` are back and forward in Chrome and Safari, while `Ctrl+Left`, `Ctrl+Right` and `Ctrl+Up` are Mission Control at the OS level, which outranks the browser. Excalidraw ships `Ctrl/Cmd+Arrow` for this gesture regardless, so its own binding is at best half-working on macOS, and that is worth confirming rather than assuming.

It is folded in here rather than given its own ticket because it is the same measurement, in the same browsers, on the same day, and it is one more row in a table this ticket is already building. So for each of the four arrows, per engine and platform:

- Does `keydown` for `Ctrl+Arrow` reach the page listener?
- Does `preventDefault()` suppress the browser's own action, where the browser has one?
- Does the OS act regardless, and is that visible as the page never seeing the event at all?
- On macOS, the same three for `Cmd+Arrow`.

Note the asymmetry worth capturing rather than averaging away: `Cmd+Up` and `Cmd+Down` are not browser navigation, so the vertical pair may survive where the horizontal pair does not. A chord that works for two directions out of four is a worse outcome than one that fails cleanly, and ADR 0030 should hear about it either way.

Record what happened per engine rather than a verdict. If the binding is dead in any engine the map cares about, **do not treat the fix as a key change**: every candidate replacement is reserved somewhere — `Ctrl+Shift+Tab` is the reverse tab switch, `Ctrl+Arrow` is Spaces and Mission Control on macOS, `Ctrl+Space` is IME switching on Windows, and the `F6` family is browser chrome. If none is free, the fix is reopening ADR 0010's decision to weld focus to selection, since "move focus without selecting" needs a chord only because selection follows focus by default. That is a design question and should surface as its own ticket rather than being decided here.

Amends ADR 0010's multi-select section and ADR 0026's table row if `Ctrl+Tab` is confirmed dead, and ADR 0030 plus ADR 0026's new row if `Ctrl+Arrow` is. The two verdicts are independent: one chord failing says nothing about the other, and ADR 0030 deliberately declined to invent a macOS-specific replacement ahead of this measurement, so a clean failure there is an answer rather than a problem.
