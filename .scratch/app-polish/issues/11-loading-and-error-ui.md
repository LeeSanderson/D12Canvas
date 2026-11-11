# Loading and error UI

Type: grilling
Status: open
Blocked by:

## Question

What do the App's start-up loading screen and `#blazor-error-ui` look like with Bootstrap?

Both live in `wwwroot/index.html` and render before or outside Blazor, so they can't use the `Icon` component or Blazor state. The theme `<head>` script already sets `data-bs-theme` before they show ([App and canvas theme](05-app-and-canvas-theme.md)), and the mark is settled ([App name and logo](06-app-name-and-logo.md)). Decide what the loading screen shows (spinner, mark, progress text), how the error bar looks and where it sits relative to the nav bar and status bar, and what its Reload and dismiss controls are.
