# Wheel-driven pan and zoom model

Type: prototype
Status: claimed
Blocked by: 03
Prototype: branch `prototype/wheel-pan-zoom` — `dotnet run --project D12Canvas.Demo`, then `/prototype-wheel-pan-zoom`

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

## Comments

**Parked mid-session, still claimed.** A prototype exists and one HITL judging pass has started but has not converged. A resuming session should continue this thread rather than restart it; a parallel session should leave the ticket alone.

### The prototype

Branch `prototype/wheel-pan-zoom` (three commits, off `main` at `dc285c4`). `dotnet run --project D12Canvas.Demo`, then `/prototype-wheel-pan-zoom`. It is a new throwaway Demo route rather than an adjustment to `/board-demo`, because the switcher chrome and the wheel-handling override would both have churned baselines on a page the visual suite covers; it seeds its own wide, dense board so it is still judged against real content. Not linked from the nav on purpose — that markup is shared by every page.

Three variants on `?variant=`, cycled with `,`/`.` (not the arrow keys, which the canvas owns): **A** wheel zooms, **B** wheel pans, **C** an explicit Mouse/Trackpad preference selecting between them. In every variant Ctrl/Cmd+wheel zooms, Shift+wheel pans horizontally, and Alt+wheel pans both axes. Live knobs for every feel constant the ticket has to settle: zoom damping K, pan speed, ambient transition ms, framing-flight ms, gesture idle timeout, pointer-vs-centre anchoring, and the preventDefault policy. A right-hand readout surfaces each wheel event as it arrives, plus a completed-gesture counter for judging undo granularity. Host-page filler above and below the canvas makes the fall-through policy judgeable.

The library is untouched — the prototype drives the public `DiagramCanvas.ZoomPanTracker` and attaches its own non-passive listener to the canvas container, stopping the event before it reaches the document so Blazor's delegated `@onwheel` never runs its fixed-step zoom underneath.

### Banked as evidence, not opinion

`D12Canvas.VisualTests/WheelInputProbeTests.cs` on the same branch — six assertions, all green. Deliberately assertion-only rather than screenshot-based, so unlike the rest of that project it has no font/AA sensitivity and runs correctly outside the pinned container. That distinction is itself a finding for [Verifying interaction quality](15-verifying-interaction-quality.md).

- **Playwright's `Keyboard.DownAsync("Control")` + `Mouse.WheelAsync` chain really does deliver `ctrlKey: true`**, and a plain wheel arrives as `deltaMode: 0` with `deltaY: 120` per notch. Ticket 02 verified this by reading Playwright's source but never executed it; it is now executed.
- **The preventDefault policy must be derived synchronously in JS.** It cannot wait for an interop round trip, so it may only read facts the listener already has — every input to that decision has to be mirrored into JS up front, never fetched from C#. This constrains whatever ticket 01 produces: the wheel path cannot ask C# whether to cancel.
- **Chromium dispatches the pointer at whole pixels.** A fractional anchor lands up to half a pixel from where it was aimed, which the zoom factor then multiplies into a visible board-space discrepancy — it produced a false test failure here before being understood. A trap for any future anchored-zoom test.
- **The pointer-anchor invariant holds** to three decimal places: the board point under the cursor does not move across a zoom.
- **`altKey` survives the trip to a wheel listener** and routes correctly, so Alt is technically available as a wheel modifier. Whether it is *safely spendable* is open — see below.

### Where the human judging got to

Unconverged, and the two reactions so far conflict:

1. Asked to compare A against B on a plain mouse wheel: **"B feels right."** The device was never confirmed, which matters — the whole case for variant C's escape hatch rests on a mouse-wheel user finding plain-wheel-pans wrong, and that never got tested.
2. Then, from a trackpad: **"A feels better, but is missing a vertical pan"** — with a direct question about whether Alt+scroll could supply it. Alt was wired as a full-pan override on that basis.
3. Then: **"adding the alt key vertical scroll seems to have broken the trackpad scrolling"** — reported but never diagnosed, because the session ended. The plain-wheel code path is unchanged (an unmodified swipe falls straight through to the variant default; Alt only intercepts when `altKey` is set), so this is either Alt arriving when it is not being pressed — a real regression in the modifier ordering — or Alt+swipe itself being eaten by Windows or the browser, which would be a finding rather than a bug. **Resume by reading the `ctrl / meta / alt / shift` and `action` rows during a plain, unmodified two-finger swipe** — that discriminates between them immediately.

### The confound that must be ruled out before anything is decided

The A-versus-B verdict is not yet trustworthy. Ticket 13 handed this ticket the finding that `.canvas-content` carries `transition: transform 0.1s ease-out` **applying to every transform write**, so pan does not track the pointer — it eases toward it by up to 100ms. That lag is not symmetric between the variants: a zoom that eases still reads as smooth, while a pan that eases reads as slipping. So B's pan was judged carrying a handicap A's zoom barely notices, on the identical transition. **Set the ambient slider to 0 and re-run A against B** before treating any variant preference as real, and before spending a modifier to work around it.

The ergonomic objection points the same way and is still unanswered: under A, **pan — the more frequent gesture on a large board — is the one requiring a modifier**, while zoom is free. Alt is also already load-bearing in this domain (ADR 0010's `Alt+Arrow` per-axis resize; Alt-drag-to-clone in the reference tools, which this map's fog already tracks under latched-versus-live modifier semantics), and on Windows/Linux it is the browser's menu-bar activation key.

### Still untouched

Nothing below has been put to the human yet: the zoom damping constant K (at the recommended 100, one mouse notch is a 3.32x jump — the readout computes this live next to the slider); whether `deltaX` pans horizontally and whether shift-wheel is worth having; whether zoom anchors on the pointer or the viewport centre, and whether that amends or supersedes ADR 0011; the gesture idle timeout and whether zoom/pan enter history at all; the fall-through policy; the ambient transition duration; and ADR 0015's undefended ~250ms framing flight, which the ticket asks to judge in the same session as the zoom curve.
