# Bootstrap in the App

Status: ready-for-agent
Blocked by: 12

Spec: [App polish](../spec.md), "Bootstrap delivery" and "Heading focus". Decision records: [Bootstrap delivery and JS](02-bootstrap-delivery-and-js.md), [Heading tab stop](03-heading-tab-stop.md). Closes `.scratch/backlog/002-no-tabstop-of-heading-page.md`.

## What to build

The App's existing pages are restyled with Bootstrap, keeping today's layout. The canvas looks exactly as before.

- Vendor Bootstrap 5.3.8's minified CSS and source map into the App's static files, with the version in the file header. The App stylesheet imports it into a cascade layer named `bootstrap`. No Bootstrap JS.
- An `Icon` component rendering inline `<svg>` at `1em`, `fill: currentColor`, `aria-hidden="true"`, holding path data copied from Bootstrap Icons 1.13.2 for the icons this ticket uses. Later tickets add icons as they need them.
- Delete every App CSS rule that reuses a Bootstrap class name (`.btn`, `.btn-sm`, `.btn-primary`, `.btn-secondary`, `.btn-danger`, `.card`, `.modal-backdrop`, `.modal-card`, `.modal-title`, `.modal-input`, `.modal-actions`) and the bare `button` reset. Existing markup uses Bootstrap's buttons, `.table`, `.form-control` and `.form-check` instead.
- One shared JS module for `showModal()`/`close()`. `SuggestionBox` and the rename dialog both use it. The rename dialog becomes a native `<dialog>` with `.modal-content`, `.modal-header`, `.modal-body`, `.modal-footer` and a `.form-control` input.
- Heading focus: keep `FocusOnNavigate` on `h1`, replace the old `h1:focus` rule with `h1:focus { outline: none; }` outside the Bootstrap layer.

Leave the `--app-*` colour tokens, the loading screen and the error bar alone; later tickets replace them.

## Acceptance criteria

- [ ] Bootstrap's CSS loads from the App's own static files, inside the `bootstrap` layer, with no network fetch.
- [ ] No App CSS rule uses a Bootstrap class name as its selector.
- [ ] Buttons, the board table and form controls on both pages render as Bootstrap components.
- [ ] The rename dialog opens as a native modal `<dialog>`: the page behind is inert, Escape closes it, and Rename still saves the new name.
- [ ] The suggestion dialog still opens, closes and saves.
- [ ] After navigating to either page, focus is on the `h1` and no focus ring shows.
- [ ] Canvas components (sticky note, text, property panel, palette, context menu) look the same as before by eye.
- [ ] App tests through the page harness cover the rename dialog: renaming from the editor calls the store with the new name.
- [ ] No comments in any `<style>` block.
