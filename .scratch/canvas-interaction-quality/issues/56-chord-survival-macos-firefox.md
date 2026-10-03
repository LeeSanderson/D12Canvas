# Chord survival on macOS and in Firefox

Type: task
Status: open
Blocked by:

## Question

Fill in the rows [Whether Ctrl+Tab and Ctrl+Arrow survive the browser](41-ctrl-tab-browser-reservation.md) could not measure: Firefox on Windows, and Safari, Chrome and Firefox on macOS. Use the same [chord probe page](../assets/41-key-probe.html). It is a single self-contained HTML file.

Needs a human at a Mac. On macOS, record whether Mission Control and Spaces shortcuts are on (System Settings, Keyboard, Keyboard Shortcuts, Mission Control), since that changes the result.

Per engine and platform, with the probe's tab stop focused, and with `preventDefault` on and then off:

- `Ctrl+Left/Right/Up/Down`, and on macOS `Cmd+Left/Right/Up/Down`.
- Does the keydown for the non-modifier key reach the page? Does the browser or OS act anyway (blur, `visibility hidden`, back/forward navigation, a Space switch)?

Keep the vertical and horizontal arrows apart. `Cmd+Up` and `Cmd+Down` are not browser navigation, so the vertical pair may survive where the horizontal pair does not.

These rows matter for ADR 0030's `Ctrl+Arrow` macOS doubt. Record the results on ADR 0030 and ADR 0026 by addendum.

## Comments

**From [Keyboard multi-select without Ctrl+Tab](55-keyboard-multi-select-without-ctrl-tab.md), resolved (ADR 0059):** `Ctrl+Tab` is removed from the library, so its rows are dropped from this probe. Keyboard multi-select now uses `Space` and `Tab`, which no browser reserves, so this ticket no longer gates it.

**From [Moving through a long tab ring](62-moving-through-a-long-tab-ring.md), resolved (ADR 0060):** add `Ctrl+Shift+Left/Right/Up/Down` and, on macOS, `Cmd+Shift+Left/Right/Up/Down` to every macOS row. They carry `Directional focus`. Windows is measured separately by [Ctrl+Shift+Arrow on Windows](63-ctrl-shift-arrow-on-windows.md). Record the results on ADR 0060 as well.
