# 41 — Text built-in

**What to build:** An end user places a Text component from the palette: free-standing text on the board with its own props (content, font settings, …), default size, default props, icon, and "Basic Shapes" category — following the pattern ticket 39 established.

**Blocked by:** 39 (Rectangle built-in)

**Status:** resolved

- [x] Text appears in the palette under "Basic Shapes" with its icon
- [x] It registers through the public mechanism with its own props type, default size, and default props
- [x] Font and other visual fields are ordinary props
- [x] Placed instances render correctly
- [x] Screenshot case for a rendered Text component

## Comments

Followed ticket 39/40's pattern exactly: `TextProps.cs` (`Text`, `Color`, `FontSize`, `FontWeight`,
`TextAlign` - ticket 10's exact answer) and `Text.razor` (a plain `[Parameter] public TextProps Props`
component rendering a `<p>` styled from those fields, matching `Rectangle.razor`/`StickyNote.razor`'s
pattern), registered in `BuiltInComponents.RegisterAll` as `"text"` (`DisplayName`/`AccessibleName`
"Text", `Category` "Basic Shapes", `DefaultSize` 200x40, `DefaultProps` `("", "#000000", 16, "normal",
"left")`, icon 🔤), immediately after Sticky Note - so palette order is `rectangle`, `sticky-note`,
`text` after any host-registered types, confirmed by extending
`HostRegisteredComponentsPrecedeBuiltInsInPaletteOrder` in `BuiltInComponentsTests.cs`.

TDD via bUnit at both seams: `TextTests.cs` (render test asserting content/color/font-size/font-weight/
text-align from props, and the same from defaults) written first against a not-yet-existing `Text`/
`TextProps` (confirmed red via compile error), then implemented; and
`AddD12CanvasRegistersTextWithoutAnyHostRegistration` in `BuiltInComponentsTests.cs` written first
against the not-yet-registered key (confirmed red - `UnknownComponentKeyException`), then registered.

Screenshot case: `BoardDemo.razor`'s seeded `Board` gained a sixth instance (a `text`), moving
`BoardRenderingVisualTests`' `.component-container` baseline count from 5 to 6. As with tickets 39/40,
adding a new "Basic Shapes" palette entry bumped every shared `/placement-demo`-and-friends visual
baseline's `.d12-palette-entry` count from 4 to 5, so all 8 affected visual-test classes
(`PaletteVisualTests`, `ClickToAddPlacementVisualTests`, `DragAndDropPlacementVisualTests`,
`DragMoveVisualTests`, `MarqueeVisualTests`, `MultiSelectionMoveResizeVisualTests`, `ResizeVisualTests`,
`SelectionVisualTests`) had their count assertions and baselines regenerated. Every regenerated
HTML/PNG pair was diffed against its prior baseline (with scoped-CSS hash noise normalized out - see
below) before being accepted; every diff was exactly the expected addition (the new palette entry, or
the new `text` `component-container`), nothing else.

**Pre-existing environment drift, not this ticket's fault:** the very first regeneration run showed
every single screenshot baseline failing - not just the 9 files with intentional count-assertion
changes - with a scoped-CSS class-name mismatch (e.g. `b-52l6hnzelf` vs `b-k7rv4lobv5`) affecting the
shared `MainLayout`/`NavMenu` markup common to every test. Reproduced identically across two independent
from-scratch container runs (ruling out stale local build artifacts), then confirmed by checking out
ticket 40's unmodified commit into a scratch worktree and running the exact same pinned
`mcr.microsoft.com/playwright/dotnet:v1.61.0-noble` image against it: the unmodified code also fails
its own committed baseline with the identical hash substitution. This means the Docker tag's contents
drifted since the baselines were generated (Microsoft's tags aren't immutable digests, so the bundled
.NET/Razor SDK patch version - and with it the deterministic scoped-CSS hash formula - can change under
a repo's feet between baseline-generation sessions). Not this ticket's regression; not investigated
further as out of scope, beyond normalizing the hash strings out of every diff before review to confirm
no other content changed. Worth a dedicated ticket if the team wants to pin the image by digest instead
of tag to prevent recurrence.
