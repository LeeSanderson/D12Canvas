# What the browser actually delivers for trackpad input

Type: research
Status: resolved

## Question

Establish, from primary sources, what a browser actually delivers to a Blazor `@onwheel` / pointer handler for trackpad input — so the arbitration model (01) and the pan/zoom work are designed against real event streams rather than assumptions.

Today `DiagramCanvas` binds `@onwheel="HandleMouseWheel"` and treats every wheel event as zoom. `ctrlKey` is read in `DiagramCanvas.razor.js` only for keyboard shortcut matching; nothing distinguishes a pinch from a scroll, and there is no two-finger-pan or momentum handling anywhere.

Find out:

- How a trackpad pinch is reported. The convention is a `wheel` event with `ctrlKey: true` synthesised by the OS/browser — confirm this is real and current, and establish how it is distinguished from a genuine Ctrl+scroll by a mouse user.
- How two-finger pan is reported, and how `deltaX`/`deltaY`/`deltaMode` differ between a discrete mouse wheel and a continuous trackpad surface.
- Whether momentum/inertia scrolling is distinguishable from user-driven scrolling, and whether there is any reliable "gesture ended" signal.
- Differences across Chrome, Firefox and Safari, and across Windows and macOS. The repo's Playwright rig runs headless Chromium in Docker, so anything that only reproduces elsewhere needs flagging as untestable in CI.
- Whether Pointer Events, `wheel`, or the non-standard `gesturestart`/`gesturechange` family is the right foundation — including what each costs in browser support and what each implies for the touch non-foreclosure constraint in the map Notes.
- Whether `preventDefault` on `wheel` requires a non-passive listener, and what that means for a Blazor `@onwheel` binding specifically (this may force JS interop rather than a Razor binding).

Capture findings as a markdown file in the repo and link it from this ticket.

## Answer

The pinch → `wheel` + synthesised `ctrlKey: true` convention is real and current, confirmed from all three engines' own source rather than folklore — Chromium's `touchpad_pinch_event_queue.cc`, Gecko's `InputData.cpp`, WebKit's `PlatformWheelEvent.cpp` (where `ctrlKey` is simply hardcoded `true`). Chromium's own comment gives the decoder: `scale = exp(-deltaY / 100)`.

Nothing distinguishes a synthesised pinch from a genuine Ctrl+scroll — no spec, no engine, no IDL attribute, and the gap is a long-standing open request against both `uievents` and `pointerevents`. **This turns out not to matter**, and that is the more useful finding: both gestures mean "zoom about the cursor", which is precisely what a mouse user pressing Ctrl is deliberately asking for. What must not be shared is the *step size* — a mouse notch and a pinch increment differ by roughly two orders of magnitude (`kWheelDelta = 120` vs. a fractional `100·ln(scale)`), so deriving zoom multiplicatively from `deltaY` rather than as a fixed `±0.1` step makes both device classes fall out of one code path with no device detection at all. Two-finger pan is an ordinary unmodified `wheel` event; the classic `deltaMode` hazard is much weaker than its reputation (Chromium *cannot* emit `DOM_DELTA_LINE`, and Gecko's line-delta behaviour is defused by default in a way Blazor's marshalling order happens to land on the right side of).

Momentum is detectable in exactly one engine: `WheelEvent.momentum` is normative in Pointer Events Level 4 and shipped in Chrome 151 (28 July 2026), but Gecko and WebKit expose nothing, and this repo's CI pins Chromium 149 — so it must be feature-detected with a working non-momentum path. **There is no "gesture ended" signal in any engine**; `scrollend` is structurally the wrong tool because a canvas that cancels the wheel event never scrolls anything. A short idle timeout is the portable answer, and it matters for undo granularity, not smoothness: `CONTEXT.md` defines a Gesture as exactly one history entry, and a macOS momentum tail delivers dozens of wheel events after the user has physically let go.

The headline finding changes an implementation plan outright: **`@onwheel:preventDefault` is a silent no-op on the .NET 9 runtime this repo pins.** Blazor delegates every `@on*` binding to a single listener on `document`, one of the four targets the DOM standard makes passive-by-default for `wheel`; on `release/9.0` `setPreventDefault` sets a flag and does nothing else. Microsoft fixed this in PR #62479, milestone 10.0-preview7 — merged to `main`, not backported. Even after a .NET 10 upgrade the Razor binding stays the weaker option, because `preventDefault` is a render-time all-or-nothing flag while the interesting decision ("is this ctrl-wheel?") is per-event, and the .NET handler runs after the event is already over.

**Recommendation: build pan/zoom on `wheel` alone, via a hand-rolled non-passive listener on the canvas `<div>` in the existing `DiagramCanvas.razor.js` module, and drop the `@onwheel` binding.** Not a grudging workaround — it is the only thing that works on .NET 9, it needs no `{passive: false}` opt-in (a listener on a `<div>` is already active per the DOM standard), it puts the cancel decision inside the event where the ctrl-vs-plain distinction actually exists, and the repo already has this exact disposable-handle shape three times over in `addResizeListener`/`addKeyboardListener`. Do **not** add `gesturestart`/`gesturechange`: on macOS Safari the ctrl-wheel *is* that gesture's default action, so handling both double-counts. Pointer Events cannot see a trackpad gesture at all, so `wheel` is not merely the pragmatic choice but the only one.

For the map's non-foreclosure constraint, the cheapest possible discharge is `touch-action: none` on `.diagram-canvas` now — inert for mouse and trackpad, and the spec is explicit that setting it inside a `pointerdown` handler is too late.

**What ticket 01 should take from this:** the arbitration model's zoom and pan inputs should be `(anchorPoint, scaleFactor)` and `(dx, dy)` in container space — never a `WheelEventArgs`. Every present and future source (plain wheel, ctrl-wheel, a touch pinch, keyboard, a future minimap drag) reduces to those two calls, which is what makes touch an adapter rather than a redesign.

**CI limits, for ticket 15:** a pinch-*shaped* event stream is testable (Playwright forwards modifier state into CDP, and fractional/horizontal deltas replay faithfully), but `WheelEvent.momentum`, real trackpad gestures (`Input.synthesizePinchGesture` emits *touch* events, not ctrl-wheel), inertia tails, and everything Firefox/Safari-specific are not. Assert direction and relative magnitude, never absolute pixel counts — Blink divides by `devicePixelRatio` and Playwright's own suite gives up asserting values on macOS hosts. Two claims in the doc are source-verified but were never executed in a browser (the `Keyboard.DownAsync("Control")` → `ctrlKey: true` chain, and the `synthesizePinchGesture` behaviour); a single throwaway spec asserting `ctrlKey`, `deltaY` sign and `defaultPrevented` would settle both cheaply before ticket 15 commits to an approach.

Full findings, 9 sections and citations traced to engine source: branch `research/trackpad-input`, file `.scratch/canvas-interaction-quality/research/trackpad-input.md`, commit `32a9cc4`.
