# App polish

Status: ready-for-agent

Map: [App polish](map.md)

## Problem Statement

D12Canvas.App still looks like the Blazor template it started from. The board list and the board editor each draw their own header, so moving between them feels like moving between two apps. The editor has one long row of text buttons, checkboxes and selects. Dark mode only reaches the canvas: the editor's Theme select resets on every load, and the board list has no theme at all. The page title says `D12Canvas.App`, the favicon is the template icon, and the loading spinner and error strip are template leftovers that ignore the theme. On a phone the header and toolbar overflow.

Two small bugs come from the backlog. After every navigation a focus ring shows around the page heading, which looks like a stray tab stop. And a new sticky note puts its text in the top-left corner, with no way to centre it or move it.

## Solution

The App gets a consistent look built on Bootstrap 5.3.8 and Bootstrap Icons 1.13.2, kept out of the canvas so the library renders as it does in any other host.

Every page shares one two-tier header. The app bar holds the D12Canvas brand, a Boards link, Suggest and a Light/Dark/System theme menu. Below it, a page header holds the page's title and its own controls. On narrow screens the app bar's items move into a side panel behind a menu button.

The board list becomes a grid of cards. Each card shows a small sketch of the board, its name and when it was last edited, plus a menu with Rename and Delete. The editor loses its toolbar row: Undo/Redo, a View menu and a Shortcuts toggle go in the page header, and a status bar under the canvas shows the selection count, Grid and Snapping toggles and the zoom control. Autosave is on by default and remembered.

One theme switch drives both the App chrome and the canvas, follows the OS when set to System, and is remembered. The loading screen shows the d12 mark and a progress bar, and the error bar matches the theme.

The heading keeps receiving focus on navigation for screen readers, but no ring shows. Sticky notes centre their text by default and gain horizontal and vertical alignment in the property bar.

## User Stories

### Shared header and navigation

1. As a board author, I want the same header on the board list and in the editor, so that the App feels like one product.
2. As a board author, I want the D12Canvas mark and name at the top left, so that I know which app I'm in.
3. As a board author, I want clicking the brand to take me to my boards, so that I can get home from anywhere.
4. As a board author, I want a Boards link in the app bar that shows as active on the board list, so that I can see where I am.
5. As a board author, I want each page's title and controls in a row under the app bar, so that page actions sit next to what they act on.
6. As a board author on a phone, I want the app bar's links and settings behind a menu button, so that the header fits on a narrow screen.
7. As a board author on a phone, I want the side panel to close when I pick Boards or Suggest, tap the backdrop or press its close button, so that I get back to the page quickly.
8. As a board author on a phone, I want the side panel to stay open after I pick a theme, so that I can see the result and try another.
9. As a board author on a phone, I want the page header row to stay visible and wrap, so that page controls are always reachable.
10. As a board author, I want to send a suggestion from any page, so that I don't have to open a board first.
11. As a board author, I want the "Saved <file>" notice after a suggestion to appear next to Suggest, so that I know it worked.
12. As a keyboard user, I want Escape to close any open menu and return focus to the button that opened it, so that I don't lose my place.
13. As a pointer user, I want a click outside an open menu to close it, so that menus don't stay open by accident.

### Branding

14. As a board author, I want browser tabs titled `<page> · D12Canvas`, so that I can tell my tabs apart when they are narrow.
15. As a board author, I want the board tab titled with the board's name, so that I can find the right board among many tabs.
16. As a board author, I want a d12 favicon that suits my OS light or dark setting, so that the tab is easy to spot.
17. As a board author using a browser without SVG favicons, I want a PNG fallback icon, so that the tab still has an icon.

### Theme

