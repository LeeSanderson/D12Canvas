# Loading screen and error UI

Status: resolved
Blocked by: 17, 18

Spec: [App polish](../spec.md), "Loading screen and error UI". Decision record: [Loading and error UI](11-loading-and-error-ui.md).

## What to build

The App's start-up screen and its unhandled-error bar match the theme and the brand. Both live in `index.html` and render outside Blazor.

- Loading screen: the d12 mark at about 64px in `--bs-primary`, centred, as static inline SVG (a second copy of the mark's path data). Under it a thin `.progress` bar whose width reads `var(--blazor-load-percentage)`. Below that, "Loading D12Canvas…" in muted text. The template ring and its CSS go.
- Error bar: a full-width `.alert.alert-danger` fixed to the bottom of the viewport, square corners, no margin, `z-index` above Bootstrap's fixed layer so it covers the status bar. `role="alert"`, an inline `exclamation-triangle-fill` SVG, and "Something went wrong. Unsaved changes may be lost." `lightyellow` and `color-scheme: light only` go.
- Reload: `<a href="." class="reload">` styled `.btn.btn-sm.btn-danger`, with an inline `onclick` that calls `location.reload()` and returns `false`. With `<base href="/">` the `href` alone would go to `/`, so it is only the fallback.
- Dismiss: `<button type="button" class="btn-close dismiss" aria-label="Dismiss">`. The `dismiss` class stays, because Blazor's script wires it.
- `.blazor-error-boundary` restyled on that selector alone: `--bs-danger-bg-subtle` background, `--bs-danger-text-emphasis` text, `--bs-danger-border-subtle` border, `--bs-border-radius`, the `exclamation-triangle-fill` path as a `mask-image` painted with `currentColor`, and `::after` text "Something went wrong." No `<ErrorBoundary>` is added.

No automated tests: this markup runs outside Blazor and App visual tests are out of scope. Check it by hand.

## Acceptance criteria

- [ ] A cold load shows the mark, a progress bar that fills as Blazor loads, and "Loading D12Canvas…", in the stored theme (check light and dark).
- [ ] Throwing an unhandled exception from a temporary test button shows the error bar at the bottom, over the status bar, in both themes. Remove the button afterwards.
- [ ] Reload from the error bar on `/board/{id}` reloads that board, not the list.
- [ ] Dismiss hides the bar.
- [ ] A temporary `<ErrorBoundary>` around a throwing component shows the restyled message in both themes. Remove it afterwards.
- [ ] No template loading or error CSS remains.

## Comments

Done. The loading screen shows the d12 mark in `--bs-primary`, a 4px `.progress` bar that reads `--blazor-load-percentage`, and "Loading D12Canvas…". `IconTests.TheLoadingScreenDrawsTheSameMarkAsTheD12Icon` fails if the inline paths drift from `IconPaths`, like the favicon test. The ticket asked for no tests, but this one checks only the copied path data.

The error bar is `.alert.alert-danger` at `z-index: 1035`, above Bootstrap's fixed layer (1030) and below offcanvas and modals. Blazor shows the bar with an inline `display: block`, so the flex row sits on an inner div. `.blazor-error-boundary` uses the danger subtle tokens and a `mask-image` triangle.

Checked by hand in Chromium, light and dark, with temporary throw and `<ErrorBoundary>` buttons that are now gone. The bar fills from 0 to 192px as the percentage goes 0 to 100%. The error bar covers the status bar. Reload on `/board/{id}` comes back to the same board. Dismiss hides the bar. The Demo keeps its template loading and error CSS; this ticket only covers the App.
