# 106 — Framing commands and the initial fit

**What to build:** Zoom to Fit, Zoom to Selection and Zoom to 100% on Shift+1, Shift+2 and Shift+0 get the user their bearings in one keystroke, and a board opens framed on its content rather than on empty canvas (ADR 0015 framing half, 0033). `Framing` lives inside `ZoomPanTracker` so one `Changed` fires with scale and pan paired: contain, centred, inset to 0.9 of the container rect, never past 1.0; empty board and empty selection are strict no-ops; 100% is centre-preserving. There is no reset view, no inset API and no opt-out. `Content extent` is instances unioned with resolvable edge endpoints, derived on demand. The state jumps and a CSS transition on the content element animates it over about 250ms; pointer events are suppressed for the flight keyed off the transition's lifecycle, never a timer, and a flight mid-press skips the guard. The canvas frames all content when a `Board` is first set, unanimated. Framing is reachable through the public tracker, which is how a host compensates for chrome it floats over the canvas. The visual suite applies reduced motion suite-wide (a rule that may only zero durations), with one case running the animated path to assert pointer suppression is applied during and cleared after. Zoom to Fit and Zoom to 100% join the canvas menu, Zoom to Selection the object menu.

**Blocked by:** 77 (Wheel device profile and the viewport under a live gesture), 89 (Composed context menu with shortcut hints)

**Status:** resolved

- [x] Shift+1 frames all content with a margin; Shift+2 frames the selection; Shift+0 returns to 100% keeping the centre; each is one short flight
- [x] A board authored far from the origin opens with its content visible, with no animation
- [x] A press during a flight is ignored and a press after it lands; a flight started mid-drag does not interrupt the drag
- [x] The three methods exist on the canvas and framing exists on the tracker; the host can call `SetPanPosition` after a fit to compensate for floated chrome
- [x] Framing a board containing only a floating edge frames that edge
- [x] Pure C# tests for framing and extent; bUnit for the chords and rows
- [x] The visual suite sets reduced motion in its browser context; one test opts back in and asserts suppression during and after the flight
- [x] The initial fit changes the opening view of every board-mounting baseline, which is planned churn; full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [x] `CONTEXT.md`'s `Framing` and `Content extent` terms describe what shipped

Shipped with four choices the ticket did not spell out. The canvas draws no board content until the container has been measured, which is also when the initial fit lands, so a board never shows first at the view the fit replaces; a container first measured at 0 by 0 is fitted on its first resize to a real size. The canvas re-renders on every tracker change, so a host's `SetPanPosition` or `Pan` after the fit shows at once. The three commands do nothing during `Port placement`, which owns the keyboard. `ZoomPanTracker` gains `Frame(bounds)` as its public framing entry, with the centre-preserving scale write internal.

The pan that brings a new inline edit into view now flies too, which closes ticket 101's carry-over. The flight is a 250ms inline transition plus a `data-d12-flight` marker on the content element; the pointer listener adds `d12-in-flight` to the container from the marked transition's `transitionrun` to its end or cancel, unless a press is held. The library's own stylesheet zeroes the content's transition under `prefers-reduced-motion`, and every browser context in the visual suite now sets reduced motion; `FramingFlightProbes` opts back in and pauses the transition to press during it.

A new `/framing-demo` page, a board authored around (50000, 50000), backs a baseline of the opening view and the probes for the real `Shift+1`, `Shift+2` and `Shift+0` key paths, the focus guard, an inline editor typing `!`, the guard during a flight and its absence under a held press.