18. As a board author, I want one Light/Dark/System menu in the app bar, so that I set the theme once for the whole App.
19. As a board author, I want the chrome and the canvas to follow the same theme, so that I never get a dark canvas in a light frame.
20. As a board author, I want System to follow my OS setting and update when the OS changes, so that the App matches my other apps.
21. As a board author, I want my theme choice remembered in this browser, so that I don't pick it again on every visit.
22. As a first-time visitor, I want the theme to default to System, so that the App matches my OS from the start.
23. As a board author, I want the theme applied before the App finishes loading, so that the page doesn't flash the wrong colours.
24. As a board author, I want the theme menu's icon to show my current choice, so that I can see the setting without opening it.

### Board list

25. As a board author, I want my boards shown as cards with a sketch of their contents, so that I recognise a board at a glance.
26. As a board author, I want one card per row on a phone and up to four on a wide screen, so that the grid uses the space I have.
27. As a board author, I want to open a board by clicking anywhere on its card, so that I don't have to aim for the name.
28. As a board author, I want long board names truncated with the full name on hover, so that cards keep a tidy layout.
29. As a board author, I want each card to say when the board was last edited in relative terms, so that I can find recent work.
30. As a board author, I want the full edit date on hover, so that I can check the exact time when it matters.
31. As a board author, I want a New board tile at the start of the grid and a New board button in the page header, so that creating a board is always one click away.
32. As a board author, I want a ⋮ menu on each card with Rename and Delete, so that I can manage a board without opening it.
33. As a board author, I want Delete to ask me to confirm with the board's name, so that I don't delete the wrong board.
34. As a board author, I want Rename to open a dialog with the current name, so that I can fix a typo quickly.
35. As a new user, I want an empty state that says I have no boards yet and offers to create one, so that I know what to do first.
36. As a board author, I want placeholder cards while my boards load, so that the page doesn't jump when they arrive.
37. As a board author, I want each card's sketch to fill in as its board loads, so that I can use the list before every sketch is ready.
38. As a board author, I want an empty board's card to say "Empty board", so that a blank sketch doesn't look broken.
39. As a board author, I want the card sketches to follow the theme, so that they don't glare in dark mode.

### Editor header

40. As a board author, I want the board's name as the editor's heading with a Rename button beside it, so that I can rename by mouse or keyboard.
41. As a board author, I want clicking the board name to start a rename as well, so that the old shortcut still works.
42. As a keyboard user, I want Enter to commit a rename and Escape to cancel, and both to return focus to the Rename button, so that I can carry on from where I was.
43. As a screen reader user, I want the rename input labelled "Board name", so that I know what I'm editing.
44. As a board author, I want Undo and Redo buttons in the editor header, so that I can undo without remembering shortcuts.
45. As a board author, I want a View menu with Zoom to fit, Zoom to selection and Actual size, each showing its shortcut, so that I can learn the keys as I go.
46. As a board author, I want Snap to grid and Object snapping as switches in the View menu, so that I can change them on any screen size.
47. As a board author, I want to choose the mouse wheel profile (Auto, Mouse, Trackpad) from the View menu, with a check on the current one, so that scrolling suits my device.
48. As a board author, I want a Shortcuts button that shows and hides the shortcuts panel and looks pressed while it's open, so that I can see the panel's state.
49. As a board author, I want every icon button to show a tooltip with its name and shortcut, so that I know what it does before clicking.
50. As a board author on a narrow screen, I want the View button to shrink to its icon, so that the header still fits.

### Autosave

51. As a board author, I want autosave on by default, so that I don't lose work by forgetting to save.
52. As a board author, I want my autosave choice remembered for every board in this browser, so that I set it once.
53. As a board author with autosave on, I want a brief "Saving…" then "Saved" after each change, so that I trust my work is stored.
54. As a board author with autosave on, I want no Save button, so that the header shows only what I need.
55. As a board author with autosave off, I want "Unsaved changes" or "Saved" and a Save button that is disabled when nothing changed, so that I know when to save.
56. As a board author, I want turning autosave on with unsaved changes to save at once, so that nothing waits for my next edit.
57. As a screen reader user, I want save status changes announced, so that I know when saving finished.
58. As a board author on a phone, I want the save status shown as an icon only, so that it doesn't crowd the header.

