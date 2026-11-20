# The visual-test harness waits on the page, relies on fixed demo seeds, and compares screenshots with a pixelmatch port

`D12Canvas.VisualTests` holds the screenshot tests and the interaction probes. ADR 0025 says what they verify, and `docs/agents/testing.md` says how to run them, what `ContentSnapshot` captures and what `ModuleInitializer` scrubs from the HTML. This ADR records how the harness is built, the browser behaviours the probes depend on, why the demo pages are seeded the way they are, and how a screenshot is compared with its baseline.

**One `DemoAppFixture` and one `PlaywrightFixture` serve the whole assembly. Each test gets its own browser context and waits for the page to show a state, never for time to pass. Demo seeds are test fixtures. `FuzzyPngComparer` passes a screenshot when at most 0.1% of its pixels differ under a .NET port of mapbox/pixelmatch v7.2.0.**

## Fixtures

`DemoAppFixture` starts `D12Canvas.Demo` once for the assembly with `dotnet run`. The process inherits standard output and error and does not redirect them. Nothing reads them, and Blazor's dev-server build output fills a redirect buffer and deadlocks the process.

`PlaywrightFixture` launches one headless Chromium for the assembly. Each test creates its own `IBrowserContext` and `IPage`, so tests share no cookies, storage or page state.

Every test class takes `DemoAppFixture demoApp` in its constructor without using it, and so does `InteractionProbe`'s constructor, because the parameter states that the class needs the demo app to have finished starting.

`ModuleInitializer` raises Playwright's expect timeout from its 5 second default to 20 seconds. A cold Blazor WebAssembly boot can take longer than 5 seconds to download, JIT and render its first frame, more so on a bind-mounted Docker volume. A slow boot that succeeds must not fail a test.

## Package references

`GlobalUsings.cs` declares the `VerifyTests`, `VerifyXunit` and `Verifier` usings itself. Verify.XunitV3 injects them through buildTransitive props gated on `$(ImplicitUsings)`. Under the .NET SDK in the pinned Playwright Docker image (10.0.301) that injection does nothing and reports nothing, and every `Verify` call then fails with CS0103. Where the injection does work, the duplicate global usings are harmless.

`D12Canvas.App.Tests` references `AngleSharp` 1.5.2 directly. bunit 2.8.4-preview pins a vulnerable AngleSharp floor (GHSA-pgww-w46g-26qg), and the direct reference overrides it.

## Capturing a snapshot

Some demo pages overflow the content area's fixed height, so `ContentSnapshot` captures the element's scroll extent and not only its box.

Before capturing, `ContentSnapshot` waits out the property bar's two hidden states. A bar draws with `visibility: hidden` until it has measured its own width, one interop round trip after it mounts. A full-page capture resizes the viewport, sometimes for a moment to 1x1. The canvas answers by moving the bar, which carries `d12-property-bar-settling` and hides while it settles. If the bar started settling during a capture, `ContentSnapshot` waits for it to settle and captures again, up to three attempts.

## Waiting on the page

The content element's transform eases over a tenth of a second, so a position read straight after a zoom can be part way. Before measuring, a probe reads a box until it stops changing, or waits until a watched shape reaches its expected size. A line's bounding box includes its stroke, so a wait on a line's end allows a few pixels past the pointer.

The suite runs with reduced motion (ADR 0025), so a framing flight lands at once. `FramingFlightProbes` in `FramingProbes.cs` is the one place a flight runs animated. It pauses every transition on the content element as the flight starts, so the probe can act mid-flight without racing the 250ms flight, and finishes them when it is done.

## What tests assume about the demo view

- A demo board opens fitted, which leaves it just below 100%. A probe that needs a known scale presses `Shift+0` first.
- Each zoom key steps the scale by a tenth, about the container's corner. A probe that zooms in on a small shape first pans it toward that corner so it stays on screen.
- The content element has no box of its own, so its client rect is where the board's origin is drawn. `InteractionProbe.PagePointOnBoardAsync` maps board points to page points from it.
- A small shape renders larger than its `Bounds`, so a probe that checks an instance's bounds reads the inline style, not the bounding box.
- Click-to-add cascades each new instance by `+20, +20`. Two default-sized instances then overlap, the later one on top. Tests select such a pair with a marquee, select the first before placing the second, or shift-click the first 5,5 inside its top-left corner, which the second can never cover.
- A dropped connector extends 40px either side of the drop point, so drops aim at the canvas's middle, which is narrower than the page.
- Classification probes dispatch a middle-button press straight at the marked element, so the walk starts from that element whatever lies on top of it. They use the middle button because the listener forwards it from every role and a middle release changes nothing.
- Order and placement keep chrome off the target. A selected note's property bar covers the edge beside it, so the test clicks the edge first. A test pans the board or frames an edge first when a menu or flyout needs room below.
- `Ctrl+G` already moves focus to the new group's tab stop. A test of focusing that stop first clicks empty canvas, which clears the selection and blurs the stop, so its `FocusAsync` is a real focus change and not a no-op.
- Hover states flake in screenshots, so tooltips are probed and never baselined.

