# Shared nav bar

Type: prototype
Status: resolved
Blocked by: 02, 05

## Question

What does the nav bar shared by `/` and `/board/{id}` hold on each route, and how does it collapse on narrow screens?

Today each page draws its own header: BoardList has a title and "New board"; BoardEditor has "← Boards", a click-to-rename `h1`, an Autosave checkbox and Save. Prototype the shared bar in MainLayout with Bootstrap, then react to it. Candidates for the bar: brand, link to Boards, the current board's name and rename, save state and Save, the theme switch, Suggest. Decide how a page puts its own items into the bar (sections, cascading values, a nav service).

The prototype loads no Bootstrap JS (see [Bootstrap delivery and JS](02-bootstrap-delivery-and-js.md)). The narrow-screen collapse and any dropdown render `.show` from Blazor state, and icons come from inline SVG.

The rename pattern is settled (see [Heading tab stop](03-heading-tab-stop.md)): the board name is a plain `h1` with a separate Rename button beside it. This ticket only places them.

The theme switch is settled too (see [App and canvas theme](05-app-and-canvas-theme.md)): an icon dropdown with Light, Dark and System, in the bar on both routes. This ticket only places it.

The brand is settled (see [App name and logo](06-app-name-and-logo.md)): the d12 mark through the `Icon` component plus the wordmark "D12Canvas". This ticket places them and decides whether the brand links to `/`.

## Answer

Decided with the user on 2026-11-11 after trying three variants in a prototype. The prototype is on the `prototype/shared-nav-bar` branch: variants A (one row with a breadcrumb), B (two tiers) and C (compact with an overflow menu). B won.

1. **Two tiers.** MainLayout renders both tiers on every route.
   - The app bar (`.navbar.navbar-expand-md.bg-body-tertiary`) holds the brand, a Boards nav link, Suggest and the theme dropdown with its label. The brand is the d12 mark plus the "D12Canvas" wordmark and links to `/`. The Boards link also goes to `/` and is `active` with `aria-current="page"` there. Both stay.
   - The page header row below holds the page's title on the left and the page's own controls on the right.
2. **What each route puts in the page header.**
   - `/`: the `h1` "Boards" and a New board button (`btn-primary btn-sm`, `plus-lg` icon).
   - `/board/{id}`: the board name `h1` with the pencil Rename button beside it (the pattern from [Heading tab stop](03-heading-tab-stop.md)), and an Autosave switch (`.form-check.form-switch`).
3. **Autosave.**
   - On by default. The choice is stored in `localStorage` as `d12canvas.autosave` (`"true"`/`"false"`) and read when a board opens. It applies to every board and survives a reload.
   - When autosave is on, the Save button and the save-state text are hidden. Each autosave shows "Saving…" (`cloud-arrow-up`) and then "Saved" (`cloud-check`) for two seconds, after which the indicator clears.
   - When autosave is off, the header shows "Unsaved changes" or "Saved" and a Save button with the `floppy` icon, disabled when nothing has changed.
   - Turning autosave on while the board has unsaved changes saves straight away.
   - The status text is one `role="status"` element that is always rendered, so screen readers announce each change. Below `sm` it shows the icon only.
4. **Suggest moves into the layout.** `SuggestionBox` moves from BoardEditor to MainLayout, so Suggest is on both routes. The "Saved <file>" notice after a suggestion shows in the app bar next to Suggest.
5. **How pages fill the bar.** Blazor sections (`Microsoft.AspNetCore.Components.Sections`). The layout renders a `SectionOutlet` for each of three named sections: title, actions and more. Pages fill them with `SectionContent`. The section names live in one static class. The user accepted this for now. A nav service was not explored.
6. **Narrow screens.** Below `md` the app bar's items (Boards, Suggest, theme) move into a responsive `.offcanvas-md.offcanvas-end` opened by a menu button (`list` icon). Blazor state toggles `.show`, and `@if` renders the `.offcanvas-backdrop`. The header's close button, a backdrop click, Boards or Suggest closes it. Picking a theme leaves it open. The page header row stays visible and wraps.
7. **Layout.** `.app-layout` is a full-height flex column: header, then `<main>` with `flex: 1; min-height: 0; overflow: auto`. The editor fills `<main>` at `height: 100%`.
8. **Dropdowns.** The theme dropdown renders `.dropdown-menu.show` from Blazor state with `data-bs-popper="static"`. In the prototype a transparent fixed layer catches outside clicks. The spec still needs Escape and focus handling (see [Bootstrap delivery and JS](02-bootstrap-delivery-and-js.md)).

Prototype shortcuts the spec should not copy: Bootstrap loads from a CDN, the "12" on the mark is SVG `<text>`, and `app.css` maps the old `--app-*` tokens onto `--bs-*` instead of removing them.