### Status bar

59. As a board author, I want a status bar under the canvas that says how many items are selected, so that I can check my selection at a glance.
60. As a screen reader user, I want selection count changes announced, so that I know what I've selected.
61. As a board author, I want Grid and Snapping toggles in the status bar that show whether they're on, so that I can flip them without opening a menu.
62. As a board author, I want zoom out, the zoom percentage and zoom in in the status bar, so that zoom is always visible and one click away.
63. As a board author, I want the zoom percentage to open a menu with the three zoom commands, so that I can fit or reset from the status bar.
64. As a board author on a narrow screen, I want the status bar to keep the selection count and zoom and drop the Grid and Snapping toggles, so that it fits on one line.

### Dialogs

65. As a board author, I want Rename and Delete dialogs that dim the page and trap focus, so that I can't act on the page behind them by mistake.
66. As a keyboard user, I want Escape to close a dialog, so that I can back out without a mouse.
67. As a board author, I want dialogs and inputs to match the rest of the App in both themes, so that they don't look out of place.

### Heading focus

68. As a board author, I want no focus ring around the page heading after I navigate, so that the page doesn't look like it has a stray selection.
69. As a screen reader user, I want focus still to move to the page heading on navigation, so that the new page gets announced.

### Loading and errors

70. As a board author, I want the loading screen to show the D12Canvas mark and a progress bar, so that I know the App is starting and how far along it is.
71. As a board author, I want the loading screen in my chosen theme, so that a dark-mode load doesn't flash white.
72. As a board author, I want a clear red bar at the bottom of the window when the App breaks, saying unsaved changes may be lost, so that I know to stop and reload.
73. As a board author, I want Reload in the error bar to reload the page I'm on, so that a broken board comes back as the same board.
74. As a board author, I want to dismiss the error bar, so that I can look at the board before reloading.
75. As a screen reader user, I want the error bar announced when it appears, so that I know something went wrong.
76. As a developer who later adds an `ErrorBoundary`, I want its default message to match the error bar in both themes, so that I don't need to restyle it.

### Sticky notes

77. As a board author, I want a new sticky note's text centred both ways, so that notes look like sticky notes.
78. As a board author, I want to set a sticky note's text to left, centre or right, so that I can lay out lists and labels.
79. As a board author, I want to set a sticky note's text to top, middle or bottom, so that I can leave room for a drawing or a heading.
80. As a board author, I want notes on boards I saved before this change to show centred, so that old and new notes match.
81. As a board author, I want text to stay in the same place when I start and stop editing a note, so that the note doesn't jump under my cursor.
82. As a board author, I want text taller than the note to start at the top, so that I can always read the first line.
83. As a board author, I want selecting a text box and a sticky note together to show one shared Text align row, so that I can align both at once.
84. As a board author, I want Vertical align shown only when the selection is all sticky notes, so that I don't see a row that doesn't apply.

### Library hosts

85. As a library host developer, I want a board sketch component that draws any `Board` as a small scaled-to-fit picture, so that I can show board previews in my own app.
86. As a library host developer, I want the board sketch to use the canvas theme tokens, so that it follows the host's canvas theme.
87. As a library host developer, I want library components to set the styles they rely on, so that a host stylesheet such as Bootstrap's Reboot doesn't change how the canvas looks.

### Maintainers

88. As a maintainer, I want Bootstrap vendored into the repo at a pinned version, so that builds work offline, in CI and in the Docker image.
89. As a maintainer, I want App tests for autosave, theme, the nav bar, the status bar and View menu, and the board list, so that later changes don't silently break them.
90. As a maintainer, I want CI to run the App tests and the App API tests, so that failures show up on every push.

## Implementation Decisions

### Bootstrap delivery