## Browser behaviours the probes depend on

bUnit sees none of these. A probe or a failed screenshot found each one.

Clipboard:

- A Blazor Server host cannot answer a synchronous `invokeMethod` from JavaScript. The `copy` and `cut` handler in `DiagramCanvas.razor.js` asks .NET for the payload synchronously, because it must write `clipboardData` inside the event. When that call throws, the handler falls back to the async clipboard. `ClipboardProbes` reproduces such a host by making every synchronous call throw.
- Chromium lets a focused page write to the clipboard without asking, so a successful write says nothing about user activation. `ClipboardProbes` records `navigator.userActivation.isActive` at each write to prove that the menu's Copy row writes while the click's activation is still live. ADR 0013's addendum covers why the menu rows use the async clipboard.

Pointer events:

- The press point reaches C# rounded to a whole pixel, so an anchor computed from it can sit a pixel away from where the test sent the mouse.
- The browser applies a released pointer capture at the next pointer event. A probe that releases capture moves the pointer once before it checks the effect.
- After a `pointercancel` the browser still considers the button held. A probe lets the button up before it checks that nothing responds, and that release must reach no one either.

Native drag and drop:

- Chromium does not repaint the page during a native HTML5 drag session. A screenshot taken mid-drag captures a stale frame even though the DOM and CSS are already correct. Tests of palette drag and drop dispatch a synthetic `dragover` instead.
- Blazor ignores a `dragover` that carries no `DataTransfer`, without an error, so the synthetic event carries a real one.
- A plain pointer drag, as for moving, resizing and marquee selection, repaints normally. Tests drive it with real mouse down, move and up.

## Demo pages are test fixtures

`D12Canvas.Demo` serves the pages the tests drive. Several seeds and registrations look arbitrary but exist to put a code path on screen. A demo page's seed, registrations and assets serve the test that uses them, stay deterministic and offline, and keep each pane independent.

- `DemoShapes` registers enough host types to fill the `Palette` rail and push the rest into its "More components" flyout. With fewer types the flyout goes untested.
- `DemoShapeProps.Outline` is a plain `Dropdown` with no option glyphs, so the property panel draws it as a `<select>`. Glyphs on its options would turn it into tiles and drop the `<select>` path.
- `PropertyBarDemo` puts one rectangle along the content's top. The initial fit leaves it close enough to the container's top that its property bar has to slide along the edge. The page also seeds two rectangles in different fills, which select to a mixed fill, and an edge in an authored colour.
- `BoardDemo` embeds a tiny PNG as a data URL, so its baseline needs no network fetch inside the visual-test container.
- `TwoThemesDemo`, `EdgeColourDemo` and `PropertyBarDemo` give each pane its own `Board` instance. Each pane's `DiagramCanvas` is then a fully independent mount, so the light and dark panes prove per-instance theming and share no state that could hide a defect.

`VirtualizationStressTest` is not a demoed feature. Like `ZoomPanTest` and `ChildComponentTest`, it stays as a dev tool for checking by hand that a change has not slowed rendering of a large synthetic component set (ADR 0035 covers its board-less containers). Its "windowed" variant exercises `DiagramCanvas`'s own `Board` windowing. The user pans by dragging and zooms with the wheel while a HUD reports frame rate and DOM count.

- The HUD in `virtualizationStressTest.js` runs its own `requestAnimationFrame` loop, so its reading follows the browser's real frame pacing and not Blazor's render cycle.
- `RenderedCount` calls `Board.GetVisible` itself on the page's own `_board` and `_overscan`. `DiagramCanvas`'s `Board` and `Overscan` parameters update one render later, so reading them would show a count one render stale right after the node count changes.

