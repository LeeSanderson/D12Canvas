# Shared nav bar

Type: prototype
Status: open
Blocked by: 02, 05

## Question

What does the nav bar shared by `/` and `/board/{id}` hold on each route, and how does it collapse on narrow screens?

Today each page draws its own header: BoardList has a title and "New board"; BoardEditor has "← Boards", a click-to-rename `h1`, an Autosave checkbox and Save. Prototype the shared bar in MainLayout with Bootstrap, then react to it. Candidates for the bar: brand, link to Boards, the current board's name and rename, save state and Save, the theme switch, Suggest. Decide how a page puts its own items into the bar (sections, cascading values, a nav service).

The prototype loads no Bootstrap JS (see [Bootstrap delivery and JS](02-bootstrap-delivery-and-js.md)). The narrow-screen collapse and any dropdown render `.show` from Blazor state, and icons come from inline SVG.

The rename pattern is settled (see [Heading tab stop](03-heading-tab-stop.md)): the board name is a plain `h1` with a separate Rename button beside it. This ticket only places them.

The theme switch is settled too (see [App and canvas theme](05-app-and-canvas-theme.md)): an icon dropdown with Light, Dark and System, in the bar on both routes. This ticket only places it.