- Bootstrap 5.3.8 and Bootstrap Icons 1.13.2. Bootstrap 6 is alpha and its class names may change before beta.
- The minified Bootstrap CSS and its source map are vendored into the App's static files and committed. The file header records the version. An upgrade is a manual copy and commit. No LibMan, no CDN.
- The App stylesheet imports Bootstrap into a cascade layer named `bootstrap`. Library `<style>` blocks are unlayered, so any property they set beats Bootstrap. App CSS is also unlayered.
- No Bootstrap JavaScript is shipped. Interactive pieces render their state from Blazor:
  - Modals are native `<dialog>` elements opened with `showModal()`, styled with `.modal-content`, `.modal-header`, `.modal-body` and `.modal-footer`. The browser provides the backdrop, focus trap, Escape and top layer. The existing `showModal()`/`close()` interop in the suggestion box becomes one shared JS module that every dialog uses. The rename dialog and the new delete confirmation use it, with `.form-control` inputs.
  - Dropdowns render `.dropdown-menu.show` from component state and carry `data-bs-popper="static"` so Bootstrap's CSS places them. The status bar's zoom menu adds `.dropup`.
  - One shared dropdown component serves the theme menu, the View menu, the zoom drop-up and each card's ⋮ menu. The prototypes settled its shape: a toggle-button fragment, plus a body `RenderFragment<Action>` that receives a close callback. It handles outside click, Escape (close and return focus to the toggle) and focus.
  - No tooltip component. Controls use native `title` attributes that include the shortcut, for example "Undo (Ctrl+Z)" and "Zoom in (PageUp)". Icon-only buttons also carry `aria-label`.
- An `Icon` App component renders inline `<svg>` at `1em` with `fill: currentColor` and `aria-hidden="true"`. It holds path data copied from Bootstrap Icons 1.13.2, only for icons the App uses, plus the d12 mark. No icon font or sprite.
- Every App CSS rule that reuses a Bootstrap class name is deleted (`.btn`, `.btn-sm`, `.btn-primary`, `.btn-secondary`, `.btn-danger`, `.card`, `.modal-backdrop`, `.modal-card`, `.modal-title`, `.modal-input`, `.modal-actions`) together with the bare `button` reset and the old `h1:focus` rule. Pages use Bootstrap components and utilities instead. Page `<style>` blocks keep only layout Bootstrap doesn't cover.

### Library hardening against host styles

- A library change audits every library `<style>` block for elements Bootstrap's Reboot styles (`p`, `button`, `label`, `textarea`, `h3`, `ul`, `input`, `select`). Wherever the library relies on a browser default, it sets the property itself. Canvas text (sticky note, text) keeps inheriting the host font, so the App sets the font through `--bs-body-font-family`.
- The Demo loads no Bootstrap, so visual baselines should not move for this change. If any baseline moves, investigate it before accepting the change.

### Theme

- One App theme drives both the chrome and the canvas. A host can still split them (ADR 0012); the App doesn't.
- The theme menu in the app bar offers Light, Dark and System. Its icon shows the current choice. The editor's Theme select is removed.
- The choice is stored in `localStorage` under `d12canvas.theme` as `light`, `dark` or `system`. A missing or unknown value means `system`.
- An inline script in `index.html`'s `<head>` runs before Blazor starts. It reads the key, resolves `system` with `matchMedia('(prefers-color-scheme: dark)')`, and sets `data-bs-theme` and `data-d12-theme` on `<html>` to the resolved `light` or `dark`. While the choice is `system`, a `change` listener on that media query re-applies it. The editor page no longer sets its own `data-d12-theme`.
- The theme menu reads and writes the key through `IPreferences` (below). Writing the `<html>` attributes is a call into a small App JS module.
- Other open tabs are not synced; they pick up the change on their next load.
- The `--app-*` colour tokens and their `:root`, `prefers-color-scheme` and `[data-d12-theme]` blocks are deleted. App rules use Bootstrap variables (`--bs-body-bg`, `--bs-tertiary-bg`, `--bs-border-color`, `--bs-body-color`, `--bs-secondary-color`, `--bs-primary`, `--bs-danger`, `--bs-success`, `--bs-border-radius`) and `.shadow-sm`. A colour with no Bootstrap equivalent gets one definition under `[data-bs-theme=dark]`.

