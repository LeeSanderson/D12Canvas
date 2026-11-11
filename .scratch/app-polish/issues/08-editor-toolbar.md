# Editor toolbar

Type: prototype
Status: resolved
Blocked by: 07

## Question

What does BoardEditor's toolbar look like once the nav bar exists?

Today it is one row of text buttons (Undo, Redo, Fit, Selection, 100%), checkboxes (Snap to grid, Object snapping), selects (Wheel, Theme) and Shortcuts/Suggest. Prototype a Bootstrap version: icon buttons with tooltips and shortcut hints, button groups, a settings dropdown for snapping and wheel profile, and the narrow-screen layout. Settle which controls moved to the nav bar.

The Theme select is already settled: it leaves the toolbar for the nav bar (see [App and canvas theme](05-app-and-canvas-theme.md)).

The nav bar is settled (see [Shared nav bar](07-shared-nav-bar.md)). Suggest and Theme leave the toolbar for the app bar. Autosave, Save and the save state go into the page header above the toolbar. The toolbar keeps undo/redo, zoom, snapping, wheel profile and Shortcuts.

## Answer

Decided with the user on 2026-11-11 after trying three variants. The prototype is on the `prototype/editor-toolbar` branch, built on top of `prototype/shared-nav-bar`: variant A (a toolbar strip under the page header), B (floating pills on the canvas) and C (page header plus status bar). C won, with one change: Shortcuts is its own button instead of an item in the View menu.

1. **No toolbar row.** BoardEditor's toolbar row goes away. Its controls split between the page header (above the canvas) and a status bar (below it).
2. **Page header, editor controls.** BoardEditor fills the header's "more" section, left to right, with:
   - Undo and Redo as an icon `btn-group btn-group-sm` (`arrow-counterclockwise`, `arrow-clockwise`, `btn-outline-secondary`).
   - A View dropdown (`sliders` icon, "View" label from `sm` up, menu aligned to the end). It holds three groups split by dividers. Zoom has Zoom to fit, Zoom to selection and Actual size, each with its icon (`arrows-fullscreen`, `bounding-box-circles`, `aspect-ratio`) and its shortcut right-aligned in muted text. Snapping has Snap to grid and Object snapping as `form-switch` toggles. Mouse wheel has Auto, Mouse and Trackpad, with a `check2` mark on the current choice.
   - A Shortcuts toggle button (`keyboard` icon, "Shortcuts" label from `lg` up). It is `btn-outline-secondary`, turns `btn-secondary` while the panel is open, and sets `aria-pressed`. It shows and hides the existing shortcuts panel, which stays an overlay on the canvas.
   - The Autosave switch and the save status/Save button from [Shared nav bar](07-shared-nav-bar.md) follow.
3. **Status bar.** A 30px `footer` under the canvas (`bg-body-tertiary`, `border-top`, small muted text):
   - Left: the selection count, "N selected" or "Nothing selected", in a `role="status"` element.
   - Right: Grid and Snapping text toggles (`grid-3x3-gap`, `magnet`) that set `aria-pressed` and look emphasised when on and dimmed when off. Then a zoom control: zoom out (`dash-lg`, PageDown), the zoom percentage as a drop-up menu with the same three zoom commands, and zoom in (`plus-lg`, PageUp). Zoom in and out call the canvas's existing `OnZoomOut`/`OnZoomIn`.
   - This settles where the selection count and zoom readout live. Snap to grid and object snapping appear twice, in the View menu and the status bar, on purpose.
4. **Tooltips.** Native `title` attributes that carry the shortcut, for example "Undo (Ctrl+Z)" and "Zoom in (PageUp)". The user is fine with native titles, so the spec adds no tooltip component.
5. **Narrow screens.** The page header wraps (see [Shared nav bar](07-shared-nav-bar.md)). Below `sm` the View button shows its icon only, and the status bar hides the Grid and Snapping toggles. Snapping is then reached through the View menu. Selection count and zoom stay.
6. **Dropdowns.** View and the zoom drop-up use the same Blazor-state dropdown as the theme menu: `.show` from component state, `data-bs-popper="static"`, `.dropup` for the status bar. The prototype's generic `PrototypeDropdown` (button fragment plus a `RenderFragment<Action>` body that gets a close callback) is a reasonable shape for one shared dropdown component. The spec still owes Escape and focus handling, as noted in [Shared nav bar](07-shared-nav-bar.md).

Prototype shortcuts the spec should not copy: icon paths are pasted into the prototype `Icon` component's dictionary, the snap switch ids carry the variant key, and variants A and B plus the `?variant=` switcher are throwaway.
