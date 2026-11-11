# Editor header controls and status bar

Status: ready-for-agent
Blocked by: 18, 19

Spec: [App polish](../spec.md), "Board editor page". Decision record: [Editor toolbar](08-editor-toolbar.md).

## What to build

The editor's toolbar row goes. Its controls move to the page header above the canvas and a status bar below it.

- Page header, in the more section, left to right, before the Autosave switch and save status:
  - Undo and Redo as an icon `btn-group btn-group-sm` (`arrow-counterclockwise`, `arrow-clockwise`, `btn-outline-secondary`).
  - A View dropdown on the shared dropdown component (`sliders` icon, "View" label from `sm` up, menu aligned to the end), in three groups split by dividers. Zoom: Zoom to fit, Zoom to selection, Actual size, each with its icon (`arrows-fullscreen`, `bounding-box-circles`, `aspect-ratio`) and its shortcut right-aligned in muted text. Snapping: Snap to grid and Object snapping as `form-switch` toggles. Mouse wheel: Auto, Mouse, Trackpad, with `check2` on the current one.
  - A Shortcuts toggle (`keyboard` icon, "Shortcuts" label from `lg` up), `btn-outline-secondary`, `btn-secondary` while open, with `aria-pressed`. It shows and hides the existing shortcuts overlay.
- Status bar: a 30px `footer` under the canvas (`bg-body-tertiary`, `border-top`, small muted text).
  - Left: "N selected" or "Nothing selected" in a `role="status"` element.
  - Right: Grid (`grid-3x3-gap`) and Snapping (`magnet`) text toggles with `aria-pressed`, emphasised when on and dimmed when off. Then zoom out (`dash-lg`), the zoom percentage as a `.dropup` menu holding the three zoom commands, and zoom in (`plus-lg`), all calling the canvas's existing zoom commands.
  - Below `sm`, the Grid and Snapping toggles are hidden.
- Every icon button has an `aria-label` and a native `title` with its shortcut, for example "Undo (Ctrl+Z)" and "Zoom in (PageUp)".
- The existing zoom readout component is replaced or reused by the status bar zoom control.

## Acceptance criteria

- [ ] The editor has no toolbar row.
- [ ] Undo and Redo in the header undo and redo canvas edits.
- [ ] The View menu's zoom commands, snapping switches and wheel profile act on the canvas, and the current wheel profile is checked.
- [ ] The Shortcuts button shows and hides the shortcuts overlay and reflects its state in `aria-pressed`.
- [ ] The status bar shows the selection count and updates as the selection changes.
- [ ] The status bar's Grid and Snapping toggles and the View menu's switches stay in step.
- [ ] Zoom out, zoom in and the zoom drop-up commands change the zoom, and the percentage updates.
- [ ] Below `sm`, the View button shows its icon only and the status bar hides Grid and Snapping.
- [ ] App tests through the page harness: the header shows Undo/Redo, View and Shortcuts on the editor and not on the list; selection count text; the Grid and Snapping toggles; zoom commands from the View menu and the status bar.
