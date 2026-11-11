# Shared two-tier header and branding

Status: ready-for-agent
Blocked by: 15, 16

Spec: [App polish](../spec.md), "Shared layout", "Branding" and the editor's rename in "Board editor page". Decision records: [Shared nav bar](07-shared-nav-bar.md), [App name and logo](06-app-name-and-logo.md), [Heading tab stop](03-heading-tab-stop.md).

## What to build

Both pages share one header, and the App is called D12Canvas everywhere.

- `MainLayout` renders an app bar (`.navbar.navbar-expand-md.bg-body-tertiary`) with the brand (d12 mark through `Icon` plus the "D12Canvas" wordmark, linking to `/`), a Boards nav link (active with `aria-current="page"` on `/`), and Suggest. `SuggestionBox` moves out of the editor into the layout, and its "Saved <file>" notice shows next to Suggest.
- Below it, a page header row with three named sections (title, actions, more), filled by pages through Blazor sections. Section names live in one static class.
- The board list puts the `h1` "Boards" and a New board button (`btn-primary btn-sm`, `plus-lg`) in its sections. The editor puts its board name `h1` and a Rename button (`pencil`, `aria-label="Rename board"`) in the title section. Clicking the name also starts a rename. Enter commits and Escape cancels; both return focus to the Rename button. Blur commits. The rename input has `aria-label="Board name"`. The editor's other controls stay in their toolbar row for now.
- Below `md`, the app bar's items move into `.offcanvas-md.offcanvas-end`, opened by a `list` icon button. Blazor state toggles `.show` and renders `.offcanvas-backdrop`. The close button, the backdrop, Boards or Suggest closes it. The page header stays visible and wraps.
- The layout is a full-height flex column: header, then `<main>` with `flex: 1; min-height: 0; overflow: auto`. The editor fills `<main>`.
- The d12 mark: one hand-drawn SVG, a pentagon face with the surrounding face edges and a bold "12" as filled paths (not `<text>`).
- `favicon.svg` from the same path data, `#0d6efd` with an internal dark-mode block switching to `#6ea8fe`. A 32px PNG rendered from it replaces the template PNG. `index.html` links the SVG first.
- Page titles are `<page> · D12Canvas` from one place: `Boards`, the board name (`Board` while loading), `Not found`. The static `<title>` is `D12Canvas`.

## Acceptance criteria

- [ ] Both routes show the app bar and the page header.
- [ ] The brand links to `/`; the Boards link is active only on `/`.
- [ ] Suggest works from both routes.
- [ ] The editor's Rename button and a click on the name both start a rename; Enter and Escape return focus to the Rename button.
- [ ] Below `md`, the menu button opens the offcanvas, and the close button, the backdrop, Boards and Suggest close it.
- [ ] Tab titles follow `<page> · D12Canvas` on every route.
- [ ] The favicon shows the d12 mark in light and dark OS modes, and the PNG fallback is checked in.
- [ ] App tests through the page harness: the brand links to `/`, Boards is active on `/` and not on the editor, the list's header shows New board, the editor's header shows the board name and Rename button, and the offcanvas opens and closes from its controls.