### Preferences seam

- New App interface `IPreferences` with `GetAsync(key)` and `SetAsync(key, value)`, string values. One implementation over `localStorage`, registered in DI. The theme and autosave read and write through it. It is the only new seam in this effort.

### Branding

- The display name is D12Canvas everywhere the App names itself.
- Page titles are `<page> · D12Canvas`. The ` · D12Canvas` suffix comes from one place (a small title component or a constant), and each page passes only its own part: `Boards` on `/`, the board name on `/board/{id}` (`Board` while the name loads), `Not found` on the not-found route. The static `<title>` in `index.html` is `D12Canvas`.
- The mark is one hand-drawn SVG of a d12 seen face-on: a pentagon face, the edges of the faces around it, and a bold "12" drawn as filled paths, not `<text>`. One drawing for every size.
- In the app bar the mark renders through `Icon` with `currentColor`, `aria-hidden`, next to the "D12Canvas" wordmark, which carries the accessible name.
- The favicon is an SVG built from the same path data, filled `#0d6efd`, with an internal `prefers-color-scheme: dark` block switching to `#6ea8fe`. A 32px PNG rendered from it is committed as a fallback and replaces the template PNG. `index.html` links the SVG first. Whoever changes the drawing re-exports the PNG.
- No brand colour. `--bs-primary` keeps Bootstrap's defaults.

### Shared layout

- `MainLayout` renders two tiers on every route:
  - App bar: `.navbar.navbar-expand-md.bg-body-tertiary` with the brand (links to `/`), a Boards nav link (active with `aria-current="page"` on `/`), Suggest, and the theme menu with its label.
  - Page header: the page's title on the left, the page's controls on the right.
- Pages fill the page header with Blazor sections. The layout renders a `SectionOutlet` for each of three named sections: title, actions and more. Section names live in one static class.
- `SuggestionBox` moves from the editor into the layout. Its "Saved <file>" notice shows in the app bar next to Suggest.
- Below `md`, the app bar's items move into a responsive `.offcanvas-md.offcanvas-end`, opened by a menu button with the `list` icon. Blazor state toggles `.show`, and `@if` renders `.offcanvas-backdrop`. The close button, a backdrop click, Boards or Suggest closes it. Picking a theme leaves it open. The page header stays visible and wraps.
- The layout is a full-height flex column: the header, then `<main>` with `flex: 1; min-height: 0; overflow: auto`. The editor fills `<main>` at full height.

### Heading focus

- Keep `<FocusOnNavigate Selector="h1">`. The App stylesheet, outside the Bootstrap layer, sets `h1:focus { outline: none; }`. This closes backlog item 002.

### Board list page

- The page header holds `h1` "Boards" and a New board button (`btn-primary btn-sm`, `plus-lg`). The old subtitle is dropped.
- The body is a card grid: `.row.row-cols-1.row-cols-sm-2.row-cols-lg-3.row-cols-xl-4.g-3` inside `.container-xl.py-4`.
- When boards exist, the first cell is a dashed New board tile (`plus-lg`) that creates a board.
- Each card:
  - Top: a 16:9 board sketch on a dotted `bg-body-tertiary` background, blank until that board loads.
  - Body: the board name as a `stretched-link` to `/board/{id}`, truncated, with the full name in `title`. Below it, "Edited <relative time>" in small muted text, with the full date in `title`. The relative text steps through "just now", minutes, hours, "yesterday", days, then "On 12 Oct", adding the year outside the current year. The relative-time formatting is a pure function that takes "now" as input.
  - A ⋮ menu (`three-dots-vertical`, `btn-link`) layered above the stretched link, with Rename… (`pencil`) and Delete… (`trash`, `text-danger`). No Open item.
  - Hover raises the shadow and tints the border with `--bs-primary-border-subtle`.
