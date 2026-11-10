# Bootstrap delivery and JS

Type: grilling
Status: resolved
Blocked by: 01

## Question

How does Bootstrap reach D12Canvas.App, and who drives its interactive components?

Read [Latest Bootstrap and Bootstrap Icons in a Blazor WebAssembly app](01-latest-bootstrap-in-blazor-wasm.md) first.

- Version: stable 5.3.8, or 6.0.0-alpha.1. "Latest" could mean either. 6.0 is alpha, with renamed components and classes and changes still expected before beta.
- Keeping Bootstrap out of the canvas: Reboot changes the font and plain-element styles the library's `StickyNote`, `Text` and panels inherit, and z-indexes collide with `ContextMenu`. Decide how to fence the canvas off.

- Delivery: vendored in `wwwroot`, CDN, LibMan or npm, and how the version is pinned and upgraded.
- Interactivity: load `bootstrap.bundle.js` and let it run dropdowns, modals and collapse, or render their open/closed state from Blazor with Bootstrap's classes only.
- Icons: font, sprite or inline SVG.
- What happens to the hand-written `.btn`, `.card` and similar rules in `wwwroot/css/app.css`.

## Answer

Decided with the user in a grilling session on 2026-11-10.

1. **Version.** Bootstrap 5.3.8 and Bootstrap Icons 1.13.2. v6 is alpha and its class names may change before beta.
2. **Fencing the canvas.** `app.css` opens with `@import url(lib/bootstrap/css/bootstrap.min.css) layer(bootstrap);`. Any property a library `<style>` block sets then beats Bootstrap. Canvas text (`StickyNote`, `Text`) keeps inheriting the host font, and the App picks that font through `--bs-body-font-family`.
   - The spec includes a library issue. It audits the library's `<style>` blocks for elements Reboot touches (`p`, `button`, `label`, `textarea`, `h3`, `ul`, `input`, `select`). Where the library relies on a browser default, it sets the property itself. This change needs the full visual suite. Baselines should not move, because the Demo loads no Bootstrap.
   - Rejected: a `revert-layer` reset on the canvas root (the App would depend on library class names), and dropping Reboot (needs a Sass build).
3. **Delivery.** Vendor `dist/css/bootstrap.min.css` and `bootstrap.min.css.map` from 5.3.8 into `wwwroot/lib/bootstrap/css/` and commit them. No Bootstrap JS is vendored. The file header records the version. An upgrade is a manual copy and commit. Rejected: LibMan (build needs the network, including CI and the Docker image) and CDN (no offline, not cached by a future service worker).
4. **Interactivity.** No Bootstrap JS.
   - Modals are native `<dialog>` elements opened with `showModal()` and styled with Bootstrap's `.modal-content`, `.modal-header`, `.modal-body` and `.modal-footer`. The browser handles backdrop, focus trap, Escape and top layer. `RenameBoardModal` moves to this pattern. The `showModal()`/`close()` interop in `SuggestionBox.razor.js` becomes one shared piece both dialogs use.
   - Dropdowns and the narrow-screen offcanvas nav render their state from Blazor. A bool toggles `.show`, `@if` renders the backdrop, and dropdowns carry `data-bs-popper="static"` so Bootstrap's CSS places them below the toggle. The App writes Escape, outside-click and focus handling for these.
   - No Bootstrap tooltips. Controls use `title` or `aria-label`.
5. **Icons.** An `Icon` component (`<Icon Name="trash" />`) renders inline `<svg>` at `1em` with `fill: currentColor` and `aria-hidden="true"`. It holds path data copied from the 1.13.2 `icons/` folder, only for icons the App uses. Icon-only buttons carry their own `aria-label`. Rejected: the icon font (about 221 KB, blank until loaded) and the sprite (1.1 MB).
6. **App CSS.** App CSS is unlayered, so an App rule with a Bootstrap class name silently beats Bootstrap. Bootstrap's `!important` utilities still win. Delete every App rule that reuses a Bootstrap name: `.btn`, `.btn-sm`, `.btn-primary`, `.btn-secondary`, `.btn-danger`, `.card`, `.modal-backdrop`, `.modal-card`, `.modal-title`, `.modal-input`, `.modal-actions`, and the bare `button` and `h1:focus` resets. Use Bootstrap components and utilities instead, for example `.table` for `.board-table`. Page `<style>` blocks keep only layout Bootstrap doesn't cover, such as the editor overlays around the canvas.
   - Not decided here: the colour tokens in `app.css` (`:root`, the `prefers-color-scheme` block, the `data-d12-theme` blocks) belong to [App and canvas theme](05-app-and-canvas-theme.md). `#blazor-error-ui` and the loading rules wait for the loading and error UI fog item.
