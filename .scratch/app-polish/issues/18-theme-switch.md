# Theme switch

Status: resolved
Blocked by: 17

Spec: [App polish](../spec.md), "Theme" and the shared dropdown in "Bootstrap delivery". Decision records: [App and canvas theme](05-app-and-canvas-theme.md), [Shared nav bar](07-shared-nav-bar.md), [Editor toolbar](08-editor-toolbar.md).

## What to build

A board author picks Light, Dark or System once in the app bar, and the whole App, chrome and canvas, follows it. The choice is remembered in this browser.

- One shared dropdown component that later menus reuse. Its shape comes from the prototypes: a toggle-button fragment plus a body `RenderFragment<Action>` that receives a close callback. It renders `.dropdown-menu.show` from component state with `data-bs-popper="static"`, supports end alignment and `.dropup`, closes on outside click and on Escape, and returns focus to its toggle on Escape.
- A theme menu in the app bar built on it, with Light, Dark and System. Its icon shows the current choice. In the offcanvas, picking a theme leaves the offcanvas open.
- The choice is stored through `IPreferences` as `d12canvas.theme` (`light`, `dark`, `system`); missing or unknown means `system`.
- An inline script in `index.html`'s `<head>` reads the key before Blazor starts, resolves `system` with `matchMedia`, and sets `data-bs-theme` and `data-d12-theme` on `<html>`. While the choice is `system`, it re-applies when the OS setting changes. The menu calls a small App JS module to write both attributes.
- The editor's Theme select and its own `data-d12-theme` are removed.
- The `--app-*` tokens and their `:root`, `prefers-color-scheme` and `[data-d12-theme]` blocks are deleted. App rules use Bootstrap variables and `.shadow-sm`. A colour with no Bootstrap equivalent gets one definition under `[data-bs-theme=dark]`.

## Acceptance criteria

- [x] Picking Light, Dark or System changes both the chrome and the canvas at once, on both routes.
- [x] The choice survives a reload, and a first visit uses System.
- [x] With System chosen, changing the OS setting updates the App without a reload.
- [x] A dark-mode reload doesn't flash light colours before Blazor starts.
- [x] The editor has no Theme select.
- [x] No `--app-*` token remains.
- [x] The dropdown closes on outside click and on Escape, and Escape returns focus to its toggle.
- [x] App tests through the page harness, with strict JS interop: each option writes `d12canvas.theme` and calls the theme module with the resolved mode; System is the default; the menu icon reflects a stored choice; the dropdown opens, closes on Escape and closes on outside click.

## Comments

Done. `Components/Dropdown.razor` is the shared dropdown. It takes a `Toggle` fragment and a `RenderFragment<Action>` body whose Action closes the menu. It also has `AlignEnd`, `DropUp`, `Caret` and `MenuClass`. While open, it renders a transparent fixed `.dropdown-outside` layer that closes it on click. Escape inside it closes it, stops the key reaching the offcanvas and focuses the toggle. `ThemeMenu` sits next to Suggest and keeps its choices in a `ThemeChoice` record. Picking writes `d12canvas.theme` through `IPreferences`, then calls `setMode` in `ThemeMenu.razor.js`. For System it asks the same module's `systemMode()` for the OS mode first. The `index.html` head script re-reads the stored key on each OS change, so the menu and the script never disagree about the choice.

The menu aligns to the end only from `md` up (`dropdown-menu-md-end`). In the offcanvas an end-aligned menu ran off the panel's left edge.
