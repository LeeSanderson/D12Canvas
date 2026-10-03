# Space starts additive traversal, and Ctrl+Tab is removed

ADR 0010 built keyboard multi-select from two keys: `Ctrl+Tab` moved focus without selecting, and `Space` toggled the focused stop. [Whether Ctrl+Tab and Ctrl+Arrow survive the browser](../../.scratch/canvas-interaction-quality/issues/41-ctrl-tab-browser-reservation.md) measured `Ctrl+Tab` as dead in Chrome and Edge on Windows. The browser switches tabs before the page sees the `Tab` keydown. So the keyboard has had no additive route.

This decides the replacement: **`Space` starts a transient mode, `Additive traversal`, in which `Tab` moves focus without selecting.** Focus still drives selection on a plain `Tab`. `Ctrl+Tab` is removed.

## Why not a new chord, and why not unweld

ADR 0026 found every candidate chord reserved somewhere: `Ctrl+Shift+Tab`, `Ctrl+Arrow` (now `Quick create`), `Ctrl+Space` (IME switching on Windows), and the `F6` family. Any new one would need the probe in [Chord survival on macOS and in Firefox](../../.scratch/canvas-interaction-quality/issues/56-chord-survival-macos-firefox.md) before anyone could trust it. That is how `Ctrl+Tab` got in.

Unwelding focus from selection, the ARIA multi-select listbox pattern, is the textbook answer. It costs a keypress on every single-select, and the ring is already long: ADR 0054 roughly doubles it on a `Quick create` board.

An explicit mode key, such as Windows' `Shift+F8` add mode, keeps single-select cheap. It still spends a key that needs probing, and few users know the convention.

What made the chosen answer free is a fact about `Space` under the weld. A plain `Tab` selects the stop it lands on, and a pointer press nulls `_focusedTabStopId` (ADR 0036). So whenever `Space` has a target outside the `Ctrl+Tab` flow, the target is already selected, and toggling it only empties the selection. The key had no useful meaning on its own, so giving it one takes nothing away.

## The rule

- **`Space` outside the mode adds the focused stop to the selection and starts the mode.** It never removes. With A selected and focus on A, the selection stays {A}. With {A, C} selected and focus on B, it becomes {A, B, C}.
- **Inside the mode, `Tab` and `Shift+Tab` move focus only**, and `Space` toggles the focused stop, as ADR 0010's `Space` did.
- It applies to instance, group and edge stops alike. ADR 0054 made an edge stop an ordinary member of the ring, and `Space` on one goes through the existing edge toggle.
- Commands act on the selection, not on focus. With focus on an unselected stop, an arrow key nudges the selection and `Delete` removes it.

The keystrokes are ADR 0010's flow without `Ctrl`: `Tab`, `Space`, `Tab`, `Tab`, `Space`. A keyboard user who never presses `Space` sees no change.

## What ends the mode

The flag lives beside `_focusedTabStopId` and is cleared on the same occasions:

- **`Escape`**, which keeps the selection.
- **A pointer press.** The `@onfocus` on `.diagram-canvas` that already nulls `_focusedTabStopId` clears the flag in the same place.
- **Focus leaving `.diagram-container`.** Tabbing past the last stop into the host page does not leave the mode waiting for the user's return.
- **`Enter` on a group stop.** The mode ends first, then the group is entered as ADR 0044 says. ADR 0044 keeps the selection to one scope, so a multi-selection should not carry across into a group.
- **A command handoff.** ADR 0036's third occasion: `Quick create`, `Ctrl+G` to the group's stop, keyboard placement, `BeginEdit`. Each relies on the target's `@onfocus` selecting it, and ADR 0036 calls that hard-select correct. A command that chooses where focus goes has also chosen what the user works on next, so the weld applies again.

These do **not** end it:

- **`Space` toggling the last member out.** The user is still building, and an empty selection inside the mode is a fresh start.
- **`Delete` and the other selection commands.** Focus may sit on an unselected stop that survives.
- **Port picking and port placement.** Both sit on top of the mode. Port picking already takes `Space` for its port cycle while active, and ADR 0050 already makes `Space` a no-op during placement. When either ends, `Space` toggles again. Connecting two shapes does not touch the selection, so nothing is lost.

## Escape's stages

One stage per press, newest first:

1. Cancel the active pointer gesture.
2. End port picking or port placement.
3. End `Additive traversal`.
4. Step out of the `Entered group`.
5. Clear the selection.

A multi-selection built inside an entered group takes three presses to clear fully, and none of them throws the selection away by surprise.

After `Escape` ends the mode, focus can still rest on an unselected stop. The next plain `Tab` replaces the selection. That is the weld returning as designed, and the selection is still there to act on until then.

## The focus indicator

Inside the mode, focus can rest on a stop that is not selected. ADR 0036 records that the library draws no focus ring, because under the weld the selection chrome marked the focused stop. That no longer holds everywhere.

