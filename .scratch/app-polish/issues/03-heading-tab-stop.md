# Heading tab stop

Type: grilling
Status: resolved
Blocked by:

## Question

How should the App stop the page `h1` from acting like a tab stop, while keeping a sensible focus target after navigation?

Backlog source: `.scratch/backlog/002-no-tabstop-of-heading-page.md` (screenshot alongside). The cause is `<FocusOnNavigate RouteData="@routeData" Selector="h1" />` in `app/D12Canvas.App/App.razor`, which focuses the `h1` on every navigation and shows a focus ring on it.

Options to weigh: remove `FocusOnNavigate`; keep it and hide the focus ring on programmatically focused headings; point it at a different element, such as the main landmark or the canvas. Also settle the editor's click-to-rename `h1`, which takes a click but has no keyboard route.

## Answer

The heading was never in the Tab order. `FocusOnNavigate` gives the `h1` `tabindex="-1"` and focuses it from script, and Chromium draws a `:focus-visible` ring on that focus after a page load. The ring is what the backlog item saw.

1. Focus on navigation. Keep `<FocusOnNavigate RouteData="@routeData" Selector="h1" />`. Add `h1:focus { outline: none; }` to the App stylesheet, outside the Bootstrap cascade layer. Focus still moves to the heading on each navigation, so screen readers announce the new page, and no ring shows. This closes `.scratch/backlog/002-no-tabstop-of-heading-page.md`.
2. Rename trigger. The board name stays a plain `h1`. A separate Rename button sits next to it, with a pencil icon from the `Icon` component and `aria-label="Rename board"`. Clicking the name still starts the rename as a mouse shortcut. Rejected: a `<button>` inside the `h1` (its accessible name is only the board name, and `FocusOnNavigate` lands on the heading, not the button), and `tabindex="0"` plus `role="button"` on the `h1` (a fake button that puts the heading in the Tab order and stops screen readers announcing it as a heading). Where the name and button sit is for [Shared nav bar](07-shared-nav-bar.md).
3. Ending a rename. Enter commits and Escape cancels, as today. Both return focus to the Rename button. Blur commits and leaves focus where the user moved it. The rename input gets `aria-label="Board name"`.
4. Not found view. No change. Its `<p role="alert">` already gets announced, and `FocusOnNavigate` finding no `h1` there is harmless.
