# What remains of ComponentContainer's edit mode

Type: grilling
Status: open

## Question

Decide what `ComponentContainer`'s edit mode is for, once ADR 0018 has taken every gesture off it.

ADR 0018 makes the canvas the sole owner of pointer gestures and deletes the `_editMode`-gated `_isDragging`/`_isResizing` pair — a second, parallel drag/resize implementation that predates the Board-backed canvas and is reachable only from `/componentcontainer-demo`. But `_editMode` does not die with it. It still gates `ShowSelectionOverlay => (IsSelected && !IsMultiSelected) || _editMode`, still drives the `edit-mode`/`view-mode` class pair on the container root, still has a public `InitialEditMode` parameter, and still rides in `ComponentContainerStateChangedEventArgs.IsEditMode`.

So the concept survives as a *rendering* mode with no gestures behind it, which is either a legitimate standalone-usage feature or a vestige — and it is currently impossible to tell from the code which.

Decide:

- **Whether edit mode is a concept the library keeps at all.** If a container's selection overlay is already governed by `IsSelected`, what does `_editMode` add beyond a second route to the same visuals? Note ADR 0017 made affordance rendering and hit participation one predicate; a mode that shows affordances without a gesture behind them would violate the spirit of that, since an overlay you can see but whose handles do nothing is worse than no overlay.
- **What `/componentcontainer-demo` is demonstrating.** It is nav-linked and has no visual-test coverage of its own, so nothing currently pins its behaviour. If a standalone container outside a `Board` is a supported usage, it needs a stated contract and a test; if it is not, the page and the parameter go.
- **Whether `InitialEditMode` and `ComponentContainerStateChangedEventArgs.IsEditMode` are public API a host may be relying on**, and what removing them costs. `D12Canvas` has no released-version story recorded, which makes this cheaper than it looks — confirm that rather than assume it.
- **What happens to the `edit-mode`/`view-mode` CSS class pair**, which is the piece most likely to move visual-test baselines. Any change here touches rendered markup and a shared `<style>` block, so `AGENTS.md`'s full Playwright run in the pinned image applies.

This is deliberately not part of ADR 0018's implementation scope: deleting the gesture pair is mechanical and follows from the ADR, while deciding whether a *mode* survives is a separate call with its own public-API consequences.
