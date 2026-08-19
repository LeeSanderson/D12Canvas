# A wheel event's meaning, its smoothing and its modifier set all follow from one device profile, guessed from delta granularity and overridable by the host

Today `DiagramCanvas` binds `@onwheel` and treats every wheel event as zoom via `ZoomPanTracker.SetScale(_scale ± 0.1)`: `DeltaX` is discarded, magnitude is discarded, `CtrlKey` is never read, and nothing cancels the browser's own scroll and Ctrl-zoom underneath. This replaces all of it.

The listener moves into `DiagramCanvas.razor.js` as a **non-passive `wheel` listener on the container element** — the `@onwheel` binding cannot cancel anything on .NET 9 — and feeds `(anchorPoint, scaleFactor)` or `(dx, dy)` into the canvas, never a `WheelEventArgs`. Pinch is not distinguishable from Ctrl+scroll in any engine and no attempt is made to tell them apart; both mean "zoom about the cursor".

## The profile, and why one switch drives three things

`DiagramCanvas` exposes **`WheelDeviceProfile`: `Auto` (default) | `Mouse` | `Trackpad`**. Three states rather than a boolean, so "guess" is a first-class value rather than a null. The profile decides:

1. **What a plain wheel means** — zoom on `Mouse`, pan on `Trackpad`.
2. **The ambient transition duration** — 100ms on `Mouse`, 0ms on `Trackpad`.
3. **Whether Shift is bound at all** — `Mouse` only.

These are not three coincidences bundled for convenience. Every one of them follows from **delta granularity**, which is the single physical fact the profile names. A mouse notch arrives as a discrete 100px step; a trackpad arrives fractional and fast.

That is also why `Auto` is sound rather than a heuristic that happens to correlate. The tell — integral versus fractional deltas — *is* granularity, and granularity *is* why the smoothing exists. `Auto` is not guessing a device and then looking up an unrelated constant; it measures the thing the constant is for, and "device" is the human-legible name for it. A misclassification is therefore self-correcting in the direction that matters: anything sending coarse integral deltas gets smoothing precisely because it sends coarse integral deltas.

**`Auto` classifies at wheel-gesture start and holds for that gesture.** Per-event classification is wrong: a trackpad occasionally emitting a whole-numbered delta would flip smoothing on for a single frame and produce a visible hitch mid-scroll. Latch-once-forever is also wrong, and for a case that is not exotic — one person, one machine, a docked laptop with an external mouse and an undocked trackpad on the train. A stored preference is silently wrong half the time, because it was set months ago and nobody connects today's bad feel to it. Per-gesture is stable where stability matters and adaptive where adaptivity matters.

**`Mouse` and `Trackpad` pin and disable the guess.** The user has spoken; stop second-guessing them.

**The host owns the control and the persistence, not the library.** D12Canvas renders no preference UI and remembers nothing across sessions. This is not a new principle but the existing host-owns-storage boundary applied to one more thing, and it keeps the chrome-versus-board-content line intact. The switch exists; the library simply does not own its surface.

## The mapping

| | plain | Shift | Alt | Ctrl/Cmd |
|---|---|---|---|---|
| **Mouse** | zoom | horizontal pan | vertical pan | zoom |
| **Trackpad** | pan (both axes) | *unbound* | no-op | zoom |

**Alt is device-independent by construction, and that is what makes it safe.** It maps to "pan both axes" in both profiles. On a mouse there is no `deltaX`, so panning both axes *is* vertical pan — the mouse user gets a complete set (zoom, horizontal, vertical) with no profile-specific case. On a trackpad it is a deliberate no-op, because a plain swipe already pans both axes.

That immunity is the point. A Windows Precision Touchpad reporting `altKey` spuriously would be behaviourally invisible here. Narrowing Alt to vertical-only would have *created* the hazard: a plain swipe misread as Alt would silently lose its horizontal component. The broad binding is the one that cannot fail this way. Alt release was measured on Windows and does not steal focus or raise the menu bar.

**Shift is unbound on `Trackpad`** because constraining to one axis is only meaningful on a device that cannot do both at once. A trackpad already pans both axes simultaneously, so the modifier has no work to do. This must be gated on the **profile, not the event**: detecting it per-event via `deltaX !== 0` breaks immediately, because a trackpad swiping perfectly vertically reports `deltaX: 0` on exactly those events and Shift would spuriously engage mid-swipe.

## Zoom is multiplicative about the pointer

`scale *= exp(-deltaY / K)` with **K = 600**, giving **1.18× per notch**, inside the 1.1–1.25× band the reference tools occupy. A fixed additive step is rejected: multiplicative response handles a coarse mouse notch and a fine trackpad pinch with the same expression, a fine delta yielding a fine factor.

**K = 100 is not a tuned value and must not be mistaken for one.** It is the engine's own inverse constant, and on a real notch it produces a factor of exactly *e* — a 2.72× jump per notch, far too violent to use.

**Zoom anchors on the pointer, not the viewport centre.** All four reference tools do, the anchor invariant holds here to three decimal places, and anchored zoom is what makes the content feel attached to the input.

Two traps for anyone testing this. **A real notch is 100px, not 120.** Windows' raw `WHEEL_DELTA` is 120, but the engine converts a notch to three lines of ~33.3px before it reaches the event; a synthetic wheel injected by a test harness skips that conversion, so a green assertion on 120 does not describe the device, and any constant tuned against it inherits a 20% error. And **the pointer is dispatched at whole pixels**, so a fractional anchor lands up to half a pixel from where it was aimed, which the zoom factor then multiplies into a visible board-space discrepancy.

