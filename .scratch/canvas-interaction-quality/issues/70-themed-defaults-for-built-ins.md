# 70 — Themed defaults for built-in components

**What to build:** A text or rectangle colour the end user never chose follows the theme (ADR 0034). `TextProps.Color`, `RectangleProps.FillColor` and `RectangleProps.StrokeColor` become nullable and default to null, meaning no author opinion; each built-in resolves null from a board token (`--d12-board-text`, `--d12-board-fill`, `--d12-board-stroke`) in its own CSS, emitting an override custom property only when the value is non-null. Light token values are byte-identical to the old literals, so light boards look the same. The panel's colour editor gains a clear control that writes null, which is the way back to the themed default. Sticky Note's yellow and black stay as literals. Existing boards keep whatever literals they hold; there is no migration. A component author's own component can fall back to the same public board tokens, since nothing in the registration path changes.

**Blocked by:** None — can start immediately

**Status:** ready-for-agent

- [ ] The three props are nullable with null defaults and their registrations' default props carry null
- [ ] Each built-in paints the token when the prop is null and the literal when set, in both themes
- [ ] The canvas declares the three board tokens with light values equal to the old literals and dark values that read on a dark surface
- [ ] The panel's colour editor shows a clear control that commits null; a null value displays as the themed state rather than an empty swatch
- [ ] A board saved with literal colours loads and renders those literals unchanged
- [ ] A demo page shows default text and rectangles on a dark board for the visual suite; light baselines are byte-identical
- [ ] Full visual suite run in the pinned image with `-parallel none`; baselines folded into the commit
- [ ] `CONTEXT.md`'s `Themed default` term describes what shipped
