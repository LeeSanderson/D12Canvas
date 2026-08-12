# Wheel-driven pan and zoom model

Type: prototype
Status: open
Blocked by: 03

## Question

Decide what a wheel event does to the viewport — which gesture it maps to, how far it moves, and what it feels like.

Ticket 02 established the input contract from engine source, and in doing so exposed that no ticket on this map owns the *output* side of it. Ticket 01 arbitrates pointer presses, and a wheel is not a press; ticket 13 owns viewport *commands* (fit, zoom-to-selection, minimap), not continuous input. Today `DiagramCanvas` binds `@onwheel="HandleMouseWheel"` and treats every wheel event as zoom via `ZoomPanTracker.SetScale(_scale ± 0.1)`: `DeltaX` is discarded, magnitude is discarded, `CtrlKey` is never read, and nothing calls `preventDefault`, so the browser's own scroll and Ctrl-zoom still run underneath.

Ticket 02 settles the mechanism and it is not up for re-litigation here: the listener moves into `DiagramCanvas.razor.js` as a non-passive listener on the container element (the `@onwheel` binding cannot cancel anything on .NET 9), and pinch is not distinguishable from Ctrl+scroll, so no attempt is made. What remains is the behaviour.

Decide:

- **What plain wheel does.** Ticket 03's teardown (§1.5) answers this directly and the answer is against current behaviour: three of four default plain wheel to **pan** with Ctrl/Cmd inverting to zoom (tldraw's `wheelBehavior: 'pan'`, FigJam, Excalidraw), while Miro splits it by an explicit Mouse/Trackpad preference. Decide whether to follow the majority, and whether a mouse-wheel user — who has no second axis and expects a wheel to zoom on a canvas — needs the Miro escape hatch.
- **Whether `deltaX` pans horizontally**, and whether a shift-wheel horizontal convention is worth having for mouse users who have no horizontal axis.
- **The zoom response curve.** Ticket 02 recommends `scale *= exp(-deltaY / 100)` — Chromium's own documented inverse — over a fixed step, but a raw mouse notch's ±100 delta becomes a ~2.7× jump, so a damping constant has to be chosen and *felt*, not reasoned about. This is why the ticket is a prototype.
- **Whether zoom anchors on the pointer** rather than the viewport centre. Anchored zoom is what makes a pinch feel attached to the content; `ClientX`/`ClientY` and `ToBoardPoint` are already available. Ticket 03 found all four reference tools anchor on the pointer and zoom multiplicatively, so the burden here is on keeping current behaviour, not on changing it. This changes ADR 0011's zoom model behaviourally, so decide whether it amends or supersedes.
- **Where a wheel gesture starts and ends.** There is no browser signal (ticket 02), so a short idle timeout, with `momentum === true` as an optional early terminator where present. The consumer is undo granularity: `CONTEXT.md` defines a Gesture as exactly one history entry, and a momentum tail delivers dozens of events after the user has let go. Decide the timeout, and whether zoom/pan enter history at all.
- **Whether the canvas swallows every wheel event or lets some fall through** to a host page's own scrolling — an embeddable library pinned inside a scrolling host page is a real deployment, and `preventDefault`-always forecloses it.

Feeds `(anchorPoint, scaleFactor)` and `(dx, dy)` into whatever ticket 01 produces, per ticket 02's recommendation — never a `WheelEventArgs`.

Worth ten minutes while prototyping: ticket 02 flagged that the `Keyboard.DownAsync("Control")` + `Mouse.WheelAsync` → `ctrlKey: true` chain is verified through Playwright's source but never actually executed. A throwaway spec asserting `ctrlKey`, `deltaY` sign and `defaultPrevented` settles it before ticket 15 commits to a testing approach.

**Two notes handed over from ticket 13**, which found them while deciding the framing commands:

- **`.canvas-content` already carries `transition: transform 0.1s ease-out`, and it applies to every transform write** — so pan does not track the pointer today, it *eases toward* it by up to 100ms, and the same lag sits on every zoom step. That is very likely part of why the canvas feels indirect, and it is invisible when reading the C#, since nothing in `ZoomPanTracker` or `ContentStyle` mentions time. This ticket owns it: a continuous gesture wants the transition gone (or near-zero), while ADR 0015 wants a longer one for discrete framing, so the two cannot share one blanket rule. ADR 0015 already assumes the split — it applies its own class for a framing flight — so what is left here is deciding what the *ambient* duration should be once framing no longer depends on it.
- **ADR 0015's ~250ms framing-flight duration is the one number in it defended by judgement rather than evidence**, and it is a feel constant of exactly the kind this prototype exists to settle. Worth pulling into the same session as the zoom response curve, since both are judged by the same question — does the viewport feel attached to the input — and a fit that reads as sluggish and a wheel that reads as sluggish have the same fix.
