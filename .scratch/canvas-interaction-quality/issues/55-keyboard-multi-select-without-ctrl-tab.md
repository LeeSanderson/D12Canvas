# Keyboard multi-select without Ctrl+Tab

Type: grilling
Status: resolved
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

## Answer

Resolved by grilling on 2026-10-03. Recorded in [ADR 0059](../../../docs/adr/0059-space-starts-additive-traversal.md), with a new `CONTEXT.md` term, `Additive traversal`.

Keep the weld and let `Space` start the additive mode. No new chord, and focus is not unwelded. Outside the mode, `Space` was nearly useless: whenever it had a target, that target was already selected, so the toggle only emptied the selection. Giving it a new meaning takes nothing away.

- `Space` outside the mode adds the focused stop (never removes) and starts `Additive traversal`. Inside it, `Tab` and `Shift+Tab` move focus only and `Space` toggles. Instance, group and edge stops all behave alike.
- Ended by `Escape` (selection kept), a pointer press, focus leaving `.diagram-container`, `Enter` on a group stop, and any ADR 0036 command handoff (`Quick create`, `Ctrl+G`, keyboard placement, `BeginEdit`). Not ended by emptying the selection, by `Delete` and other selection commands, or by port picking or placement, which sit on top of the mode and take `Space` while active.
- `Escape` stages: cancel gesture, end port pick or placement, end the mode, leave the entered group, clear the selection.
- Focus indicator: `:focus-visible:not([aria-selected="true"])` draws a dashed `--d12-accent` outline. No new token, no C# class, no live region, no `aria-multiselectable`.
- `Ctrl+Tab`, `OnCtrlTabPressed`, `_suppressFocusSelect` and `DiagramCanvasCtrlTabSpaceMultiSelectTests` are removed.
- Verified by a real-key `Interaction probe`, one visual baseline for the ring, and bUnit for the flag's clear events, `Space` routing and `Escape` order.

Amends ADRs 0010, 0026 (table rows rewritten), 0036, 0044 and 0046. Confirms 0022, 0050 and 0054. The `Ctrl+Tab` row was dropped from [Chord survival on macOS and in Firefox](56-chord-survival-macos-firefox.md).
