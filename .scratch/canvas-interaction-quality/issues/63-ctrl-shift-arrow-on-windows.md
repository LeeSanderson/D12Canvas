# Ctrl+Shift+Arrow on Windows

Type: task
Status: open
Blocked by:

## Question

Measure whether `Ctrl+Shift+Left/Right/Up/Down` reaches the page on Windows, before ADR 0060's `Directional focus` is built on it. Use the [chord probe page](../assets/41-key-probe.html), as [Whether Ctrl+Tab and Ctrl+Arrow survive the browser](41-ctrl-tab-browser-reservation.md) did. Run it in Chrome and Edge, and in Firefox if it is installed.

Per browser, with the probe's tab stop focused, and with `preventDefault` on and then off:

- Does the keydown for the arrow reach the page with `ctrlKey` and `shiftKey` set?
- Does the browser or OS act anyway (focus leaves, the page scrolls, a text selection appears, anything else)?

This needs a person at the keyboard. Playwright drives the page, not the browser, so it cannot see the browser acting on a chord (ADR 0026).

The implementation ticket for ADR 0060 must list this ticket in its `Blocked by:` line. If Windows reserves the chord, ADR 0060 reopens on the binding only. Record the result on ADR 0060 and ADR 0026 by addendum.
