# 80 — Port pixelmatch to .NET and use it as a tolerant PNG comparer for visual-test baselines

**What's wrong:** Verify's default PNG comparison is byte-exact. Ticket 79 showed that's too strict for
this suite: the identical pinned Docker image produces byte-different but visually-identical screenshots
depending on which physical machine runs it (confirmed nondeterministic even across two separate GitHub
Actions runs of the same unmodified code/baseline), and two mid-resize tests never stabilized against any
baseline at all. HTML snapshots were stable throughout - only the PNG raster comparison was the problem.

**Status:** resolved

**How it was found:** While landing ticket 78, pushing to GitHub Actions surfaced PNG-only failures that
never showed up locally, then a second CI run failed a *different* set of tests using baselines nobody
had touched - ruling out "different but stable per machine" and confirming genuine run-to-run
rasterization nondeterminism on GitHub's own infrastructure. Asked the user how to handle it; they pointed
at Node's `pixelmatch` (the library Playwright's own JS/TS ecosystem uses for exactly this) and asked for
a .NET port rather than pulling in an image-diffing dependency.

**Implementation:**

- `Png.cs` - a minimal, dependency-free PNG decoder (BCL `ZLibStream` only) covering exactly what
  Chromium/Playwright screenshots actually produce: 8-bit, non-interlaced RGB/RGBA. Throws on anything
  else (palettes, 16-bit depth, interlacing) rather than silently decoding wrong pixels.
- `PixelMatch.cs` - a faithful port of `mapbox/pixelmatch` v7.2.0's actual comparison decision: the
  OKLab/HyAB perceptual color-difference metric plus its anti-aliasing detector (Vysniauskas 2009). Drops
  the parts that only exist to render a highlighted diff *image* (`output`/`diffMask`/`windowSize`) since
  callers here only need a pass/fail pixel count. Upstream's module-level color cache became a per-call
  local cache instead, since xUnit runs test collections in parallel and a shared mutable cache would
  race across threads.
- `FuzzyPngComparer.cs` - registers with `VerifierSettings.RegisterStreamComparer("png", ...)`, decodes
  both images, runs them through `PixelMatch.Compare`, and accepts up to a `MaxDiffPixelRatio` (0.001,
  i.e. 0.1%) of differing pixels.
- `PixelMatchTests.cs` - correctness coverage for the port itself (identical images, tiny/large uniform
  color shifts, an exact localized hard-edge change, a mismatched-size guard, and decode/compare
  round-tripping a real committed Chromium screenshot) - a subtly wrong OKLab/HyAB port would either
  defeat the point (too lenient) or mask real regressions (too strict), so this needed its own tests
  before trusting it.

**Threshold calibration:** downloaded the actual `.received.png` artifacts from two separate failed CI
runs (via the workflow's own `visual-test-diffs` upload) and diffed each against whatever baseline it was
"supposed" to match - 13 real pairs spanning local-container-vs-GitHub-runner and GitHub-run-vs-GitHub-run
noise. Every single one measured an exact 0.0% diff under `PixelMatch`'s own default settings (threshold
0.1, anti-aliasing detection on) - strong evidence both that the port is behaving correctly and that this
really was imperceptible rendering noise, not a detection gap. `MaxDiffPixelRatio = 0.001` was chosen as
headroom above that observed 0% noise floor, while remaining far below what any real added/moved/changed
visual element would produce.

**Verification:** ticket 79's two Skip-quarantined tests (`ResizeVisualTests.ResizeInProgress_MatchesBaseline`,
`MultiSelectionMoveResizeVisualTests.GroupResizeInProgress_MatchesBaseline`) were un-skipped and passed
reliably across repeated clean-container runs with the fuzzy comparer active. Full suite (31 visual tests
+ 321 bUnit tests) verified green from a clean `obj`/`bin` state inside the pinned container.

**Noted but not investigated:** one run hit `ClickToAddPlacementVisualTests.
ConsecutiveClickToAdds_CascadeWithAnOffset_MatchesBaseline` failing on an HTML mismatch (`left: -100px;
top: -75px` instead of the correct centered position) - this is ticket 77's already-documented cold-boot
container-dimension race, not a PNG-comparison issue, and it passed on the very next rerun. Adding/
un-skipping tests here likely shifted which test happens to be first to hit the shared `DemoAppFixture`
after a fresh boot, which is exactly the condition ticket 77 describes. Not fixed here - it's ticket 77's
pre-existing bug, not this ticket's.
