# Ctrl+Shift+Arrow on Windows

Type: task
Status: resolved
Blocked by:

## Question

Measure whether `Ctrl+Shift+Left/Right/Up/Down` reaches the page on Windows, before ADR 0060's `Directional focus` is built on it. Use the [chord probe page](../assets/41-key-probe.html), as [Whether Ctrl+Tab and Ctrl+Arrow survive the browser](41-ctrl-tab-browser-reservation.md) did. Run it in Chrome and Edge, and in Firefox if it is installed.

Per browser, with the probe's tab stop focused, and with `preventDefault` on and then off:

- Does the keydown for the arrow reach the page with `ctrlKey` and `shiftKey` set?
- Does the browser or OS act anyway (focus leaves, the page scrolls, a text selection appears, anything else)?

This needs a person at the keyboard. Playwright drives the page, not the browser, so it cannot see the browser acting on a chord (ADR 0026).

The implementation ticket for ADR 0060 must list this ticket in its `Blocked by:` line. If Windows reserves the chord, ADR 0060 reopens on the binding only. Record the result on ADR 0060 and ADR 0026 by addendum.

## Answer

**Windows does not reserve `Ctrl+Shift+Arrow`. ADR 0060's binding stands.** Measured 2026-10-03 on Windows 11, Chrome 153 and Edge, by hand, with the probe's tab stop focused.

- All four arrows deliver a keydown to the page with `ctrlKey` and `shiftKey` set, in both browsers.
- With `preventDefault` on, it takes on every keydown.
- With `preventDefault` off, nothing happens either. No window blur, no `visibility hidden`, no scroll, no text selection, and nothing outside the page: no window snap, tab switch or layout popup. So on a focused non-editable element the chord has no default action to suppress. The library still calls `preventDefault`, like every other row.
- Firefox is not installed here, so it was not measured. Its `Ctrl+Shift+Arrow` rows join [Chord survival on macOS and in Firefox](56-chord-survival-macos-firefox.md), which already carries Firefox on Windows for `Ctrl+Arrow`.

Edge behaved the same as Chrome. Only the Chrome log was kept: [63-ctrl-shift-arrow-windows.log](../assets/63-ctrl-shift-arrow-windows.log).

The [chord probe page](../assets/41-key-probe.html) gained filler content and two logged events, `scroll` and a non-empty `selectionchange`. Without them it could not show the scroll and selection outcomes this ticket asks about. The macOS run reuses the same page and benefits too.

When ADR 0060's implementation ticket is written, this ticket goes in its `Blocked by:` line as the ticket says. It is resolved, so it no longer blocks anything.