## The two durations are opposite in kind

**Ambient transition — 100ms on `Mouse`, 0ms on `Trackpad`.** This is smoothing, and it exists to interpolate coarse discrete input. It was measured across both operations on both devices and tracks the device, not the operation: both mouse cells want 100 and both trackpad cells want 0, because a 100px notch is a big discrete step whether it zooms or pans.

Today a blanket `transition: transform 0.1s ease-out` on `.canvas-content` applies to **every** transform write, including a pointer drag, where it is pure lag — the canvas does not track the pointer, it eases toward it. That blanket rule ends here. The ambient duration is chosen as the **lowest** value that removes the jumpiness, because every millisecond above it is lag on input the user is actively driving.

**Framing flight — 250ms**, confirmed by feel. This is the opposite kind of constant: nobody is driving a framing flight, so its duration is doing useful spatial communication rather than getting in the way. Too short and the viewport teleports, losing the connection between where you were and where you arrived. The two durations must never be anchored to each other.

## A wheel gesture is not a history entry

**Wheel zoom and pan stay out of undo history**, matching every reference tool. Undoing a pan is usually the opposite of what is wanted — the user panned in order to *look at* the thing they are about to undo.

This means the undo model is **untouched rather than amended**. A wheel gesture never becomes a `Gesture` in the history sense, so "one gesture, one history entry" needs no stretching to accommodate a momentum tail delivering dozens of events after the user let go.

**The gesture boundary survives with a different job.** A short idle timeout closes a wheel gesture, with `momentum === true` as an optional early terminator where the engine supplies it. Its original consumer was undo granularity, which no longer exists; its remaining consumer is the `Auto` classification boundary. **300ms**, and it is low-stakes by construction — reclassifying a gesture from the same device yields the same answer, so only two loose constraints bind: above the inter-notch gap of a comfortable mouse scroll so continuous scrolling does not fragment, and below the window in which alternating devices would be misclassified. Recorded explicitly as a *classification* boundary so it is never re-tuned against the consumer that was removed.

## The canvas always captures the wheel

`preventDefault` on every wheel event the canvas receives. A canvas embedded in a scrolling host page therefore becomes a dead zone for page scrolling while the pointer is over it. That limitation is accepted and documented rather than pre-solved, and revisited if it is actually hit.

The apparently balanced middle option is a trap and is rejected outright: capturing only when the action is zoom would let a `Trackpad` user's plain swipe fall through, so the canvas never pans and the host page scrolls instead — breaking the profile this decision rests on.

**The policy is derived synchronously in JavaScript** and may only read facts the listener already holds. It cannot wait for an interop round trip, so nothing about selection state or canvas state is available to it. Since the policy is now unconditional, this costs nothing — but it constrains any future relaxation, which must remain expressible from the event and the profile alone.

## What this confirms and what it leaves alone

**The unbounded-zoom/LOD/grid decision is confirmed and untouched, and its scope is worth stating plainly** because this was mis-predicted while charting. That ADR owns the zoom *range* — min and max as host parameters, replacing hardcoded constants — plus the LOD cutoff and the adaptive grid. It says nothing about how a zoom is applied: no anchoring, no response curve, no input mapping. Today's fixed `±0.1` step is code, not a decision recorded there. So this ADR neither amends nor supersedes it; it occupies ground that was left empty.

**The undo/redo history model is confirmed**, per the section above.

**The viewport-commands ADR's ~250ms framing flight is now defended by feel** rather than by judgement, and its assumption that framing needs its own duration separate from the ambient one is upheld — with the refinement that the ambient side is not one number but two.

**The pointer gesture arbitration model is untouched.** A wheel is not a press and takes no part in gesture ownership. Its banked constraint — that every input to a synchronous decision must be a fact the listener already holds — is satisfied here as it was there.

## Considered and rejected

- **Plain wheel always zooms** (today's behaviour). Judged against a fair test with the ambient transition removed. It leaves a trackpad user with no natural pan on the device where panning is the more frequent gesture.
- **Plain wheel always pans, Ctrl inverting to zoom** — what three of the four reference tools default to. Rejected not on feel but because a mouse-wheel user has no second axis and expects a wheel to zoom on a canvas; the profile serves both rather than picking one.
- **The preference as the sole source of truth**, with no guess. Rejected: it makes every user configure the library before it feels right, and the docked-laptop case makes a stored answer silently wrong.
- **Per-event device classification.** Rejected — one stray integral delta from a trackpad flips smoothing for a frame and hitches mid-scroll.
- **Alt as vertical-only pan.** Rejected; narrower, needs a profile-specific case, and creates the spurious-`altKey` hazard the broad binding is immune to.
- **Shift as an axis lock on `Trackpad`** (use `deltaX`, discard `deltaY`), and **Shift as an axis sum** (today's `deltaY + deltaX`, which folds vertical motion into horizontal and surprises on a diagonal swipe). Both rejected in favour of leaving Shift unbound where the device needs no constraint.
- **A host-configurable capture policy.** Rejected for now as one knob too many; always-capture with a documented limitation is the honest position until the embedded-figure case actually bites.
- **Deriving the capture policy from canvas state.** Foreclosed by the synchronous-JS constraint regardless of desirability.
