# 42 — Image built-in

**What to build:** An end user places an Image component from the palette: it renders an image from a URL prop with sensible fitting, has its own default size, default props, icon, and "Basic Shapes" category — following the pattern ticket 39 established.

**Blocked by:** 39 (Rectangle built-in)

**Status:** resolved

- [x] Image appears in the palette under "Basic Shapes" with its icon
- [x] It registers through the public mechanism with its own props type (source URL, fit) and defaults
- [x] A missing/unloadable source renders a graceful placeholder state rather than breaking
- [x] Placed instances render correctly
- [x] Screenshot case for a rendered Image

## Comments

Followed ticket 39/40/41's pattern, using ticket 10's exact answer: `ImageProps.cs` (`Url`,
`AltText`, `Fit`) and `Image.razor` (a plain `[Parameter] public ImageProps Props` component),
registered in `BuiltInComponents.RegisterAll` as `"image"` (`DisplayName`/`AccessibleName` "Image",
`Category` "Basic Shapes", `DefaultSize` 240x180, `DefaultProps` `("", "", "cover")`, icon 🖼️),
immediately after Text - so palette order is `rectangle`, `sticky-note`, `text`, `image` after any
host-registered types, confirmed by extending `HostRegisteredComponentsPrecedeBuiltInsInPaletteOrder`
in `BuiltInComponentsTests.cs`.

Unlike the other three built-ins, Image carries transient render state: `_hasError`, flipped by an
`@onerror` handler on the `<img>` element (Blazor's native media-element error event), and reset via
`OnParametersSet` whenever `Props.Url` changes to a new value - so a host correcting a broken URL
gets a fresh chance to load rather than being stuck on the placeholder. The registration contract's
`AccessibleName` stays a fixed per-type string (per ADR 0001 - it doesn't support a per-instance
delegate), so ticket 10's note about `AltText` feeding accessibility is realized at the render level
instead: `Image.razor` sets the `<img>`'s own `alt` attribute from `Props.AltText`, which is the
correct, standard place for per-instance image accessibility text.

**Placeholder state:** both "missing source" (`Url` empty/whitespace) and "unloadable source"
(`@onerror` fired) render the same `.d12-image-placeholder` div (a dashed-border box with a 🖼️ glyph
and short label - "No image" vs. "Image unavailable") instead of a broken `<img>`.

TDD via bUnit at both seams: `ImageTests.cs` (written first against a not-yet-existing `Image`/
`ImageProps`, confirmed red via compile error) covers props-driven rendering (url/alt/fit), the
missing-source placeholder, the onerror-triggered placeholder (bUnit's generated `.Error()` trigger
on the `<img>` element), and recovery after a `Props` update supplies a new URL; and
`AddD12CanvasRegistersImageWithoutAnyHostRegistration` in `BuiltInComponentsTests.cs` (written first
against the not-yet-registered key, confirmed red - `UnknownComponentKeyException`).

Screenshot case: `BoardDemo.razor`'s seeded `Board` gained a seventh instance (an `image`, using a
tiny embedded base64 PNG data URI rather than a real network URL, so the baseline stays deterministic
and doesn't depend on network access from inside the Playwright Docker container), moving
`BoardRenderingVisualTests`' `.component-container` baseline count from 6 to 7. As with tickets
39-41, adding a new "Basic Shapes" palette entry bumped every shared `/placement-demo`-and-friends
visual baseline's `.d12-palette-entry` count from 5 to 6, so all 9 affected visual-test classes
(`PaletteVisualTests`, `ClickToAddPlacementVisualTests`, `DragAndDropPlacementVisualTests`,
`DragMoveVisualTests`, `MarqueeVisualTests`, `MultiSelectionMoveResizeVisualTests`,
`ResizeVisualTests`, `SelectionVisualTests`, plus `BoardRenderingVisualTests` itself) had their count
assertions and baselines regenerated via the pinned
`mcr.microsoft.com/playwright/dotnet:v1.61.0-noble` image. Every regenerated HTML/PNG pair was
diffed against its prior baseline before being accepted; every diff was exactly the expected addition
(the new palette entry, or the new `image` `component-container`), nothing else - no repeat of the
scoped-CSS-hash or cold-boot flakiness prior tickets ran into.
