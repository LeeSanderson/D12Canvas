# Loading and error UI

Type: grilling
Status: resolved
Blocked by:

## Question

What do the App's start-up loading screen and `#blazor-error-ui` look like with Bootstrap?

Both live in `wwwroot/index.html` and render before or outside Blazor, so they can't use the `Icon` component or Blazor state. The theme `<head>` script already sets `data-bs-theme` before they show ([App and canvas theme](05-app-and-canvas-theme.md)), and the mark is settled ([App name and logo](06-app-name-and-logo.md)). Decide what the loading screen shows (spinner, mark, progress text), how the error bar looks and where it sits relative to the nav bar and status bar, and what its Reload and dismiss controls are.

## Answer

Today `index.html` has the Blazor template's ring, which fills to `--blazor-load-percentage` with "NN%" text, and the template's pale yellow error strip with a "Reload" link and a 🗙 glyph. Their CSS hard-codes `#e0e0e0`, `#1b6ec2`, `lightyellow` and `color-scheme: light only`, so neither follows the theme. Both render outside Blazor, so any icon is static inline SVG, not the `Icon` component. `data-bs-theme` is already set by the `<head>` script, so Bootstrap classes and variables theme them correctly.

1. Loading screen. The d12 mark at about 64px in `--bs-primary`, centred, inlined as static SVG. This is a second copy of the path data, kept in step by hand like the favicon. Under it, a thin `.progress` bar whose width reads `var(--blazor-load-percentage)`, so it still shows real progress. Below that, "Loading D12Canvas…" in muted text. The ring and its CSS go. Rejected: restyling the ring (template look, no brand), a `.spinner-border` (can't show progress), and an app-shell skeleton (a static copy of the nav bar that drifts).
2. Error bar. A full-width `.alert.alert-danger` fixed to the bottom of the viewport, square corners, no margin, with a `z-index` above Bootstrap's `$zindex-fixed` so it covers the editor's status bar. The status bar's controls are useless once the app has failed. It leads with an inline `exclamation-triangle-fill` SVG, then reads "Something went wrong. Unsaved changes may be lost." It has `role="alert"`. `lightyellow` and `color-scheme: light only` go. Rejected: pinned under the app bar (pushes over the page header, and its offset changes below `md`) and a corner toast (easy to miss when the app is broken).
3. Controls. `blazor.webassembly.js` hides the bar when `.dismiss` is clicked, and Reload is a plain link, so both class names stay. Reload is `<a href="." class="reload">` styled `.btn.btn-sm.btn-danger`. With `<base href="/">`, `href="."` alone goes to `/`, so the link also gets an inline `onclick` that calls `location.reload()` and returns `false`. A broken editor comes back to the same board, and the `href` stays as the fallback. Rejected: plain `href="."`, which always lands on the board list. Dismiss is `<button type="button" class="btn-close dismiss" aria-label="Dismiss">`, which replaces the 🗙 glyph and adapts to dark mode. Dismiss stays so people can look at the board before reloading. Rejected: no dismiss.
4. Error boundary. Nothing in `app/` uses `<ErrorBoundary>`, so `.blazor-error-boundary` never applies today. It stays (user decision), restyled to match. Blazor renders it as a bare `<div>`, so the restyle is CSS on that selector: `--bs-danger-bg-subtle` background, `--bs-danger-text-emphasis` text, a `--bs-danger-border-subtle` border and `--bs-border-radius`. The base64 icon gives way to the `exclamation-triangle-fill` path as a `mask-image` painted with `currentColor`. The `::after` text becomes "Something went wrong.". Adding `<ErrorBoundary>` to pages is not part of this effort.
