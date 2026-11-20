# DiagramCanvas bUnit tests drive the C# entry points and leave the real key path to the probes

The `DiagramCanvas*Tests` classes in `D12Canvas.Tests` run under bUnit. bUnit renders the canvas into AngleSharp, and `ComponentTestBase.SetupDiagramCanvasJsModule` stubs the canvas JS module. The browser-side keydown and keyup listener never runs, and no native browser behaviour such as `Tab` traversal exists.

**A canvas bUnit test calls the `JSInvokable` handler a key would reach and sets focus with `.Focus()`. An interaction probe in `D12Canvas.VisualTests` proves that the real key arrives.**

## Keys are handler calls

A keyboard test calls the handler directly: `OnArrowKeyPressed`, `OnArrowKeyReleased`, `OnAltArrowKeyPressed`, `OnEnterPressed`, `OnSpacePressed`, `OnEscapePressed`, `OnUndoPressed` and the rest. Space, Enter and Escape are the entry points the keydown table calls. Such a test does not prove that the key reaches the handler. The probes do that: `AdditiveTraversalProbes`, `DirectionalFocusProbes`, `PortPlacementProbes` and `PortRemovalProbes` drive the real key path in a browser. ADR 0025 sets out the split between the layers for pointer input, and `docs/agents/testing.md` covers the pointer entry points.

## Focus is a real focus event

A test sets the focused stop with `.Focus()`, not `.Click()`. `.Focus()` raises a real AngleSharp focus event. It goes through the stop's own `@onfocus`, which is `ComponentContainer.HandleFocus` and then `DiagramCanvas.FocusEntity`, and that is where a native `Tab` lands. The canvas never intercepts native `Tab` or `Shift+Tab`, so "the user tabbed to the next instance" is a second `.Focus()` call.

A move that lands through `focusTabStopAt` is checked the same way. The browser turns that call into a focus on the stop at the given index. The test reads the index back against the rendered stops and calls `.Focus()` on that stop, as the browser would.

## Expected values use the production arithmetic

A test that zooms with `ZoomIn()` computes the scale as `1.0 + 0.1`, the same arithmetic `ZoomPanTracker` uses. It does not write the literal `1.1`. The two differ in double rounding, so an expected value built from the literal can disagree with production code at the tenth decimal place.

## Forcing a render after a direct model write

A test that changes the model directly, for example by assigning `ComponentInstance.Bounds`, raises no event that Blazor sees. It calls `OnContainerResized(800, 600)` to force the render. That call reports the same 800 by 600 container size that the JS module setup already reports, so it changes nothing else.

## Internal enums in theories

`GestureKind` is internal, and a public xUnit theory cannot take an internal type as a parameter. A theory over every gesture kind enumerates the names with `Enum.GetNames<GestureKind>()` and parses each name inside the test.

## Consequences

- A passing bUnit test says nothing about whether the browser delivers the key. A keyboard behaviour that depends on the real key path needs a probe beside its bUnit tests.
- If the canvas starts handling native `Tab` itself, `.Focus()` no longer models what a keyboard user does, and tests built on these conventions need rework.
