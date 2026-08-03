# 70 — LOD placeholder

**What to build:** An end user zooms far out on a dense board and it stays fast and legible: any component instance whose on-screen size (bounds × zoom) drops below a host-configurable threshold renders the generic, non-interactive LOD placeholder — a plain box built from the type's registered `DisplayName`/`Icon` — instead of mounting its full Razor component tree. Zooming back in swaps the real component back. (ADR 0011 — windowing alone can't bound a dense, zoomed-out board.)

**Blocked by:** 69 (Unbounded extent & zoom, configurable limits)

**Status:** resolved

- [x] Instances below the on-screen size threshold render the generic placeholder from `DisplayName`/`Icon`
- [x] Placeholders are non-interactive; zooming in restores the full component seamlessly
- [x] The threshold is host-configurable with a sensible default
- [x] The stress-test page shows the frame-rate benefit on a dense, zoomed-out board
- [x] Screenshot case for a board of placeholders

## Comments

`DiagramCanvas` gained a `LodSizeThreshold` parameter (`double`, default `DefaultLodSizeThreshold =
32`) - unlike `MinZoom`/`MaxZoom` (nullable, unbounded by default) this one is always-on with a
concrete default, matching the "sensible built-in default" the spec called for. An instance's
on-screen size is `Math.Max(bounds.Width, bounds.Height) * Scale` - the larger dimension rather than
area or the smaller dimension, so a naturally thin-but-wide shape isn't perpetually placeholdered at
a normal zoom just because one axis is small. Below the threshold, `DiagramCanvas.razor` swaps the
whole `ComponentContainer`/`DynamicComponent` tree for a plain `<div class="lod-placeholder">`
showing the registration's `Icon` (if any) and `DisplayName`, with `aria-hidden="true"`, no
`tabindex`, and no event handlers - avoiding the mounting cost entirely rather than mounting it
small. `FocusableTabStopIds` excludes placeholdered instances the same way it already excludes
grouped members, so Ctrl+Tab skips them too.

`VirtualizationStressTest.razor` gained an `LodSizeThreshold` numeric control (alongside its existing
`Overscan` control) for the "windowed" variant, so a developer can dial the threshold and zoom out on
the existing dense synthetic board to watch the pre-existing HUD's "mounted DOM nodes" count (and
FPS) improve as instances swap to placeholders - no HUD/JS change needed, since it already counts
`.component-container` elements. A new Playwright case
(`LodPlaceholderVisualTests.DenseZoomedOutBoard_MatchesBaseline`) zooms `/board-demo`'s 7 seeded
instances out until all of them placeholder, asserting zero `.component-container` and seven
`.lod-placeholder` elements before screenshotting.

`/code-review` (Standards + Spec sub-agents) findings and how they were addressed:
- **Spec**: caught a real gap - marquee-select read raw `Board.Components` `Bounds` intersection
  directly, with no LOD awareness, so a marquee drag over a region of placeholders on a zoomed-out
  board could silently add them to the selection (and then to a Delete/Nudge/Alt+Arrow-resize) with
  no visual feedback they were ever selected, contradicting "placeholders are non-interactive."
  Fixed by skipping any instance for which `IsBelowLodThreshold` is true in
  `UpdateMarqueeSelection`, mirroring `FocusableTabStopIds`'s own skip - added
  `MarqueeSelectionSkipsInstancesBelowTheLodThresholdTooNotJustClicksAndTabStops` to cover it. Also
  noted the `max(width, height) * scale` on-screen-size formula is an implementation choice the spec
  itself leaves unstated (both the ticket and ADR 0011 just say "bounds × zoom") - left as-is with
  its own explanatory comment, not a defect.
- **Standards**: no hard violations. Flagged (judgement call) that the new `LodPlaceholderStyle`
  helper repeats the same `left/top/width/height[/z-index]` string-interpolation shape already
  duplicated across `GroupTabStopStyle`/`MarqueeStyle`/`SelectionBoundingBoxStyle`/
  `ComponentContainer.ContainerStyle` - a pre-existing smell this diff grows to a sixth call site;
  left unconsolidated as an unrelated refactor outside this ticket's scope. Also flagged a
  pre-existing test's `Bounds` bump (10x10 → 50x50, to dodge the new default LOD threshold) had no
  comment explaining why - addressed by adding one.

Full `D12Canvas.Tests` suite (669 tests, 1 pre-existing skip) and the full `D12Canvas.VisualTests`
suite (54 tests, run in the pinned Playwright Docker image) pass. Every other visual-test baseline's
`.verified.html` picked up the same one CSS-text addition (the new `.lod-placeholder` rules appended
to `DiagramCanvas.razor`'s shared inline `<style>` block) with no other change - expected fallout from
editing shared canvas-chrome CSS, not a regression (per the existing precedent noted in
`DiagramCanvas.razor`'s own arrowhead-marker comment).
