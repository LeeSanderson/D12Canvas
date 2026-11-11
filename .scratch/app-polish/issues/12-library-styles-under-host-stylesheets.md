# Make library styles hold up under host stylesheets

Status: ready-for-agent
Blocked by:

Spec: [App polish](../spec.md), "Library hardening against host styles".

## What to build

A host that loads Bootstrap (or any stylesheet with a reset like Reboot) no longer changes how the canvas and its chrome look. Today several library components rely on browser defaults for elements Reboot restyles, so the App's Bootstrap work would quietly shift margins, fonts, line heights and form controls inside the canvas.

Audit every library `<style>` block for the elements Reboot touches (`p`, `button`, `label`, `textarea`, `h3`, `ul`, `input`, `select`). Wherever a library rule relies on a browser default, set that property explicitly. Canvas text (sticky note, text) keeps inheriting the host's font family on purpose, since the App picks its font through `--bs-body-font-family`.

The Demo loads no Bootstrap, so this change should move no visual baselines. If a baseline does move, find out why before accepting it.

## Acceptance criteria

- [ ] Every library `<style>` block that styles one of the listed elements sets the properties Reboot changes (margins, padding, font size and weight, line height, list style, borders and appearance of form controls) instead of leaving them to the browser.
- [ ] Rendering library components with Bootstrap 5.3.8's CSS loaded, imported into a lower cascade layer, matches rendering without it. Check this by hand in a throwaway page or the Demo with Bootstrap linked temporarily; nothing ships with Bootstrap in the Demo.
- [ ] No new comments in any `<style>` block.
- [ ] The existing library test suite passes.
- [ ] The full Playwright visual suite passes in the pinned Docker image with `-parallel none`, with no baseline changes, per `docs/agents/testing.md`.
