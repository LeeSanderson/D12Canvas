# Keyboard multi-select without Ctrl+Tab

Type: grilling
Status: open
Blocked by:

## Question

Decide how a keyboard-only user builds a multi-selection now that `Ctrl+Tab` is measured dead in Chrome and Edge on Windows. The browser switches tabs before the page sees the `Tab` keydown, so `OnCtrlTabPressed` never runs ([Whether Ctrl+Tab and Ctrl+Arrow survive the browser](41-ctrl-tab-browser-reservation.md)).

Today the keyboard's additive route is `Ctrl+Tab` to move focus without selecting, then `Space` to toggle. ADR 0010 needs that chord only because focus drives selection: native traversal lands on a tab stop, `@onfocus` selects it, and that replaces the selection. ADR 0036 has since narrowed that weld to a single route, the tab stop's own `@onfocus`, so reopening it is smaller than ADR 0010's text implies.

The two shapes to weigh, without treating this list as the whole space:

- **Unweld focus from selection.** Plain `Tab` moves focus only and `Space` or `Enter` selects, as in the ARIA listbox pattern with `aria-multiselectable`. Costs a keypress for the common single-select case and changes what every keyboard user already does.
- **Keep the weld, find another chord.** ADR 0026 lists every candidate as reserved somewhere: `Ctrl+Shift+Tab` (reverse tab switch), `Ctrl+Arrow` (now quick-create, and Mission Control on macOS), `Ctrl+Space` (IME switching on Windows), `F6` (browser chrome). Any new candidate needs the probe run in [Chord survival on macOS and in Firefox](56-chord-survival-macos-firefox.md) before it is trusted.

Things that lean on `Ctrl+Tab` and must still work after the decision: ADR 0044's multi-select inside an entered group, ADR 0046's note that the keyboard covers selecting two buried instances, ADR 0022's additive marquee having a keyboard analogue, and ADR 0036's mixed mouse-then-keyboard cost. [Keyboard reach for edges](49-keyboard-reach-for-edges.md) asks whether edges join the same ring, so whichever answer lands first should say what it assumes about the other.

`DiagramCanvasCtrlTabSpaceMultiSelectTests` calls `OnCtrlTabPressed()` directly and cannot see any of this. Whatever replaces the route needs an `Interaction probe` that sends the real keys.

ADR 0050 adds keyboard port placement, which uses `Arrow`, `Shift+Arrow`, `Enter` and `Escape` and makes every other board-writing key a no-op while it is active. A replacement that rebinds `Enter` or adds a key inside a mode should check against it.

## Comments

**From [Keyboard reach for edges](49-keyboard-reach-for-edges.md), resolved first (ADR 0054):** edges are now in the tab ring, each directly after the stop its source resolves to, so the ring this ticket decides over includes them. ADR 0054 assumes nothing about how this ticket lands. An edge stop is an ordinary stop and gets whatever is decided here. One thing to carry: if this ticket unwelds focus from selection, focus without selection needs a visible indicator, and ADR 0036 records that the library draws none. That covers instance, group and edge stops alike. The ring is also about twice as long on a `Quick create` board, which counts against any answer that costs an extra keypress per stop.