- Empty state: one centred list-group card, at most 480px wide, with the `easel` icon, "No boards yet", "Create your first board to start diagramming." and a small New board button. No grid and no dashed tile.
- Loading: four `.placeholder-glow` cards, each a 16:9 block, a name line and a date line.
- After the list renders, the page loads each board with `IBoardStore.LoadAsync` and fills its card's sketch when it arrives.
- Rename opens the rename `<dialog>`. Delete opens a confirmation `<dialog>` reading `Delete "<name>"? This cannot be undone.`

### Board sketch (library)

- A new public library component that takes a `Board` and draws an `<svg>`:
  - One rectangle per component from its `Bounds`: sticky-note styling for sticky notes (found by component type, not by searching the type key), an outline for the rest.
  - One line per edge between the centres of the components it connects. A floating end uses its own point. Edge-label components are skipped.
  - The `viewBox` is the union of the component bounds, so any board scales to fit. Lines use `vector-effect="non-scaling-stroke"`.
  - A board with no components shows "Empty board".
  - Colours come from `--d12-*` tokens, so the sketch follows `data-d12-theme`.

### Board editor page

- The page header's title section holds the board name `h1` and a Rename button beside it (`pencil` icon, `aria-label="Rename board"`). Clicking the name also starts a rename. The rename input has `aria-label="Board name"`. Enter commits and Escape cancels; both return focus to the Rename button. Blur commits and leaves focus where the user moved it.
- The toolbar row is removed. The editor fills the header's more section, left to right:
  - Undo and Redo as an icon `btn-group btn-group-sm` (`arrow-counterclockwise`, `arrow-clockwise`, `btn-outline-secondary`).
  - A View dropdown (`sliders` icon; "View" label from `sm` up; menu aligned to the end) in three groups split by dividers:
    - Zoom: Zoom to fit, Zoom to selection and Actual size, each with its icon (`arrows-fullscreen`, `bounding-box-circles`, `aspect-ratio`) and its shortcut right-aligned in muted text.
    - Snapping: Snap to grid and Object snapping as `form-switch` toggles.
    - Mouse wheel: Auto, Mouse, Trackpad, with `check2` on the current one.
  - A Shortcuts toggle (`keyboard` icon; "Shortcuts" label from `lg` up). `btn-outline-secondary`, `btn-secondary` while open, with `aria-pressed`. It shows and hides the existing shortcuts panel, which stays an overlay on the canvas.
  - The Autosave switch and the save status/Save button.
- Status bar: a 30px `footer` under the canvas (`bg-body-tertiary`, `border-top`, small muted text).
  - Left: the selection count, "N selected" or "Nothing selected", in a `role="status"` element.
  - Right: Grid (`grid-3x3-gap`) and Snapping (`magnet`) text toggles with `aria-pressed`, emphasised when on and dimmed when off. Then zoom out (`dash-lg`), the zoom percentage as a drop-up holding the three zoom commands, and zoom in (`plus-lg`). Zoom in and out call the canvas's existing zoom commands.
  - Below `sm` the Grid and Snapping toggles are hidden. Snapping stays reachable through the View menu.
- Snap to grid and object snapping appear in both the View menu and the status bar on purpose.

### Autosave

- On by default. Stored through `IPreferences` as `d12canvas.autosave` (`"true"`/`"false"`), read when a board opens, applied to every board.
- On: Save and the save-state text are hidden. Each save shows "Saving…" (`cloud-arrow-up`), then "Saved" (`cloud-check`) for two seconds, then clears.
- Off: the header shows "Unsaved changes" or "Saved" and a Save button (`floppy`), disabled when nothing changed.
- Turning autosave on while the board has unsaved changes saves at once.
- The status text is one always-rendered `role="status"` element. Below `sm` it shows the icon only.

### Loading screen and error UI

