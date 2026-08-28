# Whether window `blur` actually fires on a focus steal mid-gesture

Type: task
Status: open
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
