# Whether Ctrl+Tab survives the browser

Type: task
Status: open

## Question

Establish whether ADR 0010's `Ctrl+Tab` multi-select binding reaches the page at all, and report what each browser does with it.

ADR 0010 rejected `Shift+Tab` and `Alt+Tab` because both are "captured by browser/OS convention before a page-level handler would ever see them", and chose `Ctrl+Tab` instead. That rejection appears to apply to its own choice. `Ctrl+Tab` is the tab-switching chord in Chrome, Edge and Firefox, handled above the page. And because every binding in ADR 0026's table accepts `metaKey` alongside `ctrlKey`, the Mac reading of the row is `Cmd+Tab` — the OS application switcher, which never reaches the browser at all.

Nothing in the repo can see this. `DiagramCanvasCtrlTabSpaceMultiSelectTests` calls `OnCtrlTabPressed()` directly, so it proves the C# is correct and says nothing about whether the chord arrives — ADR 0025's plumbing-not-magnitudes rule pointing at a hole rather than confirming a fix.

**A `task` rather than a `research` or `prototype` ticket, and the reason bounds ADR 0025.** The observable is whether the *browser* switches tabs, which is invisible from inside the page: Playwright drives the page, not the browser UI, so a probe would confirm the handler fires and miss the failure entirely. ADR 0025 bounded Playwright's reach at device physics; this is a second boundary, browser-chrome-level bindings, and it needs a human pressing keys.

For each of Chrome, Firefox and Safari, on Windows and macOS as available, with a board open and focus on an instance tab stop:

- Does `keydown` for `Ctrl+Tab` reach the page listener at all?
- Does `preventDefault()` on it suppress the browser's tab switch?
- Does the browser switch tabs regardless?
- On macOS, the same three for `Cmd+Tab`, and separately for `Ctrl+Tab`.

Record what happened per engine rather than a verdict. If the binding is dead in any engine the map cares about, **do not treat the fix as a key change**: every candidate replacement is reserved somewhere — `Ctrl+Shift+Tab` is the reverse tab switch, `Ctrl+Arrow` is Spaces and Mission Control on macOS, `Ctrl+Space` is IME switching on Windows, and the `F6` family is browser chrome. If none is free, the fix is reopening ADR 0010's decision to weld focus to selection, since "move focus without selecting" needs a chord only because selection follows focus by default. That is a design question and should surface as its own ticket rather than being decided here.

Amends ADR 0010's multi-select section and ADR 0026's table row if the binding is confirmed dead.
