# Every keyboard focus hand-off names its target, and shortcut hints name the key the layout prints

ADR 0010, ADR 0026, ADR 0036 and ADR 0050 set the main focus and key rules. A set of edge cases in `DiagramCanvas` and `DiagramCanvas.razor.js` was decided in code and written down nowhere else: where focus goes after an edit, a palette add or a cut, what keeps port placement and port picking visible and reachable, how the listener routes keys, and how a shortcut hint names its key.

**Every focus hand-off below names its target, and every key row says when it calls `preventDefault`. An armed source port stays highlighted after focus leaves it, and port placement starts only on an instance with a tab stop. `ShortcutHint.Render` names a chord's key from the layout map the browser reports for the chord's `Code`.**

## Where focus goes

ADR 0051 and ADR 0062 return focus to the edited instance after an inline edit ends with `Escape`. For a label edit, focus goes to the stop of the edge whose label was edited. When the target stop is not on screen, focus goes to the canvas.

When a palette button's `Enter` or `Space` reaches `ClickToAdd`, focus moves to the placed instance, because a keyboard user has no other way to reach it.

A cut can remove the stop that holds focus once its render lands. Focus would then drop to the body, and the `Ctrl+V` that usually follows would have nowhere to land. So the clipboard listener focuses the canvas after a cut.

## Port placement needs a tab stop

ADR 0050 ends placement when focus leaves the instance's stop, so placement is only ever live on the focused instance. Below the LOD cutoff (ADR 0011) an instance renders as a placeholder with no tab stop. Focus could never reach it, and nothing would end placement. So `PlaceablePortTarget` accepts only an instance whose stop can take focus, and the menu offers **Add port…** only when it finds one. Zooming out until an instance becomes a placeholder removes the row from its menu. Zooming in brings it back.

`BeginPortPlacement` moves focus to the instance's stop. A menu opened with focus on the canvas would otherwise leave the placement keys somewhere `Enter` does not reach.

`EndPortPlacementOnFocus` ends placement when focus lands anywhere but the instance's stop. A landing on the canvas ends it only once the stop has held focus. The menu that started placement hands focus back to wherever it was at the keydown, the canvas included, before focus reaches the stop.

## Port picking keeps its source visible

ADR 0036 records that `FocusEntity` clears stale port-picking state on every genuine focus change. It must, because native `Tab` is never intercepted and every focus change passes through it. Port picking never moves DOM focus itself, so `FocusEntity` never clears a pick in progress on the same instance.

Under focus-follows-selection, tabbing to the target selects the target, so the source is no longer selected or focused. The source's armed port keeps `.port-focused` anyway. That highlight shows the keyboard user where the connection started while they move to the target. The highlight reads the armed port state, never selection or focus.

## Tab stop indexes

`FocusableTabStopIds` skips what native `Tab` skips: a member that is not addressable and an LOD placeholder. ADR 0054 covers why it must agree index for index with `focusTabStopAt`, which counts `.instance-layer [tabindex="0"]` in document order.

## Key routing in the listener

Focus has left the container only once it lands somewhere else on the page. A window losing focus, or a focused stop removed from the DOM, leaves focus nowhere new. Neither ends anything that waits for focus to leave.

The keydown handler calls `preventDefault` only inside a branch that invokes .NET. `Tab` and every unhandled key keep the browser's default.

An inline editor ends its own edit on `Escape`. The listener does not forward that `Escape`, so the next `Escape` is the one that reaches the canvas.

The framing chords `Shift+1`, `Shift+2` and `Shift+0` match the physical key (`Digit1`, `Digit2`, `Digit0`), so they hold on any layout. Their guard is stricter than the table's focus guard. They fire only while focus is inside the container, so an embedded canvas that does not hold focus leaves the page its own `Shift+1`.

## Shortcut hints follow the physical key

ADR 0026 matches every chord on `event.code`, so `Ctrl+A` fires on the key in the US `A` position whatever the layout prints on it. A hint that always said "A" would name the wrong key on any other layout.

`DiagramCanvas.razor.js` asks `navigator.keyboard.getLayoutMap()` for the label of each code in `HINTED_KEY_CODES` (`KeyA`, `KeyD`, `KeyG`, `KeyL`, `BracketLeft`, `BracketRight`, `Quote`) and passes them to the canvas. `ShortcutHint.Render` uses that label for `Chord.Code`. On AZERTY the key `Ctrl+A` answers to is hinted as `Ctrl+Q`, or `⌘Q` on a Mac.

Only Chromium answers the call. Elsewhere it returns nothing, and every hint keeps its US name. A non-US layout outside Chromium shows the US name until those browsers expose the layout map.

A chord carried by the browser's own clipboard events has no `Code`, because the operating system already maps those through the layout. Its hint keeps `Chord.Key`.

## Consequences

A new focus hand-off or key row names its target and its `preventDefault` behaviour, here or in ADR 0026's table.

On Chromium the hint and the key that fires agree on every layout.
