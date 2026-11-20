# Library style text is carried once per root, matches the C# that sizes it, and keeps its machinery internal

ADR 0012, ADR 0063 and ADR 0064 decide which tokens each library root declares. CONTEXT.md describes `RaisedSurface.TokenRules` and the six components that use it. This records the rules for the rest of the style text the library generates in C#, and for which pieces of that machinery stay out of the public API.

**A library root that draws icons or hosts tooltips carries their rules once in its own style block. Generated attribute selectors are unquoted. Each chrome root pins its own font and button values. A C# size constant and the style block that draws the same box change together. `Tooltip` is a helper, not a component, and `WheelInput` is public only for interop.**

## Shared rules are carried once per root

`D12IconStyles` holds the rules every `D12Icon` needs. A library root that draws icons, such as the palette or the canvas, carries them once in its own style block and cascades `D12IconStyles.InScope`, so the icons inside it leave out their own copy. An icon with nothing in scope, such as one a host places on its own page, still brings the rules itself.

`Tooltip.Rules` works the same way. The root that hosts tooltips carries the rules once in its style block and may set `--d12-tooltip-offset`, the gap between the control's edge and the tooltip. The tooltip shows on the given side of the control it wraps while the pointer is over the control or the control has keyboard focus, never through the native `title`. Its text is hidden from assistive technology, because the control carries its own accessible name.

## Generated attribute selectors are unquoted

`RaisedSurface.TokenRules` writes its `data-d12-theme` attribute selectors without quotes. The text renders inside a `<style>` element, and a prerendered page would encode a quote, which breaks the selector.

## Chrome pins its own text and button values

`PinnedChrome` declares font and button values on each chrome root. A host stylesheet such as Bootstrap's Reboot restyles body text and buttons. Without the pinned values the chrome takes the host's look instead of the one the library drew.

## Fixed sizes mirror the stylesheet

`PropertyBarPlacement.Height` is 34: a 26px cell inside 3px of padding and a 1px border, the same box the bar's stylesheet draws. The constant and the style block describe one box and change together. ADR 0068 records the same rule for `ContextMenuPlacement` and the menu.

## Board values in inline styles

ADR 0073 covers `InlineStyleValue.Safe`, which filters a board value before a built-in writes it into an inline `style` attribute.

## Internal pieces stay out of the public surface

`Tooltip` is a `RenderFragment` helper, not a component, because a `.razor` component is always public. The tooltip is library machinery, not API.

`WheelInput` is public only because Blazor requires an interop entry point and its parameter type to be public. It is one wheel event, or the summed events of one animation frame, as the browser-side listener hands it over. `X` and `Y` are the container-relative pointer, the deltas are in pixels, and `Coarse` is the granularity the listener classified the wheel gesture with at its start. ADR 0069 records the same reason for `PointerPress`.

## Consequences

A new library root that draws icons or hosts tooltips adds `D12IconStyles` or `Tooltip.Rules` to its own style block, and an icon root cascades `D12IconStyles.InScope`.

A new piece of library machinery that renders markup is a `RenderFragment` helper unless it is meant to be public API.
