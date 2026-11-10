# App name and logo

Type: grilling
Status: resolved
Blocked by:

## Question

What is the App called in its nav bar, page titles and favicon, and what is its logo mark?

Today the page title is `D12Canvas.App` and `wwwroot/favicon.png` is the template icon. Settle the display name, the `<PageTitle>` pattern for each route (for example "Boards · Name" and "<board name> · Name"), and where the logo comes from: a Bootstrap Icon, a small hand-made SVG, or an existing asset.

Also settle whether the App has a brand colour. Without one, the accent becomes Bootstrap's `#0d6efd` (see [App and canvas theme](05-app-and-canvas-theme.md)). A brand colour overrides `--bs-primary` and needs a light and a dark value.

## Answer

Today `index.html` and the board list both say `D12Canvas.App`, the editor title is the board name alone, and `favicon.png` is the Blazor template icon. The repo has no logo asset.

1. Display name. D12Canvas, written the same in the nav bar, page titles and anywhere else the App names itself. It matches the library, the package and the repo. Rejected: "D12 Canvas" with a space, and a separate product name.
2. Page titles. `<page> · D12Canvas`, most specific part first so it survives a narrow tab. The ` · D12Canvas` suffix comes from one place (a small title component or a constant; the spec picks), and each page passes only its own part.
   - `/` is `Boards · D12Canvas`.
   - `/board/{id}` is `<board name> · D12Canvas`, and `Board · D12Canvas` while the name is still loading.
   - The not-found route is `Not found · D12Canvas`.
   - The static `<title>` in `index.html`, shown before Blazor starts, is `D12Canvas`.
3. Logo mark. One hand-drawn SVG of a twelve-sided die seen face-on, with the "12" face toward the viewer: a pentagon face, the edges of the faces around it, and a bold "12" drawn as filled paths (not `<text>`) that fills most of the face. One drawing for every size. At 16px the digits blur, and the d12 outline still carries it. In the nav bar it renders through the `Icon` component with `currentColor`, marked `aria-hidden`, and the "D12Canvas" wordmark beside it carries the accessible name. Rejected: a Bootstrap Icon (it has `dice-1` to `dice-6` and no d12), no mark at all, and a second simplified drawing for small sizes.
4. Favicon. `wwwroot/favicon.svg` built from the same path data, filled with `#0d6efd`, with a `@media (prefers-color-scheme: dark)` block inside that switches to `#6ea8fe` (Bootstrap's dark-mode primary text colour). It follows the OS, not the in-app switch, which is right for a browser tab. A 32px `favicon.png` rendered from the SVG is checked in as a fallback for browsers that ignore SVG icons, and the template PNG goes. `index.html` links both, SVG first. Whoever changes the drawing re-exports the PNG.
5. Brand colour. None. `--bs-primary` keeps Bootstrap's light and dark defaults, so [App and canvas theme](05-app-and-canvas-theme.md) stands as written: the old `#2f6fed` gives way to `#0d6efd`. Rejected: a custom hue, because Bootstrap 5.3 derives none of the primary variants from CSS alone, so every variant and its contrast would need hand tuning in both modes.

Placement of the mark and wordmark in the bar, and whether the brand links to `/`, belong to [Shared nav bar](07-shared-nav-bar.md).