## Tolerant screenshot comparison

The same pinned Docker image produces PNGs that differ in bytes but look identical, depending on which physical machine runs it. Verify's default comparison is byte-exact and cannot tell that noise from a real regression. `FuzzyPngComparer.Register`, called from `ModuleInitializer`, replaces it.

### The tolerance

`FuzzyPngComparer.MaxDiffPixelRatio` is 0.001. Calibration used 13 real pairs that should match, taken from two separate GitHub Actions runs and the committed baselines. Every pair measured an exact 0.0% diff under pixelmatch's default threshold. The tolerance sits well above that noise floor, as headroom for slightly worse noise, and orders of magnitude below what an added, moved or changed element produces.

A test guards the other side. `PixelMatchTests.Evaluate_SmallButRealChangeOnActualScreenshot_IsNotEqual` drops a 40x40 solid magenta patch onto a real committed baseline and asserts that the comparer reports a difference, so nobody can loosen the tolerance far enough to swallow a small real change. `FuzzyPngComparer.Evaluate` takes decoded images, so tests exercise the pass or fail decision without encoding a PNG.

### The port

`PixelMatch` keeps upstream's comparison decision: the OKLab HyAB colour difference and the anti-aliasing detector. It drops the `output`, `diffMask` and `windowSize` options, which exist only to draw a diff image, since callers need only a count of differing pixels. Upstream's module-level colour cache becomes a cache local to each `Compare` call, because xUnit runs test collections in parallel and a shared mutable cache would race.

A cleanup that looks faithful would break these details:

- `Options` is a record struct. A struct's implicit parameterless constructor wins over a primary constructor's default arguments, so `new Options()` gives a threshold of 0, not 0.1. Callers use `Options.Default`.
- The anti-aliasing detector follows "Anti-aliased Pixel and Intensity Slope Detector" (V. Vysniauskas, 2009). A pixel counts as anti-aliasing when it sits on a monotonic brightness ramp between two neighbours that each have three or more identical siblings in both images. It needs both a darker and a brighter neighbour, so it never excludes a flat image changed uniformly, or a hard edge that only darkens or only lightens.
- The ramp direction uses a cheap Rec.601 luma delta on purpose, not the OKLab metric that `ColorDelta` uses.
- `ColorDelta` returns 0 when the HyAB distance is within `maxDelta`, otherwise 1 or -1. It is negative when the second image's pixel is darker.
- `OklabHyabDelta` avoids the square root. HyAB is `|dLr| + sqrt(da² + db²)`, which stays within `maxDelta` exactly when `|dLr| <= maxDelta` and `da² + db² <= (maxDelta - |dLr|)²`. Beyond that only the sign matters.
- The sRGB to linear table built by `BuildLinearLut` has a 257th entry, so `LinLut` can always read the next entry when it interpolates.
- `CbrtLut` clamps its index for inputs slightly outside 0 to 1, such as underflow near black. Upstream's JavaScript reads out of bounds and gets NaN without throwing. The clamp keeps that never-crash behaviour without changing normal results.

`PixelMatchTests` covers the port's own decisions. A subtly wrong OKLab or HyAB port would make the comparer too strict, which defeats its purpose, or too lenient, which hides real regressions.

### The decoder

`Png` is a minimal decoder with no dependencies. `PixelMatch` needs raw pixels, and `Png` reads the 8-bit, non-interlaced RGB and RGBA images that Chromium and Playwright produce. It throws on palettes, 16-bit depth and interlacing rather than return wrong pixels.

## Consequences

- A new test class takes `DemoAppFixture` in its constructor.
- A new snapshot source that varies between runs gets a scrubber in `ModuleInitializer`. Baselines do not absorb it.
- A change to the initial fit, the zoom key step, the click-to-add offset or the transform's easing breaks the assumptions above and the probes built on them.
- A change to a demo seed is a change to test coverage, and review treats it as one. Demo assets are embedded, never fetched.
- Removing the `try` around the synchronous copy call breaks clipboard keys on Blazor Server hosts, and no WebAssembly test fails.
- A new screenshot of a native drag comes from a dispatched `dragover` with a `DataTransfer`, never from a live drag.
- A baseline diff under 0.1% of pixels passes without review. The tolerance changes only with new calibration pairs, and only while the magenta patch test still fails.
- Moving to a newer pixelmatch means porting its comparison again, not copying its diff-image code.
