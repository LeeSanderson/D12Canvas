# 77 — Wheel device profile and the viewport under a live gesture

**What to build:** A mouse wheel zooms about the pointer and a trackpad swipe pans, chosen from how the input arrives, and the viewport stays live during any press (ADRs 0019, 0056).

- `WheelDeviceProfile` (`Auto`, `Mouse`, `Trackpad`) is a canvas parameter. It decides plain-wheel meaning (zoom on mouse, pan on trackpad), the ambient transform transition (100ms mouse, 0ms trackpad) and whether Shift binds to horizontal pan (mouse only). Alt pans both axes on both, Ctrl zooms on both, pinch arrives as Ctrl+wheel. Zoom is `scale *= exp(-deltaY / 600)` about the pointer. `Auto` classifies on delta granularity at `Wheel gesture` start (300ms idle boundary, `momentum` as early terminator) and holds for the run. The listener is a non-passive JavaScript `wheel` listener on the container that always prevents default, because `@onwheel:preventDefault` is a silent no-op on the pinned runtime. The blanket transform transition on the content element ends. Wheel changes never enter history. The host owns any control and persistence.
- The press point is anchored in board space, so whatever the pointer holds stays under it through a wheel zoom or pan mid-drag. A viewport change re-runs the gesture tick at the last pointer position with zero velocity and promotes a `pointing` press to `active`. During `Pan`, an outside viewport change re-anchors the pan so the two add.
- The App's `BoardEditor` gains a settings control bottom-left exposing the profile.

**Blocked by:** 74 (MoveSelection, the gesture preview and live geometry)

**Status:** resolved

- [x] With `Mouse`, a wheel notch zooms about the pointer multiplicatively and Shift+wheel pans horizontally; with `Trackpad`, a plain wheel pans with no easing and Ctrl+wheel zooms
- [x] `Auto` holds one classification for a run and re-classifies after the idle boundary
- [x] Ctrl+Z after a wheel zoom undoes the last board edit, never the zoom
- [x] Wheel-zooming mid-drag keeps the dragged shape under the pointer and the drag continues; PageUp mid-drag does the same
- [x] A press still `pointing` is promoted to `active` by a viewport change
- [x] Constants are asserted by relationship (zoom factor is multiplicative, trackpad transition shorter than mouse), never by value; a probe proves a wheel with a button held reaches the canvas and that the listener prevents default
- [x] The wheel handler on the canvas element is gone and `ZoomPanTracker` gains a zoom-about-a-point operation
- [x] The App exposes the profile control; the demo gains a page or parameter for the visual suite
- [x] Full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [x] `CONTEXT.md`'s `Wheel gesture` and `Wheel device profile` terms describe what shipped