- Loading screen: the d12 mark at about 64px in `--bs-primary`, centred, as static inline SVG in `index.html` (a second hand-kept copy of the path data, like the favicon). Under it a thin `.progress` bar whose width reads `var(--blazor-load-percentage)`. Below that, "Loading D12Canvas…" in muted text. The template ring and its CSS go.
- Error bar: a full-width `.alert.alert-danger` fixed to the bottom of the viewport, square corners, no margin, `z-index` above Bootstrap's fixed layer so it covers the status bar. It has `role="alert"`, an inline `exclamation-triangle-fill` SVG, and the text "Something went wrong. Unsaved changes may be lost." `lightyellow` and `color-scheme: light only` go.
- Reload is `<a href="." class="reload">` styled `.btn.btn-sm.btn-danger`, with an inline `onclick` that calls `location.reload()` and returns `false`, so it reloads the current route. (With `<base href="/">`, the `href` alone goes to `/`; it stays as the fallback.)
- Dismiss is `<button type="button" class="btn-close dismiss" aria-label="Dismiss">`. The `dismiss` class must stay because Blazor's script wires it.
- `.blazor-error-boundary` is restyled in CSS on that selector alone, since Blazor renders it as a bare `<div>`: `--bs-danger-bg-subtle` background, `--bs-danger-text-emphasis` text, `--bs-danger-border-subtle` border, `--bs-border-radius`. The warning icon is the `exclamation-triangle-fill` path as a `mask-image` painted with `currentColor`. The `::after` text is "Something went wrong."

### Sticky note text alignment (library)

- `StickyNoteProps` gains two string dropdown props, mirroring `TextProps`:
  - `TextAlign`: `left`, `center`, `right`, on the existing `PropertyRole.TextAlign`.
  - `VerticalAlign`: `top`, `middle`, `bottom`, on a new `PropertyRole.VerticalAlign` declaring `EditorKind.Dropdown`, CLR type `string` and merged-row label "Vertical align". The property bar needs a glyph for it.
- Strings, not enums, because `TextAlign` is already a string role and an enum would break the cross-type merge with `Text`.
- New notes default to `center` / `middle` through the registration's default props. The record's positional parameters default to the same values, so System.Text.Json fills them in for boards saved before this change. No value is ever null.
- A selection of text boxes and sticky notes merges on `TextAlign`. `VerticalAlign` shows only when sticky notes are the only type selected.
- Inline editing keeps the text where it renders. The editor sizes to its content and the note places it by `VerticalAlign`. The implementer picks the mechanism (growing the textarea to its `scrollHeight`, or `field-sizing: content`).
- Text taller than the note uses `safe` alignment, so it starts at the top.
- `CONTEXT.md`'s Property role entry lists `VerticalAlign` in its closed set. No new glossary term and no ADR.
- This closes backlog item 001.

## Testing Decisions

### What a good test is here

A test drives the UI the way a person does (click View, then Zoom to fit; flip the Autosave switch) and checks what the person would see, plus what reached the store, the preferences or JS. It does not reach into component fields or private methods, and it does not assert CSS class lists except where a class is the behaviour (`.show` on an open menu, `active` on a nav link, `aria-pressed`).

### App tests

- A new `D12Canvas.App.Tests` project, set up like `D12Canvas.Tests`: xUnit v3 on Microsoft.Testing.Platform, bUnit 2.8.4-preview with the AngleSharp 1.5.2 override. It references the App and joins the solution.
- One seam for every App test: a helper renders a routed page (`BoardList` or `BoardEditor`) as the body of `MainLayout`, so sections, the app bar, the theme menu, the offcanvas and Suggest are all present. It registers:
  - a hand-written `InMemoryBoardStore` implementing `IBoardStore`, with a dictionary of saved boards, a settable clock so `UpdatedAt` and "Edited …" are deterministic, and a record of each save so autosave tests can count them. No mocking library.
  - an `InMemoryPreferences` implementing `IPreferences`.
  - bUnit's fake `NavigationManager`.
  - strict `JSInterop`, with `SetupModule` for the theme module and the dialog module. A JS call a test didn't set up fails that test.
