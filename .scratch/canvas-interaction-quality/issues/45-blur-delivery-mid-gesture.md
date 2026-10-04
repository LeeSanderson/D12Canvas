# Whether window `blur` actually fires on a focus steal mid-gesture

Type: task
Status: resolved
Blocked by:

## Question

Establish, by hand in a real browser, whether a window `blur` event is delivered when the user `Alt`+`Tab`s away (or clicks another application) with a pointer button held and a captured gesture live — and whether it is still delivered when the press called `preventDefault`.

ADR 0031 makes interruption a cancel and adds a window `blur` listener as the third channel, because pointer capture survives window focus loss and neither `pointercancel` nor the `lostpointercapture` net fires on that path. The decision to *cancel* rather than commit does not depend on this fact. Whether the leak is actually closed does.

The failure this closes is real and silent: drag a shape, `Alt`+`Tab`, release the button over another application, come back, move the pointer, and the drag resumes minutes later. That is leak path seven, the one door ADR 0018 left unwatched after ticket 04 converted the other six into an observable event.

This is not assertable by the suite. ADR 0025 can prove the plumbing — dispatching the event with a live gesture reverts it — and explicitly puts device physics out of reach, which is the wall ADR 0019 hit with the synthetic 120px notch and ADR 0029 hit with the operating system's cursor clamp. Both were settled by a hand-driven probe, and ticket 04's harness on `research/gesture-leak-probe` is the closest starting point.

Establish:

- **Does `window` fire `blur` on `Alt`+`Tab` with a button held**, in Chromium, Firefox and WebKit. Ticket 04's finding that all six gestures leak was engine-independent; this may not be.
- **Does `preventDefault` on the press change the answer.** ADR 0018 calls `preventDefault` on every captured press, and the reason the focus-transfer gap existed at all is that suppressing the default suppresses focus movement. If it suppresses blur delivery too, the channel is dead on the exact path it was added for.
- **What arrives on return.** If `blur` does not fire, the next `pointermove` after the user comes back is the first signal, and `event.buttons === 0` on a live gesture is a fallback the model can already express — ADR 0025 defines a leaked gesture behaviourally as a response to a buttonless pointer, which is the same test.
- **Whether `visibilitychange` covers a case `blur` does not**, notably a tab switch within the same window versus a switch to another application.

If `blur` proves unreliable, the fallback above is the likely answer and it needs no new event source, only a guard on a move C# already receives. Record which one shipped.

## Comments

- Parked 2026-10-03 because the dev was on mobile. A standalone probe page is ready at [probes/blur-probe.html](../probes/blur-probe.html). It logs `blur`, `focus`, `visibilitychange`, `pointercancel`, `lostpointercapture` and the first move's `buttons` per attempt, with `preventDefault` and `setPointerCapture` as toggles. Attempts to run in Chrome, Edge and Firefox: hold and `Alt`+`Tab` away, release, return and move; the same with `preventDefault` off; `Ctrl+PgDn` to another tab instead; optionally `Win+D`. Then **Copy results** and paste them into the session. WebKit and macOS cannot be driven by hand here and stay unmeasured, as in ticket 41.
- Parked again 2026-10-03, dev still on mobile. Status set to `parked` so frontier scans skip it. Unpark by setting `Status: open` from a desktop session, then run the attempts above.
- 2026-10-04, Chrome 153 on Windows, attempt 1 (`preventDefault` on, capture on, `Alt+Tab` away, release away, return and move):
  - `blur` fired 634ms after the press, with the gesture live. `preventDefault` on the press did not suppress it.
  - Two `pointermove` events with `buttons=1` arrived *after* the blur (at +18ms and +1253ms), while the `Alt+Tab` switcher was up. A cancelled gesture must ignore moves that follow its cancel.
  - `visibilitychange -> hidden` fired 1.3s after the blur, when the other window covered this one.
  - Nothing fired at the release, which happened in another app.
  - On return, in order within 30ms: `focus`, `visibilitychange -> visible`, `lostpointercapture`, then the first move with `buttons=0`. So `lostpointercapture` does fire on this path in Chrome, but only on return, which can be minutes after the release.
  - The final `pointerup buttons=0` is the click on Copy results, not part of the trial.
- 2026-10-04, Chrome 153 on Windows, attempt 2 (`preventDefault` **off**, otherwise as attempt 1): the same sequence. `blur` fired 1s after the press with the gesture live, one `buttons=1` move followed it, `hidden` came 1.7s later. On return `focus` and `visible` fired, then nothing for about 1s until the pointer moved, and only then `lostpointercapture` and the `buttons=0` move together. So `preventDefault` makes no difference to blur delivery in Chrome. It also corrects attempt 1: `lostpointercapture` is tied to the first move after return, not to the return itself.
- 2026-10-04, Chrome 153 on Windows, attempt 3 (`Ctrl+PgDn` to another tab in the same window, `preventDefault` on, capture on; the page was reloaded so the trial reads as 1 with the old label): `blur`, `lostpointercapture` and `visibilitychange -> hidden` all fired within 5ms of the switch, with the gesture live. On return `visible` then `focus`, and the first move had `buttons=0`. So a tab switch fires `blur` as well, and here Chrome also releases capture at once rather than on return. `visibilitychange` covered no case that `blur` missed.
- 2026-10-04, the dev ran the same attempts in Edge and Firefox on Windows and reported identical results. Logs were not pasted.

## Answer

**Window `blur` is reliable, and it is what ships.** In Chrome 153, Edge and Firefox on Windows it fires while the gesture is live, on `Alt+Tab` to another app and on `Ctrl+PgDn` to another tab, whether or not the press called `preventDefault`. `visibilitychange` always came after it and caught nothing it missed, so no listener is added for it. No `buttons === 0` guard is added either: where `blur` is missing, the gesture stays live until `lostpointercapture` and ADR 0018's net reverts it loudly. WebKit and macOS stay unmeasured.

The probe also showed that the release never reaches the page, which broke ADR 0031's rule that a cancelled gesture holds the press until its button comes up. Under ADR 0038 that would block board and selection keys after `Alt+Tab` back until the first mouse move. The dev chose to have a blur cancel end the press at once: clear the gesture, then release capture. Escape and a `Board` swap keep the hold. Recorded as ADR 0066, with addenda on ADRs 0031 and 0038 and the `Pointer gesture` entry in `CONTEXT.md`. ADR 0031's claim that `lostpointercapture` never fires on this path is corrected there: it fires at once on a tab switch and at the first move back after `Alt+Tab`.
