# The property panel joins the token layer, and every surface that floats over the board declares the raised values

`PropertyPanel` had no token layer. It had five colour literals, no `--d12-*` declarations and no dark block, so it rendered white on a dark host. ADR 0012's list of chrome elements never named it. The colour sweep found it, and the acceptance surface now floats it over the full-bleed canvas, where it is the brightest thing on screen.

## The panel declares Palette's four blocks

**`.d12-property-panel` declares the same four token blocks as `Palette` and `SelectionContextMenu`, with `Palette`'s values.** These are a light default, a `prefers-color-scheme: dark` block, and the two `data-d12-theme` overrides. The background reads `--d12-surface`, the panel and input borders read `--d12-border`, and the labels and the empty message read `--d12-muted-text`. The light values equal the old literals byte for byte.

**The root also sets `color: var(--d12-text)`.** The panel set no text colour before, so input and dropdown text inherited whatever the host set. On a dark host with light body text, that put light text in white inputs. This was a sixth gap beside the five literals.

## Board values and raised values

The canvas and the floating chrome have always disagreed on three tokens:

| Token | Canvas (light / dark) | Palette, context menu (light / dark) |
|---|---|---|
| `--d12-surface` | `#f0f0f0` / `#1e1e1e` | `#fff` / `#2a2a2a` |
| `--d12-border` | `rgba(0,0,0,0.1)` / `rgba(255,255,255,0.12)` | `#ccc` / `rgba(255,255,255,0.12)` |
| `--d12-muted-text` | `#6b6b6b` / `#a0a0a0` | `#666` / `#a0a0a0` |

ADR 0012 calls each component's block a copy, which reads as if the copies should match. The surface and border differ for a reason. The canvas's `--d12-surface` is the board background and its `--d12-border` is the grid-line colour (ADR 0063). Anything floating over the board has to be lighter than the board in both themes, or it disappears into it.

**So there are two value sets, and the rule is which one a component declares:**

- **Board values** are the canvas's block. The board and what is drawn on it read them.
- **Raised values** are `Palette`'s block. Every surface that floats over the board declares them on its own root: `Palette`, the context menu, the property panel, the minimap (ADR 0015) and the property bar (ADR 0021).

The property bar is the case the rule protects. It renders inside `.diagram-container`, so it would inherit board values and draw a `#f0f0f0` bar on a `#f0f0f0` board. It redeclares the raised values on its own root, as the context menu already does from the same position.

**The muted-text gap stays.** `#666` against `#6b6b6b` looks unchosen, but closing it moves baselines for no visible gain, and the rule does not depend on it.

## `color-scheme` is part of the raised values

The panel's inputs and `<select>` have parts CSS cannot style: the select's popup list, the checkbox tick, the number spinner and the colour input's swatch frame. Only the `color-scheme` property tells the browser to draw them dark.

**Each raised block sets `color-scheme`**: `light` in the light default and the light override, `dark` in the media-query block and the dark override. The property inherits, so an author's custom editor inside the panel gets it too. `Palette` and the context menu adopt it in the same change. Neither has form controls, so only scrollbars should differ, and their dark baselines are checked rather than assumed.

**`.d12-property-panel-input` sets `background: var(--d12-surface)` and `color: inherit`.** Without them, dark Chrome fills the input with its own field grey, which does not match the panel. In light the background stays `#fff`, the browser default. Input text moves from the browser's black to `#212529`, so the four light panel baselines move. Keeping the browser default in light alone would leave the kind of unowned value the colour sweep removed.

## Verification

- **Two dark baselines**: `PopulatedPanel_DarkColorScheme` (Rectangle: text, number and colour inputs) and `PopulatedPanelWithDropdownControl_DarkColorScheme` (Text: the closed select). `PropertyPanelVisualTests` takes `PaletteVisualTests`' `NewPageAsync(ColorScheme)` shape.
- **No dark empty or custom-control baseline.** The first dark baseline already shows muted text on the surface, and a custom editor's colours belong to its author.
- **`PropertyPanelThemeTokensTests`** mirrors `PaletteThemeTokensTests`: the four blocks, the five tokens and `color-scheme` in each. A screenshot cannot capture the open select popup, which the OS draws, so this test is the only check on it. The `Palette` and context-menu token tests also assert `color-scheme`.
- **No `data-d12-theme` override baseline.** `ThemeVisualTests` covers the override mechanism, and the panel's override selectors are pinned by the unit test.

## Amends, confirms

- **Extends ADR 0012** by naming the panel as chrome and by stating which of two copies a component declares. Nothing in ADR 0012 is reversed.
- **Binds ADR 0015's minimap and ADR 0021's property bar** when they are built. Both declare raised values on their own root.
- **Leaves ADR 0063's guard test where it was.** That test covers the canvas and container style blocks only. A panel equivalent comes from the token test above.

## Considered and rejected

- **Leaving form controls on browser defaults.** White inputs on a `#2a2a2a` panel, and a white popup from the select.
- **Styling controls through tokens alone.** The popup, checkbox and spinner stay light, because CSS cannot reach them.
- **Keeping the browser's black input text in light.** Saves four small baseline moves and keeps a colour nobody chose.
- **Closing the `#666` and `#6b6b6b` gap.** Moves baselines in every raised component for a difference nobody can see.
- **One value set, with the raised surfaces reading the canvas's.** A floating surface drawn in the board's own colour.
- **A shared stylesheet or base component holding the raised block.** ADR 0012 has each component declare its own copy so it themes correctly standalone.
- **Dark baselines for all four panel cases.** The other two add nothing the first two do not show.
