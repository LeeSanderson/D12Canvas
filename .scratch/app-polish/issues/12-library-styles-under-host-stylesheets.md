# Make library styles hold up under host stylesheets

Status: resolved
Blocked by:

Spec: [App polish](../spec.md), "Library hardening against host styles".

## What to build

A host that loads Bootstrap (or any stylesheet with a reset like Reboot) no longer changes how the canvas and its chrome look. Today several library components rely on browser defaults for elements Reboot restyles, so the App's Bootstrap work would quietly shift margins, fonts, line heights and form controls inside the canvas.

Audit every library `<style>` block for the elements Reboot touches (`p`, `button`, `label`, `textarea`, `h3`, `ul`, `input`, `select`). Wherever a library rule relies on a browser default, set that property explicitly. Canvas text (sticky note, text) keeps inheriting the host's font family on purpose, since the App picks its font through `--bs-body-font-family`.

The Demo loads no Bootstrap, so this change should move no visual baselines. If a baseline does move, find out why before accepting it.

## Acceptance criteria

- [x] Every library `<style>` block that styles one of the listed elements sets the properties Reboot changes (margins, padding, font size and weight, line height, list style, borders and appearance of form controls) instead of leaving them to the browser.
- [x] Rendering library components with Bootstrap 5.3.8's CSS loaded, imported into a lower cascade layer, matches rendering without it. Check this by hand in a throwaway page or the Demo with Bootstrap linked temporarily; nothing ships with Bootstrap in the Demo.
- [x] No new comments in any `<style>` block.
- [x] The existing library test suite passes.
- [x] The full Playwright visual suite passes in the pinned Docker image with `-parallel none`, with no baseline changes, per `docs/agents/testing.md`.

## Comments

Resolved in the commit "Pin library styles so host resets like Reboot don't change the canvas or its chrome".

The ticket was wrong about one thing: the Demo already links Bootstrap 5.3.3 without a layer (`test/D12Canvas.Demo/wwwroot/index.html`), so every visual baseline renders under Reboot. Pinning browser defaults would have moved them. The library now pins the values the baselines show: `line-height: 1.5` on `.diagram-container` and on the palette, property panel, property bar and context menu roots; `font-size` and `font-weight` on those chrome roots; `box-sizing: border-box` on bordered elements (ports, marquee, selection box, canvas frame); and margin, font, appearance and spin-button/swatch padding on every form control. A host with no reset now renders like the Demo, so it sees 1.5 line spacing and border-box sizing where it used to get browser defaults.

Checked by comparing computed styles of every library element on 12 Demo pages in three setups: the Demo as it is, with Bootstrap removed, and with Bootstrap 5.3.8 in a lower `@layer`. Library elements now match in all three. The only differences left come from the host page's own layout. Library tests: 2176 passed. Visual suite (pinned image, `-parallel none`): 312 passed, no baseline changes.
