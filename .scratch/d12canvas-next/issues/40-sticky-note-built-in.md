# 40 — Sticky Note built-in

**What to build:** An end user places a Sticky Note from the palette: a text-carrying built-in with its own props (text content, note colour, …), its own default size, default props, icon, and "Basic Shapes" category — following the pattern ticket 39 established.

**Blocked by:** 39 (Rectangle built-in)

**Status:** resolved

- [x] Sticky Note appears in the palette under "Basic Shapes" with its icon
- [x] It registers through the public mechanism with its own props type, default size, and default props
- [x] Its colour and other visual fields are ordinary props
- [x] Placed instances render correctly
- [x] Screenshot case for a rendered Sticky Note

## Comments

Followed ticket 39's pattern exactly: `StickyNoteProps.cs` (`Text`, `Color`, `TextColor`, `FontSize` -
ticket 10's exact answer) and `StickyNote.razor` (a plain `[Parameter] public StickyNoteProps Props`
component rendering a coloured note div with the text inside, matching `Rectangle.razor`'s shape),
registered in `BuiltInComponents.RegisterAll` as `"sticky-note"` (`DisplayName`/`AccessibleName`
"Sticky Note", `Category` "Basic Shapes", `DefaultSize` 200x200, `DefaultProps`
`("", "#FFEB3B", "#000000", 14)`, icon 🗒️), immediately after Rectangle - so palette order is
`rectangle` then `sticky-note` after any host-registered types, confirmed by extending
`HostRegisteredComponentsPrecedeBuiltInsInPaletteOrder` in `BuiltInComponentsTests.cs`.

TDD via bUnit at both seams: `StickyNoteTests.cs` (render test asserting text/color/textColor/fontSize
from props, and the same from defaults) written first against a not-yet-existing `StickyNote`/
`StickyNoteProps` (confirmed red via compile error), then implemented; and
`AddD12CanvasRegistersStickyNoteWithoutAnyHostRegistration` in `BuiltInComponentsTests.cs` written
first against the not-yet-registered key (confirmed red - `UnknownComponentKeyException`), then
registered.

Screenshot case: `BoardDemo.razor`'s seeded `Board` gained a fifth instance (a `sticky-note`), moving
`BoardRenderingVisualTests`' `.component-container` baseline count from 4 to 5. As with ticket 39,
adding a new "Basic Shapes" palette entry bumped every shared `/placement-demo`-and-friends visual
baseline's `.d12-palette-entry` count from 3 to 4, so all 8 affected visual-test classes
(`PaletteVisualTests`, `ClickToAddPlacementVisualTests`, `DragAndDropPlacementVisualTests`,
`DragMoveVisualTests`, `MarqueeVisualTests`, `MultiSelectionMoveResizeVisualTests`,
`ResizeVisualTests`, `SelectionVisualTests`) had their count assertions and baselines regenerated.
Every regenerated HTML/PNG pair was diffed against its prior baseline before being accepted; every
diff was exactly the expected addition (the new palette entry, or the new sticky-note
`component-container`), nothing else.

**Pre-existing screenshot-diff flakiness, not this ticket's fault:** re-running the full
`D12Canvas.VisualTests` suite repeatedly after accepting baselines showed a variable number of PNG-only
mismatches (HTML identical, PNG bytes differing with no visible pixel difference on inspection) across
otherwise-unrelated test classes, plus occasional recurrences of ticket 77's already-known cold-boot
click-to-add placement race. Checked out the unmodified ticket-39 commit in a scratch worktree and
reproduced the same PNG-only flakiness there against its own already-committed, untouched baselines -
confirming this is pre-existing test-infra nondeterminism (screenshot timing/encoding, not sticky-note
content) rather than a regression introduced here. Not investigated further as out of scope; a
dedicated ticket would be needed if this needs tightening (e.g. a fuzzy-diff threshold or additional
render-settle wait).
