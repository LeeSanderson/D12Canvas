# App polish

Label: wayfinder:map

## Destination

A spec at `.scratch/app-polish/spec.md` plus implementation issues under `.scratch/app-polish/issues/`, ready for `/implement`. The spec covers: D12Canvas.App restyled with the latest Bootstrap and Bootstrap Icons, one nav bar shared by the board list (`/`) and the board editor (`/board/{id}`), App dark mode, narrow-screen layouts, branding, and both items from `.scratch/backlog/`.

## Notes

- Planning only. Tickets resolve decisions; no App or library code changes until the spec is handed to `/implement`.
- Bootstrap reaches the App chrome only. `lib/D12Canvas` takes no Bootstrap dependency and keeps its `--d12-*` tokens.
- Sticky-note alignment is a library change and is in scope.
- Skills: `/grilling` and `/domain-modeling` for grilling tickets, `/prototype` for prototype tickets (throwaway routes in the App are fine), `/research` for research tickets. Write the destination with `/to-spec` then `/to-tickets`.
- Apply `/unslop` to everything written here.
- App visual tests are out of scope, but a library change (sticky note) still needs the full visual suite run per `docs/agents/testing.md`.

## Decisions so far

- [Latest Bootstrap and Bootstrap Icons in a Blazor WebAssembly app](issues/01-latest-bootstrap-in-blazor-wasm.md): stable is Bootstrap 5.3.8 and Icons 1.13.2. 6.0 is alpha with large renames. 5.3's JS clashes with Blazor-owned DOM, and Reboot reaches into the canvas.
- [Bootstrap delivery and JS](issues/02-bootstrap-delivery-and-js.md): 5.3.8 CSS vendored in `wwwroot/lib`, imported into a cascade layer so library styles win. No Bootstrap JS: modals are native `<dialog>`, dropdowns and the offcanvas nav render from Blazor state. Icons are inline SVG via an `Icon` component. App rules that reuse Bootstrap class names are deleted. A library issue sets properties the library now leaves to browser defaults. This also settles modals and forms: both dialogs move to the `<dialog>` pattern with `.form-control` inputs.
- [Heading tab stop](issues/03-heading-tab-stop.md): keep `FocusOnNavigate` on `h1` and hide the ring with `h1:focus { outline: none; }`. The board name stays an `h1`, with a separate Rename button beside it. Enter and Escape return focus to that button, and the rename input gets a label.
- [Sticky note text alignment](issues/04-sticky-note-text-alignment.md): `TextAlign` (left/center/right, existing role) and `VerticalAlign` (top/middle/bottom, new role) as string dropdowns. New and old notes alike default to center/middle. Text doesn't move when inline editing starts, and overflow starts at the top. Sticky note only.
- [App and canvas theme](issues/05-app-and-canvas-theme.md): one switch for chrome and canvas, an icon dropdown (Light/Dark/System) in the nav bar, saved in `localStorage` as `d12canvas.theme` with `system` as the default. A `<head>` script writes the resolved mode to both `data-bs-theme` and `data-d12-theme` on `<html>`. The `--app-*` tokens give way to Bootstrap's variables.
- [App name and logo](issues/06-app-name-and-logo.md): D12Canvas everywhere, titles as `<page> · D12Canvas`. The mark is one hand-drawn d12 SVG with the "12" face forward. The favicon is an SVG with a dark-mode block plus a checked-in PNG fallback. No brand colour, so the accent stays Bootstrap blue.
- [Shared nav bar](issues/07-shared-nav-bar.md): two tiers. An app bar holds the brand (links to `/`), a Boards link, Suggest and the theme, and moves into an offcanvas below `md`. A page header holds the title and the page's controls, which pages fill through Blazor sections. Autosave is on by default and remembered in `localStorage`. While it is on, Save is hidden and a brief "Saving…"/"Saved" shows instead.

## Not yet specified

- App test coverage. No test project covers D12Canvas.App today. Decide whether the spec adds bUnit tests for the nav bar and pages, once the nav bar's shape is known.
- Shortcuts panel and status bits in the editor (selection count, zoom readout). Where they sit once the toolbar is redesigned. The saved-suggestion notice now sits in the nav bar.
- Loading and error UI in `index.html` (the spinner and `#blazor-error-ui`), restyled to match.
- Spec and ticket slicing. Order of implementation issues, for example Bootstrap setup first, then nav bar, then pages.

## Out of scope

- App visual tests. Playwright baselines for the App's pages stay out of this effort (user decision while charting).
- Bootstrap in the library. `lib/D12Canvas` chrome (property panel, palette, menus) keeps its own styling (user decision while charting).
- Vertical alignment for `Text`. The backlog item asks for sticky notes only, so `Text` keeps `TextAlign` alone (decided in [Sticky note text alignment](issues/04-sticky-note-text-alignment.md)).
