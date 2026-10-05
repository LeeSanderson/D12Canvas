# Chord survival on macOS and in Firefox

Type: task
Status: resolved
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

**Parked 2026-10-03:** no Mac in the current setup. Set back to `open` when someone has a Mac.

**From [Ctrl+Shift+Arrow on Windows](63-ctrl-shift-arrow-on-windows.md), resolved:** Chrome and Edge on Windows pass. Firefox was not installed, so add `Ctrl+Shift+Left/Right/Up/Down` to the Firefox-on-Windows row here. The probe page now also logs scroll and text selection, so record those too.

**From [Moving through a long tab ring](62-moving-through-a-long-tab-ring.md), resolved (ADR 0060):** add `Ctrl+Shift+Left/Right/Up/Down` and, on macOS, `Cmd+Shift+Left/Right/Up/Down` to every macOS row. They carry `Directional focus`. Windows is measured separately by [Ctrl+Shift+Arrow on Windows](63-ctrl-shift-arrow-on-windows.md). Record the results on ADR 0060 as well.

**Firefox on Windows, measured 2026-10-05 (partial answer):** Firefox 157 on Windows 11, by hand, with the probe's tab stop focused. Log: [56-firefox-windows.log](../assets/56-firefox-windows.log).

- All eight chords reach the page. `Ctrl+Left/Right/Up/Down` arrive with `ctrlKey` set, and `Ctrl+Shift+Left/Right/Up/Down` with `ctrlKey` and `shiftKey` set.
- With `preventDefault` on, it takes on every keydown and nothing else happens. No window blur, no `visibility hidden`, no scroll, no text selection.
- With `preventDefault` off, Firefox acts on the page, unlike Chrome and Edge. `Ctrl+Left` and `Ctrl+Right` scroll the page horizontally about 32 px. `Ctrl+Down` scrolls to the bottom of the page. `Ctrl+Up` showed nothing, because the page was already at the top. `Ctrl+Shift+Arrow` starts or extends a text selection, and the page scrolls to show it.
- None of this leaves the page. Nothing in the log points to a tab switch, navigation or OS action.

So the `Ctrl+Arrow` and `Ctrl+Shift+Arrow` bindings hold in Firefox on Windows. But Firefox has a real default action for both chords where Chrome has none, so `preventDefault` matters here. Recorded on ADRs 0026, 0030 and 0060 by addendum.

**Parked again 2026-10-05:** only the macOS rows are left: Safari, Chrome and Firefox, with `Ctrl` and `Cmd`, plain and with `Shift`. Set back to `open` when someone has a Mac.

## Answer

**Windows is measured in all three browsers. macOS is assumed, not measured: we treat it as behaving like Windows.** Decided 2026-10-05 by the dev, because no Mac is available to this effort and none is expected.

- **Windows, measured.** Chrome and Edge (tickets 41 and 63) and Firefox 157 (above) deliver all four `Ctrl+Arrow` and all four `Ctrl+Shift+Arrow` keydowns to the page, and `preventDefault` takes in all three. Firefox alone has a default action for both chords, which is why ADR 0026's addendum makes a no-op row still call `preventDefault`.
- **macOS, assumed.** `Ctrl/Cmd+Arrow` and `Ctrl/Cmd+Shift+Arrow` are assumed to reach the page in Safari, Chrome and Firefox and to be suppressible by `preventDefault`, as on Windows. The bindings ship as they are, with no platform split.
- **What the assumption accepts.** ADRs 0026 and 0030 already name macOS defaults that contradict it: `Cmd+Left` and `Cmd+Right` are back and forward in Chrome and Safari, and with Mission Control shortcuts on, `Ctrl+Left`, `Ctrl+Right` and `Ctrl+Up` go to the OS before the browser. If those hold, quick-create and directional focus are partly dead on a Mac, and `Cmd+Left/Right` may navigate away from the page. Nobody has checked the `Shift` variants or the vertical `Cmd` pair.
- **When to revisit.** The first report from a Mac user, or the first time a Mac is available. The [chord probe page](../assets/41-key-probe.html) and this ticket's checklist still apply as written. A failure reopens ADRs 0030 and 0060 on the binding only. The declined platform split in ADR 0030 is the obvious next step.

Recorded on ADRs 0026, 0030 and 0060 by addendum.
