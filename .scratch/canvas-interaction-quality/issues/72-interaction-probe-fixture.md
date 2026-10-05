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

**Status:** ready-for-agent

- [ ] Probe classes live beside the visual tests and reuse the assembly fixtures; no third project
- [ ] The fixture fails any test during which the page logged a `console.error`
- [ ] Each of the six probes above exists and passes in the pinned image under `-parallel none`
- [ ] A probe asserts behaviour (nothing responded to a buttonless move), never which gesture is live
- [ ] `docs/agents/testing.md` describes how to run and add a probe
- [ ] `CONTEXT.md`'s `Interaction probe` term describes what shipped
