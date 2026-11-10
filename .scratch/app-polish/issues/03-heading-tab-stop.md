# Heading tab stop

Type: grilling
Status: open
Blocked by:

## Question

How should the App stop the page `h1` from acting like a tab stop, while keeping a sensible focus target after navigation?

Backlog source: `.scratch/backlog/002-no-tabstop-of-heading-page.md` (screenshot alongside). The cause is `<FocusOnNavigate RouteData="@routeData" Selector="h1" />` in `app/D12Canvas.App/App.razor`, which focuses the `h1` on every navigation and shows a focus ring on it.

Options to weigh: remove `FocusOnNavigate`; keep it and hide the focus ring on programmatically focused headings; point it at a different element, such as the main landmark or the canvas. Also settle the editor's click-to-rename `h1`, which takes a click but has no keyboard route.
