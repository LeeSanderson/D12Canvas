using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// The real Shift+1, Shift+2 and Shift+0 key paths on the framing demo, whose board is authored
// around (50000, 50000) and opens framed at 100%. Reduced motion is on, so every flight lands at
// once. The rows above the content are empty canvas at any pan these probes reach.
public sealed class FramingProbes(PlaywrightFixture playwright, DemoAppFixture demoApp)
    : InteractionProbe(playwright, demoApp)
{
    private const string LeftId = "f4000000-0000-0000-0000-000000000001";
    private const string NoteId = "f4000000-0000-0000-0000-000000000003";

    protected override string ProbePagePath => "/framing-demo";

    protected override int ProbePageInstanceCount => 3;

    private ILocator Container => Page.Locator(".diagram-container");

    private ILocator Content => Page.Locator(".canvas-content");

    private ILocator Instance(string id) =>
        Page.Locator($".component-container[data-d12-entity='{id}']");

    private async Task<LocatorBoundingBoxResult> BoxOfAsync(ILocator locator)
    {
        var box = await locator.BoundingBoxAsync();
        Assert.NotNull(box);
        return box!;
    }

    private async Task FocusCanvasAndPanAwayAsync()
    {
        var empty = await PagePointOnCanvasAsync(20, 20);
        await Page.Mouse.ClickAsync(empty.X, empty.Y);
        for (var i = 0; i < 4; i++)
        {
            await Page.Keyboard.PressAsync("ArrowRight");
        }

        await SettleAsync();
    }

    private static readonly Regex TransformPattern = new(
        @"translate\((?<x>[-\d.e]+)px, (?<y>[-\d.e]+)px\) scale\((?<scale>[-\d.e]+)\)"
    );

    private async Task<(double PanX, double PanY, double Scale)> TransformAsync()
    {
        var match = TransformPattern.Match(await Content.GetAttributeAsync("style") ?? "");
        Assert.True(match.Success);
        return (
            double.Parse(match.Groups["x"].Value, CultureInfo.InvariantCulture),
            double.Parse(match.Groups["y"].Value, CultureInfo.InvariantCulture),
            double.Parse(match.Groups["scale"].Value, CultureInfo.InvariantCulture)
        );
    }

    private static (double X, double Y) BoardPointAtCentre(
        (double PanX, double PanY, double Scale) transform,
        LocatorBoundingBoxResult container
    ) =>
        (
            (container.Width / 2 - transform.PanX) / transform.Scale,
            (container.Height / 2 - transform.PanY) / transform.Scale
        );

    private static void AssertNear(double expected, double actual) =>
        Assert.InRange(actual, expected - 2, expected + 2);

    [Fact]
    public async Task TheBoardOpensWithEveryShapeInsideTheContainerAndNoFlight()
    {
        var container = await BoxOfAsync(Container);
        var shapes = Page.Locator(".component-container");

        for (var i = 0; i < 3; i++)
        {
            var shape = await BoxOfAsync(shapes.Nth(i));
            Assert.InRange(shape.X, container.X, container.X + container.Width - shape.Width);
            Assert.InRange(shape.Y, container.Y, container.Y + container.Height - shape.Height);
        }

        await Expect(Content).Not.ToHaveAttributeAsync("data-d12-flight", "true");
        Assert.DoesNotContain("transition", await Content.GetAttributeAsync("style"));
    }

    [Fact]
    public async Task ShiftOneOnTheFocusedCanvasFramesAllContentAgain()
    {
        var opened = await BoxOfAsync(Instance(LeftId));
        await FocusCanvasAndPanAwayAsync();
        Assert.NotEqual(opened.X, (await BoxOfAsync(Instance(LeftId))).X);
        await ClearCallsAsync();

        await Page.Keyboard.PressAsync("Shift+Digit1");
        await SettleAsync();

        Assert.Single(await CallsToAsync("ZoomToFit"));
        var framed = await BoxOfAsync(Instance(LeftId));
        AssertNear(opened.X, framed.X);
        AssertNear(opened.Y, framed.Y);
    }

    [Fact]
    public async Task ShiftTwoFramesTheSelectionAndShiftZeroReturnsTo100PercentAboutTheCentre()
    {
        await Instance(LeftId).ClickAsync();
        await Page.Keyboard.PressAsync("Shift+Digit2");
        await SettleAsync();

        var container = await BoxOfAsync(Container);
        var framed = await BoxOfAsync(Instance(LeftId));
        AssertNear(container.X + container.Width / 2, framed.X + framed.Width / 2);
        AssertNear(container.Y + container.Height / 2, framed.Y + framed.Height / 2);

        await Page.Keyboard.PressAsync("PageDown");
        await Page.Keyboard.PressAsync("PageDown");
        await SettleAsync();
        var zoomedOut = await TransformAsync();
        Assert.True(zoomedOut.Scale < 0.9);
        var centre = BoardPointAtCentre(zoomedOut, container);

        await Page.Keyboard.PressAsync("Shift+Digit0");
        await SettleAsync();

        Assert.Single(await CallsToAsync("ZoomToSelection"));
        Assert.Single(await CallsToAsync("ZoomTo100Percent"));
        var restored = await TransformAsync();
        Assert.Equal(1.0, restored.Scale);
        var restoredCentre = BoardPointAtCentre(restored, container);
        AssertNear(centre.X, restoredCentre.X);
        AssertNear(centre.Y, restoredCentre.Y);
    }

    [Fact]
    public async Task TheChordsAreLeftToThePageWhileFocusIsOutsideTheCanvas()
    {
        var before = await Content.GetAttributeAsync("style");

        await Page.Keyboard.PressAsync("Shift+Digit1");
        await Page.Keyboard.PressAsync("Shift+Digit0");
        await SettleAsync();

        Assert.Empty(await CallsToAsync("ZoomToFit"));
        Assert.Empty(await CallsToAsync("ZoomTo100Percent"));
        Assert.Equal(before, await Content.GetAttributeAsync("style"));
    }

    [Fact]
    public async Task ShiftOneInsideAnInlineEditorTypesRatherThanFrames()
    {
        await Instance(NoteId).DblClickAsync();
        var editor = Page.Locator("textarea");
        await Expect(editor).ToBeFocusedAsync();
        await ClearCallsAsync();

        await Page.Keyboard.PressAsync("Shift+Digit1");
        await SettleAsync();

        Assert.Empty(await CallsToAsync("ZoomToFit"));
        Assert.Contains("!", await editor.InputValueAsync());
    }
}

