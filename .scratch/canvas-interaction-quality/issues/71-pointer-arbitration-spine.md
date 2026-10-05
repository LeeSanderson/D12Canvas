# 71 — Pointer arbitration spine

**What to build:** Every press on the canvas is classified once, in the browser, and exactly one `Pointer gesture` owns it until release (ADRs 0017 classification half, 0018, 0022, 0031, 0036, 0038, 0066, 0025). This ticket builds the whole spine and moves the two canvas-owned gestures onto it:

- A `pointerdown` listener on the canvas element walks up from the event target to the nearest marked element and hands C# a `Hit target` of `(role, entityId, part)` plus press count, `pointerType`, buttons and modifiers. All eleven roles are classified from the start; `canvas` means the walk found nothing.
- Five decisions are taken synchronously before the interop hop: `preventDefault`, `setPointerCapture` on a stable element the library controls, the single `Focus transfer` to the canvas element (which gains `tabindex="-1"` and is focused with `preventScroll`), the 4 screen-pixel `Drag threshold` (JavaScript never calls C# below it), and native-menu suppression. Moves are coalesced to one call per animation frame. The press point is converted to board space once, so a delta is always release minus press.
- Four public `[JSInvokable]` entry points (`OnPointerPressed`, `OnPointerMoved`, `OnPointerReleased`, `OnPointerCancelled`) and one reusable `addPointerListener` with `classify` on or off. The gesture kind is chosen in C# after the hop because it needs the selection. Gestures are objects over an explicit context (board, selection, `ZoomPanTracker`, commit path), never a switch, with phases `pointing`, `active`, `cancelled`.
- `Pan` and `MarqueeSelect` are built. A plain left-drag on empty canvas draws the marquee, Shift+drag adds the band's contents to the selection, a quick click on empty canvas clears the selection, the right and middle buttons pan whatever they land on, a middle click below the threshold does nothing, and a right-click release from `pointing` resolves the selection (preserve if inside it, select the pressed entity, clear on empty canvas) and opens the existing menu at the press point.
- Ownership is keyed by `pointerId` and the claiming button; other buttons' down and up are dropped. `pointerup` commits, `pointercancel` reverts, `lostpointercapture` on a live gesture reverts and writes `console.error`. A window `blur` cancels and ends the press. Escape cancels: drop the preview, restore the `Selection snapshot` taken at press, mark `cancelled`, keep capture until the claiming button comes up, and that release does nothing. The viewport is never restored.
- While any gesture owns the press, nothing writes `Board` except that gesture's release and no keyboard command changes the selection. The guard sits on `CommandHistory` and the two selection-only handlers; Escape, copy, snap toggle, focus moves and viewport commands stay live. A host replacing the `Board` reference cancels without restoring the snapshot.
- The canvas-level `@onmousedown`, `@onmousemove`, `@onmouseup`, `@onclick` and `@oncontextmenu` bindings are deleted, along with `_dragMoved`, `_isPanning`, `_isMarqueeSelecting` and the pan render throttle.

Until ticket 78 lands, a press whose role is still served by an old handler (`instance`, `resize-handle`, `port`, `port-strip`, `edge`, `edge-endpoint`, `edge-label`, `selection-bounds`, `selection-handle`, `author-content`) is left to that handler: the listener takes none of the synchronous decisions for it and C# chooses no gesture. Preventing `pointerdown` would suppress the compatibility mouse events those handlers rely on. Ticket 78 removes the allowance.

**Blocked by:** None — can start immediately

**Status:** ready-for-agent

- [ ] A fast click on empty canvas never leaves the canvas panning; a release outside the canvas or over a shape ends a pan or marquee and a buttonless move afterwards does nothing
- [ ] Left-drag on empty canvas marquees (intersection semantics as today); Shift+drag adds to the selection across sweeps; right and middle drag pan; the pan is `panOrigin + (current - press)` with no reset-anchor
- [ ] Right-click opens the menu at the press point on release from `pointing` and never wipes a selection it landed inside; a right-drag past the threshold pans and opens nothing
- [ ] Escape mid-marquee restores the selection snapshot; Escape mid-pan leaves the viewport where it is; a cancelled press does nothing at release
- [ ] A window blur mid-gesture cancels, releases capture and leaves the keyboard working on return
- [ ] Delete, Ctrl+Z and Ctrl+G do nothing while a press is held; Escape and PageUp still act
- [ ] `InternalsVisibleTo` on the test project is added, bought only for reading the `Gesture preview` as data; gesture types stay internal
- [ ] A `[Theory]` enumerates the gesture-kind set with one release-reliability case and one cancel case per member; the set is closed over what exists so far and grows with tickets 74 to 77 and 107. `docs/agents/testing.md` gains one line pointing at it
- [ ] The press-to-kind mapping is tested as a bUnit table through the four public entry points for the `canvas` role across both buttons and modifiers
- [ ] The test base's module mock covers `addPointerListener`; marquee, pan, context-menu and selection tests that dispatched mouse events are rewritten at the gesture-object or entry-point seam, never kept calling a handler directly
- [ ] The canvas focuses itself on a press with `preventScroll`; nothing moves focus because the selection changed
- [ ] A demo page or an existing one exercises a mid-marquee state for the visual suite; the canvas `tabindex` moves every `.verified.html`, which is planned churn
- [ ] Full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [ ] `CONTEXT.md`'s `Pointer gesture`, `Hit target`, `Drag threshold`, `Focus transfer`, `Selection snapshot` and `Release-reliability case` terms describe what shipped
