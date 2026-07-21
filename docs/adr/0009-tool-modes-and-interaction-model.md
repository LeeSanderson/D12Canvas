# No persistent tool modes — placement and connector creation are momentary drag/click gestures, backed by a baseline keyboard-shortcut and context-menu set

There is no persistent tool-mode state machine (no "Select" / "Rectangle" / "Connector" toggle the user switches into and stays in). **Select is the only persistent state.** Placing a new component instance or drawing a connector is a single momentary gesture that completes (or cancels) in one motion and leaves the app back in select state — there is nothing to "switch out of." This keeps the interaction model consistent with ADR 0005's always-on ports, which already implies connectors are drawn by dragging directly from a port rather than first activating a dedicated tool.

**Placing a new instance from `CanvasPalette`** supports two independent gestures, not one at the exclusion of the other:
- **Drag-and-drop**: dragging a palette entry onto the board places a new instance at the drop point.
- **Click-to-add**: clicking a palette entry places a new instance immediately, with no intermediate armed/preview state — a plain click is a complete gesture, not the first half of one.

Both use the component type's registered `DefaultSize` (ADR 0001). The **default position** for click-to-add is the center of the current viewport, with a small cascading offset (e.g. `+20, +20` px) applied per successive click, so repeatedly clicking the same entry doesn't stack instances in a perfectly overlapping pile. A board-fixed origin was rejected because `DiagramCanvas` supports pan/zoom — a fixed board coordinate can land off-screen once the user has panned away, while a viewport-relative default is always visible by construction.

**Drawing a connector** happens by dragging directly from a port: hovering a component instance reveals its ports (ADR 0005), and dragging from one starts an edge. Releasing over a valid target (another port, or a component) attaches that end; releasing over empty canvas leaves it as a floating endpoint — both already valid `Edge` states per ADR 0005.

**A connector is also available in `CanvasPalette`** as a built-in, fixed entry — not something a host registers via ADR 0001's `RegisterComponent`, since `Edge` is its own entity type (ADR 0003), not a component type. Dragging or clicking this entry creates a new `Edge` with both endpoints floating, placed with the exact same mechanics as a shape (drop point, or viewport-center-plus-cascade on click). The user then drags each floating endpoint onto a port to attach it, same as any other floating edge.

**Baseline keyboard shortcuts** bind only to actions already decided in prior tickets — no new action gets introduced solely to have a shortcut:

| Shortcut | Action | Decided in |
|---|---|---|
| Delete / Backspace | Delete selected instance(s)/group | ADR 0006 |
| Escape | Clear selection, or cancel an in-progress connector drag | ADR 0006 / this ADR |
| Ctrl+Z / Ctrl+Shift+Z | Undo / Redo | ADR 0007 |
| Ctrl+G / Ctrl+Shift+G | Group / Ungroup | ADR 0006 |
| Ctrl+] / Ctrl+[ | Bring forward / send backward | ADR 0008 |
| Ctrl+Shift+] / Ctrl+Shift+[ | Bring to front / send to back | ADR 0008 |

**Context menu** (right-click): on a selection, the menu offers the same action set as the shortcut table — Delete; Group (2+ ad-hoc selected) or Ungroup (a `Group` selected); and the four z-index commands. Right-click on empty canvas opens no menu at all — there is no decided action that would belong there.

**Considered and rejected:**
- **Persistent, explicit tool modes** (a toolbar of Select/Rectangle/Connector-style tools the user switches into) — rejected; it would need to hang around as sticky state, contradicting the momentary-gesture model, and adds mode-switching UI/keyboard infrastructure nothing else in this design calls for.
- **Click-to-arm-then-click-to-place** (click a palette entry, a ghost preview follows the cursor until a second click commits it) — rejected in favor of instant click-to-add; an armed/preview state is itself a small persistent mode, which is exactly what this design avoids elsewhere.
- **Click-and-drag-to-size** a new instance while placing it — rejected for now; every component type already has a registered `DefaultSize` (ADR 0001), and free-form sizing during placement adds real complexity (minimum size, aspect ratio, what a too-small release does) nothing currently requires. Can be layered on later without revisiting this decision.
- **A dedicated connector tool mode** — rejected; contradicts the momentary-gesture model, and ADR 0005's always-on ports already make direct-drag-from-port the natural gesture.
- **Fixed board-space origin as the click-to-add default position** — rejected; can place the new instance off-screen once the user has panned away from the origin.
- **Copy/paste, duplicate, select-all shortcuts** — out of scope here; none has an underlying model decided yet (clipboard format, duplicate-position semantics, "select all" semantics under grouping), so binding a shortcut now would be speccing ahead of a decision that doesn't exist.
