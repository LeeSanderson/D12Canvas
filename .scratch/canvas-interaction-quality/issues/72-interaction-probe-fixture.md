# 72 — Interaction probe fixture and the press-plumbing probes

**What to build:** A new class of test inside the visual-test project, the `Interaction probe`, drives a real browser against a demo page and asserts DOM or component state rather than pixels (ADR 0025). It shares the demo-app and Playwright fixtures with the screenshot tests and proves only what the browser owns. This ticket builds the fixture conveniences a probe needs (a way to read what C# received, a way to assert nothing responded, a console-error trap) and the first set of probes over ticket 71's plumbing:

- the classification walk resolves the right role and entity for a press on each marked element
- no C# call arrives for a press that moves less than the threshold, and one arrives for a press that crosses it
- ten `pointermove` events dispatched in one frame arrive as one `OnPointerMoved`
- capture holds through a release outside the canvas, and each release channel (`pointerup`, `pointercancel`, window `blur`) ends the gesture
- `preventDefault` on `pointerdown` suppresses the browser's own focus move, the one browser fact ADR 0036 rests on
- a `lostpointercapture` on a live gesture writes `console.error` and the fixture fails the test

A probe never establishes a magnitude: synthetic input does not reproduce device granularity.

**Blocked by:** 65 (CI runs the visual job with `-parallel none`), 71 (Pointer arbitration spine)

**Status:** resolved

- [x] Probe classes live beside the visual tests and reuse the assembly fixtures; no third project
- [x] The fixture fails any test during which the page logged a `console.error`
- [x] Each of the six probes above exists and passes in the pinned image under `-parallel none`
- [x] A probe asserts behaviour (nothing responded to a buttonless move), never which gesture is live
- [x] `docs/agents/testing.md` describes how to run and add a probe
- [x] `CONTEXT.md`'s `Interaction probe` term describes what shipped

## Comments

`InteractionProbe` is the base class. It opens a new, unlinked `/interaction-probe-demo` page whose board is seeded with fixed entity ids: three rectangles, an edge with a label between two of them, and an edge with a floating end. An init script wraps `DotNet.DotNetObject.prototype.invokeMethodAsync` before Blazor loads, so a probe reads every call the page's JavaScript made into C# with `CallsToAsync`, after `SettleAsync` has waited for pending calls and two animation frames. `ExpectNothingRespondsToAButtonlessMoveAsync` sweeps an unpressed pointer over empty canvas and an instance. It fails if any pointer entry point is called or the viewport transform, selection or marquee changes. `ConsoleErrorTrap` collects `console.error` messages and uncaught page errors, and the base class fails the test on dispose if any arrived.

The six probes:

- `PressClassificationProbes` dispatches a middle-button press straight at each marked element in three selection states. It covers every role, including author content through an injected `<input>` and an injected opt-in marker. The middle button is used because the listener forwards it from every role and it does nothing on release.
- `MoveForwardingProbes` has three cases. A press that stays a pixel from its start sends a press and a release but no move. A press dragged well past the threshold sends moves. Ten `pointermove` events dispatched in one script turn arrive as one `OnPointerMoved` at the last position.
- `ReleaseChannelProbes` covers a pan released below the canvas, whose moves and release still arrive. A theory then ends a live pan by `pointerup`, a dispatched `pointercancel` and a window `blur`, and checks that a buttonless move draws no response afterwards.
- `PressFocusProbes` presses the same focusable element in two places. Outside the canvas, the browser focuses it. Inside `.canvas-content`, the walk finds no marker and the listener claims the press as bare canvas, so only the canvas receives focus.
- `LostCaptureProbes` releases capture mid-pan, asserts the `lostpointercapture` cancel and the `console.error`, and asserts that the trap would fail the test before clearing it.

The first focus probe pressed an instance with the secondary button and passed even with `preventDefault` removed from the listener. `ComponentContainer`'s own `@onmousedown:preventDefault` also suppresses the browser's focus move, so that probe could not see the listener. The injected element sidesteps the legacy handler until ticket 78 deletes it. Releasing capture from script also has no effect until the next pointer event, so the lost-capture probe moves the pointer once after the release.

Each probe was checked against a broken listener before it was trusted. Removing `preventDefault`, the drag threshold, the frame coalescing, the window `blur` listener or `setPointerCapture` each failed the matching probe. The full suite ran in the pinned image under `-parallel none`: all 23 probes passed, and no baseline moved. One run showed a truncated HTML snapshot for `EdgeRoutingAndArrowheadsVisualTests` with an equal PNG. That class passed when rerun on its own, and nothing in this change touches its page.

Not covered here: the LOD placeholder, which carries the `instance` role, and the second probe ADR 0036 owes, that a press on marked non-focusable author content leaves a multi-selection intact. That probe needs the primary button on author content, which still belongs to the legacy handlers. The trap's dispose wiring is exercised only by its use in every probe, not by a probe of its own. The ticket's "no C# call arrives" for a press inside the threshold is read as no move: the press itself always reaches C# by design.