- Each implementation issue writes tests for the behaviour it builds. Nothing is back-filled.
  - Autosave: on by default; the switch writes `d12canvas.autosave`; a stored value wins on load; while on, Save is hidden and "Saving…"/"Saved" shows; an edit calls `SaveAsync`; turning it on with unsaved changes saves at once.
  - Theme: Light, Dark and System each write `d12canvas.theme` and call the JS module to set both attributes; System is the default; the menu icon reflects the stored choice.
  - Nav bar: the brand links to `/`; Boards is active on `/`; the editor's page header shows Undo/Redo, View and Shortcuts and the list's doesn't; the offcanvas opens and closes from Blazor state; shared dropdowns close on Escape and outside click.
  - Status bar and View menu: the selection count text, the Grid and Snapping toggles, and the zoom commands act on the canvas.
  - Board list: the empty state shows one card; cards show the name link and "Edited …"; New board calls `CreateAsync` and navigates; Rename and Delete in the ⋮ menu call the store.
  - The relative-time function gets its own table of cases, with "now" passed in.
- Not tested: focus return after renaming (bUnit can only see that `FocusAsync` was called), `IndexedDbBoardStore`, `SuggestionBox` beyond its move into the layout, the `index.html` `<head>` script, the loading and error markup, and anything pixel-level.
- CI's unit-tests job runs `D12Canvas.App.Tests` and also `D12Canvas.App.Api.Tests`, which CI doesn't run today.

### Library tests

- The existing bUnit seam in `D12Canvas.Tests`.
- Sticky-note alignment: new notes default to center/middle; a board JSON without the two props loads centred; the property bar shows Text align and Vertical align for sticky notes, merges Text align across text and sticky notes, and hides Vertical align for a mixed selection. Prior art: the existing `TextProps`, property role and Mixed tests.
- Board sketch: given a `Board`, it draws one rectangle per component and one line per edge, skips edge labels, handles a floating edge end, and shows "Empty board" for an empty board. Prior art: the existing component rendering tests.
- Both library changes, and the Reboot hardening, touch rendered markup or shared `<style>` blocks, so each runs the full Playwright visual suite in the pinned Docker image with `-parallel none` per `docs/agents/testing.md` before committing. The sticky-note baselines will change because notes now centre by default. The hardening change should move no baselines.

## Out of Scope

- App visual tests. No Playwright baselines for the App's pages.
- Bootstrap in the library. Library chrome (property panel, palette, menus) keeps its own styling and `--d12-*` tokens.
- Loading board sketches only as cards scroll into view. The list loads every sketch for now; revisit only if it gets slow.
- Vertical alignment for `Text`. Only sticky notes get it.
- A separate canvas theme override in the App.
- Syncing the theme or autosave choice across open tabs.
- A brand colour.
- Adding `<ErrorBoundary>` to pages. Only its default style is updated.
- A tooltip component.

## Further Notes

- The decisions behind this spec live in the [App polish](map.md) map's tickets. The prototypes are on the `prototype/shared-nav-bar`, `prototype/editor-toolbar` and `prototype/board-list` branches, each built on the one before. Don't copy their shortcuts: a CDN Bootstrap, the mark's "12" as SVG `<text>`, `--app-*` tokens mapped onto `--bs-*`, icon paths pasted into a prototype dictionary, sticky notes detected by searching the type key, the sketch written as App code with `--bs-*` colours, and the `?variant=` and `?state=` switchers.
- The App is the project's showcase, so features land in its pages, not in a demo tour page.
- Suggested order for slicing: Bootstrap delivery and the library hardening first, then the test project and `IPreferences`, then theme and branding, then the shared layout and dropdown, then the editor and the board list in parallel. Sticky-note alignment, heading focus and the loading and error UI can go at any point.