// The one place the suite runs a framing flight animated. Every transition on the content element
// is paused as it starts, so a probe can act mid-flight without racing its 250ms, and finished
// when the probe is done with it.
public sealed class FramingFlightProbes(PlaywrightFixture playwright, DemoAppFixture demoApp)
    : InteractionProbe(playwright, demoApp)
{
    private const string LeftId = "f4000000-0000-0000-0000-000000000001";

    private const string PauseFlightsScript = """
        () => document.addEventListener(
            "transitionrun",
            (event) => {
                if (event.target.classList?.contains("canvas-content")) {
                    event.target.getAnimations().forEach((animation) => animation.pause());
                }
            },
            true
        )
        """;

    private const string FinishFlightsScript = """
        () => document.querySelector(".canvas-content").getAnimations().forEach((animation) => animation.finish())
        """;

    private const string RunningTransitionsScript = """
        () => document.querySelector(".canvas-content").getAnimations().length
        """;

    protected override string ProbePagePath => "/framing-demo";

    protected override int ProbePageInstanceCount => 3;

    protected override ReducedMotion Motion => ReducedMotion.NoPreference;

    private ILocator Container => Page.Locator(".diagram-container");

    private ILocator Content => Page.Locator(".canvas-content");

    private ILocator Left => Page.Locator($".component-container[data-d12-entity='{LeftId}']");

    private static readonly Regex InFlight = new("d12-in-flight");

    private async Task FocusCanvasAndPanAwayAsync()
    {
        var empty = await PagePointOnCanvasAsync(20, 20);
        await Page.Mouse.ClickAsync(empty.X, empty.Y);
        for (var i = 0; i < 4; i++)
        {
            await Page.Keyboard.PressAsync("ArrowRight");
        }

        await SettleAsync();
        await Page.EvaluateAsync(PauseFlightsScript);
        await ClearCallsAsync();
    }

    private Task<string> PointerEventsOfAsync(ILocator locator) =>
        locator.EvaluateAsync<string>("element => getComputedStyle(element).pointerEvents");

    private async Task WaitForPausedFlightAsync()
    {
        await Expect(Content).ToHaveAttributeAsync("data-d12-flight", "true");
        for (var attempt = 0; attempt < 50; attempt++)
        {
            if (await Page.EvaluateAsync<int>(RunningTransitionsScript) > 0)
            {
                return;
            }

            await Task.Delay(20);
        }

        Assert.Fail("The framing flight's transition never started.");
    }

    [Fact]
    public async Task APressDuringAFlightIsIgnoredAndAPressAfterItLands()
    {
        await FocusCanvasAndPanAwayAsync();

        await Page.Keyboard.PressAsync("Shift+Digit1");
        await WaitForPausedFlightAsync();

        await Expect(Container).ToHaveClassAsync(InFlight);
        Assert.Equal("none", await PointerEventsOfAsync(Container));
        Assert.Equal("none", await PointerEventsOfAsync(Left));
        Assert.Contains("transition: transform 250ms", await Content.GetAttributeAsync("style"));

        var during = await CentreOfAsync(Left);
        await Page.Mouse.ClickAsync(during.X, during.Y);
        await SettleAsync();
        Assert.Empty(await CallsToAsync("OnPointerPressed"));

        await Page.EvaluateAsync(FinishFlightsScript);
        await Expect(Container).Not.ToHaveClassAsync(InFlight);
        Assert.NotEqual("none", await PointerEventsOfAsync(Left));

        var after = await CentreOfAsync(Left);
        await Page.Mouse.ClickAsync(after.X, after.Y);
        await SettleAsync();
        Assert.Single(await CallsToAsync("OnPointerPressed"));
        await Expect(Left).ToHaveAttributeAsync("aria-selected", "true");
    }

    [Fact]
    public async Task AFlightStartedWhileAPressIsHeldLeavesThePressItsPointer()
    {
        await FocusCanvasAndPanAwayAsync();
        var start = await PagePointOnCanvasAsync(680, 20);
        await Page.Mouse.MoveAsync(start.X, start.Y);
        await Page.Mouse.DownAsync(MiddleDown);
        await Page.Mouse.MoveAsync(start.X - 40, start.Y + 10, new() { Steps = 4 });
        await SettleAsync();
        Assert.Single(await CallsToAsync("OnPointerPressed"));

        await Page.Keyboard.PressAsync("Shift+Digit1");
        await WaitForPausedFlightAsync();

        await Expect(Container).Not.ToHaveClassAsync(InFlight);
        await ClearCallsAsync();
        await Page.Mouse.MoveAsync(start.X - 80, start.Y + 20, new() { Steps = 4 });
        await Page.Mouse.UpAsync(MiddleUp);
        await SettleAsync();

        Assert.NotEmpty(await CallsToAsync("OnPointerMoved"));
        Assert.Single(await CallsToAsync("OnPointerReleased"));
        await Page.EvaluateAsync(FinishFlightsScript);
        await Expect(Container).Not.ToHaveClassAsync(InFlight);
    }
}
