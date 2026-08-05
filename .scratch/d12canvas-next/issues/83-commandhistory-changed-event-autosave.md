# 83 — CommandHistory.Changed event + optional autosave in D12Canvas.App

**What to build:** `CommandHistory` gains a new parameterless `Changed` event, firing on `Do`,
`Undo`, and `Redo` alike - all three mutate board content, so a host needs to know about any of
them, not just fresh gestures. `DiagramCanvas` re-exposes it publicly, mirroring the existing
`SelectionChanged` pattern: an internal signal on the class that owns the mechanism, surfaced as a
public event by `DiagramCanvas` for host/chrome code to subscribe to.

The event carries no payload and no "is modified" tracking of its own - the library only reports
"something changed"; whichever host is listening owns its own dirty bookkeeping (e.g. set modified
= true on the event, false again after a successful save). A richer "clean since last save, even
across undo back to the exact save point" model was considered and explicitly rejected as
unnecessary for now.

Building on that event, in `D12Canvas.App` (ticket 82):
- The board page's Save button becomes disabled whenever there's nothing unsaved - enabled again on
  the next `Changed` firing, disabled again after a successful save.
- An optional, off-by-default autosave: when enabled, persists via `IBoardStore.SaveAsync` a short
  debounce after `Changed` fires, coalescing rapid successive gestures (e.g. several quick nudges)
  into one save rather than saving on every single command.

**Blocked by:** 82 (D12Canvas.App)

**Status:** resolved

- [x] `CommandHistory.Changed` fires on `Do`, `Undo`, and `Redo`
- [x] `DiagramCanvas` re-exposes it publicly, same pattern as `SelectionChanged`
- [x] No payload; no library-side modified/dirty tracking - hosts own that bookkeeping themselves
- [x] `D12Canvas.App`'s Save button disables itself whenever there's nothing unsaved
- [x] `D12Canvas.App` has an optional, off-by-default debounced autosave built on the same event
- [x] bUnit coverage of the new `CommandHistory`/`DiagramCanvas` event itself - this is core-library
      surface, unlike ticket 82's app-level playground code, so it follows the repo's normal
      testing bar. Its `D12Canvas.App`-side consumption (autosave, button state) needs none, per
      ticket 82's own carve-out.

## Comments

`CommandHistory.Changed` fires from inside `Do`/`Undo`/`Redo` themselves (not at each of
`DiagramCanvas`'s ~20 call sites) and is skipped on a no-op `Undo`/`Redo` against an empty stack,
since nothing actually changed in that case. `DiagramCanvas` re-exposes it by subscribing to
`_history.Changed` in `OnInitialized` and unsubscribing in `DisposeAsync`, re-invoking its own
parameterless `Changed` event - the exact subscribe/re-raise/unsubscribe wiring `ZoomPanTracker.Changed`
→ `OnZoomPanChanged` → `ZoomOrPanChanged` already uses, just without ZoomPanTracker's extra
`EventCallback` parameter (matching `SelectionChanged`'s plain-event-only shape instead, per the
ticket).

`D12Canvas.App`'s `BoardEditor.razor` subscribes to the rendered `DiagramCanvas`'s `Changed` event
via the same reference-tracking pattern `PropertyPanel` uses for its own `Canvas` parameter, since
`@ref` isn't captured until a render after the page's own first one (the canvas only mounts once the
board finishes loading). Autosave debounces via a cancel-and-restart `CancellationTokenSource` behind
a `Task.Delay`, coalescing a burst of `Changed` firings into a single `SaveAsync` call.

Verified via new `HistoryTests`/`DiagramCanvasUndoRedoTests` bUnit cases, plus the same
headless-Chromium Playwright run noted on ticket 82: Save started disabled, enabled after placing a
component, disabled again after a manual Save, and - with Autosave toggled on - disabled itself again
~1s after a resize with no explicit Save click.