**`:focus-visible:not([aria-selected="true"])` on the instance, group and edge stops draws a dashed `--d12-accent` outline**, offset outside where selection chrome sits.

- It appears only in the gap this decision creates. Outside the mode the focused stop is always selected, so nothing on screen changes and no existing baseline moves.
- It reads `aria-selected`, which every stop already renders, so it cannot drift from the mode flag, and the mode needs no CSS class or C# state.
- Dashed says "here, not chosen". A solid ring would read as a second selection.
- It reuses the accent, so ADR 0012's token set does not grow.

The edge stop is an invisible proxy sized to the union box of its endpoints (ADR 0054), so the ring outlines that box.

**The mode itself has no indicator.** A sighted user sees it on the first `Tab` after `Space`: the ring moves and the selection chrome stays. A screen-reader user hears the next stop read as "not selected" where a plain `Tab` would have read "selected". A live-region announcement would be the library's first `aria-live` region, built for one transition. `aria-multiselectable` is valid only on a `listbox`, `grid` or `tree` container, and giving the board a container role is a wider question than this one.

## Ctrl+Tab is removed

The binding, `OnCtrlTabPressed`, `_suppressFocusSelect`, and `DiagramCanvasCtrlTabSpaceMultiSelectTests` go. An alias kept for browsers where the chord might arrive would show a hint that works on some machines and switches browser tabs on others. ADR 0026 recorded that as the failure to avoid. The `Ctrl+Tab` row leaves the probe list in [Chord survival on macOS and in Firefox](../../.scratch/canvas-interaction-quality/issues/56-chord-survival-macos-firefox.md).

## How this is verified

Per ADR 0025:

- **An `Interaction probe` sends real keys.** With focus on the board: `Tab`, `Space`, `Tab`, `Tab`, `Space`, then assert the selection is {first, third}. `Escape` keeps it. A plain `Tab` then replaces it. A second case tabs past the last stop and back in, and asserts the mode has ended. No step calls a handler, because a test that calls the handler is how the `Ctrl+Tab` defect stayed green.
- **One Playwright visual case** on a Demo page: two stops selected, focus on a third, unselected stop showing the dashed ring.
- **bUnit for the state rules:** each event that clears the flag, the three meanings of `Space` (start, toggle, cycle ports), and the order of `Escape`'s stages. These tests drive handlers directly and claim nothing about key delivery.

No manual pass is owed. `Tab` and `Space` are not browser-reserved anywhere, so ticket 41's measurement problem does not arise.

## Amends, confirms

- **Amends ADR 0010.** The keyboard multi-select section: `Ctrl+Tab` is replaced by `Additive traversal`. Focus-follows-selection holds outside the mode.
- **Amends ADR 0026.** The `Ctrl+Tab` row is removed. The `Space` row reads: start `Additive traversal`, adding the focused stop, and inside it toggle. `Escape`'s row gains the stages above.
- **Amends ADR 0036.** A command handoff ends the mode before it moves focus. "This library draws no focus ring anywhere" no longer holds: a focused stop that is not selected draws one.
- **Amends ADR 0044 and ADR 0046** where they name `Ctrl+Tab` and `Space` as the keyboard's multi-select. It is now `Space` and `Tab`. Inside an entered group the mode works over the scope's members, as before.
- **Confirms ADR 0022.** The additive marquee keeps a keyboard analogue.
- **Confirms ADR 0050 and ADR 0054.** Placement's no-op `Space` and the edge stop's `Space` toggle are unchanged.
- **Confirms ADR 0036's mixed-flow cost.** After a pointer press, `Space` still has no target until a `Tab` lands focus on a stop.

## Considered and rejected

- **Unweld focus from selection.** Costs a keypress on every single-select, on a ring that is already long.
- **A mode key such as `Shift+F8`.** Spends a key that needs a probe in every browser, for a convention few users know.
- **A new chord for "move focus without selecting".** Every candidate is reserved somewhere, and picking one without measuring is the mistake `Ctrl+Tab` was.
- **Keeping `Ctrl+Tab` as an alias.** Works on some machines and switches tabs on others, behind the same hint.
- **Ending the mode when the selection empties.** The user is mid-build, and it would make `Space` on the last member a hidden exit.
- **Ending the mode when port picking or placement starts.** Drops the selection being built at a moment the user did not choose.
- **Selecting each handoff target explicitly while the mode stays on.** Four call sites each gain a select, to keep a mode alive whose selection the command has just replaced.
- **A ring on every focused stop.** Doubles up with selection chrome on every plain `Tab` and moves every keyboard baseline.
- **A mode class set from C#.** The same pixels from a second source of truth.
- **A live-region announcement or `aria-multiselectable`.** The first is new chrome for one transition. The second needs a container role the board does not have.
