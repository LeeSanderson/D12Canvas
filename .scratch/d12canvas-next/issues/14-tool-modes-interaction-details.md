# Tool modes & interaction details

Type: grilling
Status: resolved
Blocked by: 05, 09

## Question

Given the resolved component registration contract (05) and the resolved selection model (09), design tool modes and interaction details: keyboard shortcuts, context menus, and how "drawing" a new shape or connector actually feels (e.g. click-to-place vs. click-and-drag-to-size for a new component instance; a dedicated "connector" tool mode vs. dragging directly from a port). Decide what modes exist (e.g. select mode vs. a per-component-type placement mode) and how a user switches between them.

## Discussion

1. **Persistent modes vs. implicit/momentary.** Agreed: no persistent tool-mode state machine. Select is the only persistent state; placing a shape or drawing a connector is a single momentary gesture that reverts to select the instant it completes. Consistent with ticket 08's always-on ports (implying direct-drag-from-port, not a dedicated connector-mode toggle).
2. **Placing a new instance from the palette.** Two independent gestures, both supported (not one-or-the-other): drag-and-drop (drop point = position) *and* a plain click on a palette entry, which adds the instance immediately at a default position — no armed/preview intermediate state. Both use the registered `DefaultSize` (ticket 05).
3. **Default position for click-to-add.** Center of the current viewport, with a small cascading offset (e.g. `+20,+20` px) per successive click so repeated adds don't stack in a perfectly overlapping pile.
4. **Connector drawing.** Direct drag from a port (hover reveals ports on that instance); releasing over a valid target attaches, releasing on empty canvas leaves a floating endpoint. No dedicated connector tool.
5. **Connector via palette.** A built-in, fixed "Connector" entry in `CanvasPalette` — not registered via `RegisterComponent` (edges are their own entity type, ticket 08/ADR 0003) — creates a floating `Edge` using the same placement mechanics as a shape (drag-and-drop or click-to-add).
6. **Baseline keyboard shortcuts.** Delete/Backspace (delete selection), Escape (clear selection / cancel connector drag), Ctrl+Z / Ctrl+Shift+Z (undo/redo), Ctrl+G / Ctrl+Shift+G (group/ungroup), Ctrl+] / Ctrl+[ (forward/backward), Ctrl+Shift+] / Ctrl+Shift+[ (front/back). Copy/paste, duplicate, and select-all explicitly excluded — no underlying model decided yet.
7. **Context menu.** On a selection: Delete, Group/Ungroup (contextual on selection type), and the four z-index commands — same set as the shortcuts. No context menu on empty canvas.

## Answer

No persistent tool-mode state machine — select is the only persistent state; placing a shape or drawing a connector is a single momentary gesture, consistent with ADR 0005's always-on ports already implying direct-drag-from-port over a dedicated connector tool.

**Placing a new instance** supports both drag-and-drop (drop point = position) and click-to-add (instant, no armed/preview state), both using the registered `DefaultSize` (ADR 0001). Click-to-add's default position is the center of the current viewport with a small cascading offset per successive click, so repeated clicks don't stack instances exactly on top of each other — a fixed board-space origin was rejected since pan/zoom can put it off-screen.

**Connectors** are drawn by dragging directly from a port revealed on hover; releasing on a valid target attaches, releasing on empty canvas floats (ADR 0005). A connector is also available as a built-in, fixed `CanvasPalette` entry — not a `RegisterComponent`-registered type, since `Edge` is its own entity type (ADR 0003) — that creates a floating edge via the same placement mechanics as a shape.

**Baseline keyboard shortcuts** (Delete/Backspace, Escape, Ctrl+Z/Ctrl+Shift+Z, Ctrl+G/Ctrl+Shift+G, Ctrl+]/Ctrl+[, Ctrl+Shift+]/Ctrl+Shift+[) bind only to actions already decided in prior tickets. Copy/paste, duplicate, and select-all are explicitly excluded — none has an underlying model yet, so a shortcut now would spec ahead of a decision that doesn't exist.

**Context menu** on a selection offers the same action set as the shortcuts (Delete, Group/Ungroup, the four z-index commands); right-click on empty canvas opens no menu — there's no decided action to put there.

Seeded ADR [0009](../../../docs/adr/0009-tool-modes-and-interaction-model.md). No new `CONTEXT.md` terms — everything here composes existing vocabulary (Gesture, Palette, Port, Edge). Graduates the "Full keyboard-accessibility interaction" fog item into a new ticket, now that a baseline shortcut set and placement/connector model exist to build on top of.
