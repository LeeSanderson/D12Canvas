# App and canvas theme

Type: grilling
Status: resolved
Blocked by: 01

## Question

How do the App's light/dark/system mode and the canvas theme relate?

Today BoardEditor has a Theme select (System, Light, Dark) that sets `data-d12-theme` on the editor only. The board list has no theme at all.

Settle: one switch for App chrome and canvas, or two; where the switch lives (nav bar, settings menu); whether the choice persists across sessions and where; and how "system" is resolved for Bootstrap's colour modes and for `data-d12-theme`.

## Answer

Today the Theme select lives in a BoardEditor field, resets on every load, and sets `data-d12-theme` on `.board-editor-page` only. Bootstrap 5.3.8 needs a script to follow the OS, so CSS alone would put a light chrome around a dark canvas.

1. One switch. A single App theme drives both the chrome (`data-bs-theme`) and the canvas (`data-d12-theme`). A dark canvas in a light chrome reads as a bug. The library still lets a host split them (ADR 0012), so a separate canvas override can come later if anyone asks.
2. Where it lives. An icon dropdown in the shared nav bar with Light, Dark and System, the same on `/` and `/board/{id}`. The icon shows the current choice. The editor toolbar's Theme select is removed. Its exact position belongs to [Shared nav bar](07-shared-nav-bar.md). Rejected: a general settings menu (it would hold one item) and keeping it in the editor toolbar (the board list couldn't reach it).
3. Persistence. `localStorage` key `d12canvas.theme`, value `light`, `dark` or `system`. A missing or unknown value means `system`, so `system` is the default for a first visit. The choice is per browser, like the boards in IndexedDB. The App has no user accounts, so server storage isn't an option.
4. Resolving `system`. An inline script in `index.html`'s `<head>` runs before Blazor starts. It reads the key, resolves `system` with `matchMedia('(prefers-color-scheme: dark)')`, and sets both `data-bs-theme` and `data-d12-theme` on `<html>` to the resolved `light` or `dark`. A `change` listener on that media query re-applies while the choice is `system`. `.board-editor-page` no longer carries its own attribute. The canvas's `prefers-color-scheme` block never applies in the App, because the explicit attribute wins. Rejected: leaving `data-d12-theme` off for `system` and letting the canvas follow its media query, which puts two mechanisms on the same decision.
5. Blazor side. The nav bar dropdown calls a small App JS function through `IJSRuntime` that writes the key and sets both attributes. It reads the stored choice on first render to pick its icon. This is App JS, not Bootstrap JS, so [Bootstrap delivery and JS](02-bootstrap-delivery-and-js.md) still holds.
6. App colour tokens. Delete the `--app-*` tokens and their `:root`, `prefers-color-scheme` and `[data-d12-theme]` blocks in `app.css`. Rules use Bootstrap's variables instead (`--bs-body-bg`, `--bs-tertiary-bg`, `--bs-border-color`, `--bs-body-color`, `--bs-secondary-color`, `--bs-primary`, `--bs-danger`, `--bs-success`, `--bs-border-radius`, and `.shadow-sm` for the shadow). A colour with no Bootstrap equivalent gets one definition under `[data-bs-theme=dark]`. The old accent `#2f6fed` gives way to Bootstrap's `#0d6efd` unless [App name and logo](06-app-name-and-logo.md) sets a brand colour by overriding `--bs-primary`.
7. Other tabs. Not synced. Open tabs pick up a change on their next load.
