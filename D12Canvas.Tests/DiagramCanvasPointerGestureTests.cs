using Bunit;
using D12Canvas.Model;
using D12Canvas.Pointer;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// The pointer arbitration spine, driven through the four public entry points the browser-side
// listener calls. The release-reliability and cancel theories enumerate the closed gesture set,
// so a new member fails the suite until its cases exist; the switches below throw for one they
// do not know. Nothing here inspects which gesture is live: a leak shows as a response to a
// buttonless move, and a cancel shows as what the board and the selection look like afterwards.
public class DiagramCanvasPointerGestureTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasPointerGestureTests()
    {
        SetupDiagramCanvasJsModule();
        SetupComponentContainerJsModule();

        var registry = new ComponentRegistry();
        registry.Register(
            new ComponentRegistration(
                Key: ComponentTypeKey,
                ComponentType: typeof(TestPropsComponent),
                PropsType: typeof(TestProps),
                DisplayName: "Test Props",
                AccessibleName: "Test props component",
                DefaultProps: new TestProps(),
                Icon: null,
                Role: "group",
                DefaultSize: null,
                Category: null
            )
        );
        Services.AddSingleton<IComponentRegistry>(registry);
    }

    // Enumerated by name because the enum is internal and a public theory cannot take it.
    public static IEnumerable<object[]> EveryGestureKind() =>
        Enum.GetNames<GestureKind>().Select(name => new object[] { name });

    private static ComponentInstance AddInstance(Board board, double x, double y)
    {
        var instance = new ComponentInstance(
            ComponentTypeKey,
            new TestProps(),
            new Bounds(x, y, 50, 50)
        );
        board.AddComponent(instance);
        return instance;
    }

    private static string? AriaSelected(IRenderedComponent<DiagramCanvas> canvas, int index) =>
        canvas.FindAll(".component-container")[index].GetAttribute("aria-selected");

    private static string ContentStyle(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.Find(".canvas-content").GetAttribute("style")!;

    // Every case starts on the same board: one instance at (100, 100), another at (400, 400),
    // the first selected before the press. A pan starts with the middle button at (0, 0) and
    // drags to (50, 30). A marquee starts on empty canvas at (380, 380) and drags over the second
    // instance, replacing the selection.
    private IRenderedComponent<DiagramCanvas> RenderSeededBoard(out Board board)
    {
        var seeded = new Board();
        AddInstance(seeded, 100, 100);
        AddInstance(seeded, 400, 400);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, seeded));
        canvas.FindAll(".component-container")[0].Click();
        board = seeded;
        return canvas;
    }

    private static async Task StartAndDrag(
        IRenderedComponent<DiagramCanvas> canvas,
        GestureKind kind
    )
    {
        switch (kind)
        {
            case GestureKind.Pan:
                await canvas.Press(0, 0, PointerPress.MiddleButton);
                await canvas.Move(50, 30);
                break;
            case GestureKind.MarqueeSelect:
                await canvas.Press(380, 380);
                await canvas.Move(460, 460);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "No case for this kind.");
        }
    }

    private static int ClaimingButton(GestureKind kind) =>
        kind switch
        {
            GestureKind.Pan => PointerPress.MiddleButton,
            GestureKind.MarqueeSelect => PointerPress.PrimaryButton,
            _ => throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                "No case for this kind."
            ),
        };

    private static void AssertDragTookEffect(
        IRenderedComponent<DiagramCanvas> canvas,
        GestureKind kind
    )
    {
        switch (kind)
        {
            case GestureKind.Pan:
                Assert.Contains("translate(50px, 30px)", ContentStyle(canvas));
                break;
            case GestureKind.MarqueeSelect:
                Assert.Single(canvas.FindAll(".marquee-select"));
                Assert.Null(AriaSelected(canvas, 0));
                Assert.Equal("true", AriaSelected(canvas, 1));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "No case for this kind.");
        }
    }

    private static void AssertDragWasCancelled(
        IRenderedComponent<DiagramCanvas> canvas,
        GestureKind kind
    )
    {
        switch (kind)
        {
            case GestureKind.Pan:
                // The viewport is never restored.
                Assert.Contains("translate(50px, 30px)", ContentStyle(canvas));
                break;
            case GestureKind.MarqueeSelect:
                Assert.Empty(canvas.FindAll(".marquee-select"));
                Assert.Equal("true", AriaSelected(canvas, 0));
                Assert.Null(AriaSelected(canvas, 1));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "No case for this kind.");
        }
    }

    [Theory]
    [MemberData(nameof(EveryGestureKind))]
    public async Task AReleaseEndsTheGestureAndAButtonlessMoveAfterwardsDoesNothing(string kindName)
    {
        var kind = Enum.Parse<GestureKind>(kindName);
        var canvas = RenderSeededBoard(out _);

        await StartAndDrag(canvas, kind);
        AssertDragTookEffect(canvas, kind);

        // Released outside the canvas: capture delivers it wherever the pointer ends up.
        await canvas.Release(-20, -20, ClaimingButton(kind));
        var styleAfterRelease = ContentStyle(canvas);
        var selectionAfterRelease = (AriaSelected(canvas, 0), AriaSelected(canvas, 1));

        await canvas.Move(300, 300);

        Assert.Equal(styleAfterRelease, ContentStyle(canvas));
        Assert.Equal(selectionAfterRelease, (AriaSelected(canvas, 0), AriaSelected(canvas, 1)));
        Assert.Empty(canvas.FindAll(".marquee-select"));
    }

    [Theory]
    [MemberData(nameof(EveryGestureKind))]
    public async Task EscapeCancelsTheGestureAndTheClaimingReleaseThenDoesNothing(string kindName)
    {
        var kind = Enum.Parse<GestureKind>(kindName);
        var canvas = RenderSeededBoard(out var board);

        await StartAndDrag(canvas, kind);
        await canvas.InvokeAsync(() => canvas.Instance.OnEscapePressed());
        AssertDragWasCancelled(canvas, kind);

        // The cancelled gesture still owns the press, so Escape's selection-clearing rung is not
        // reached and a delete cannot land.
        await canvas.InvokeAsync(() => canvas.Instance.OnEscapePressed());
        await canvas.InvokeAsync(() => canvas.Instance.OnDeletePressed());
        AssertDragWasCancelled(canvas, kind);
        Assert.Equal(2, board.Components.Count);

        await canvas.Move(10, 10);
        await canvas.Release(10, 10, ClaimingButton(kind));
        AssertDragWasCancelled(canvas, kind);

        // The press has ended: the keyboard works again.
        await canvas.InvokeAsync(() => canvas.Instance.OnDeletePressed());
        Assert.Single(board.Components);
    }

    [Theory]
    [MemberData(nameof(EveryGestureKind))]
    public async Task AWindowBlurCancelsAndEndsThePressSoTheKeyboardWorksOnReturn(string kindName)
    {
        var kind = Enum.Parse<GestureKind>(kindName);
        var canvas = RenderSeededBoard(out var board);

        await StartAndDrag(canvas, kind);
        await canvas.Cancel("blur");
        AssertDragWasCancelled(canvas, kind);

        await canvas.Move(300, 300);
        AssertDragWasCancelled(canvas, kind);

        await canvas.InvokeAsync(() => canvas.Instance.OnDeletePressed());
        Assert.Single(board.Components);
    }

    [Theory]
    [InlineData(PointerPress.PrimaryButton, false, "marquee")]
    [InlineData(PointerPress.PrimaryButton, true, "marquee")]
    [InlineData(PointerPress.MiddleButton, false, "pan")]
    [InlineData(PointerPress.MiddleButton, true, "pan")]
    [InlineData(PointerPress.SecondaryButton, false, "pan")]
    [InlineData(PointerPress.SecondaryButton, true, "pan")]
    public async Task APressOnEmptyCanvasResolvesByButton(int button, bool shift, string expected)
    {
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, new Board()));

        await canvas.Press(10, 10, button, shift);
        await canvas.Move(60, 40, shift);

        if (expected == "marquee")
        {
            Assert.Single(canvas.FindAll(".marquee-select"));
            Assert.Contains("translate(0px, 0px)", ContentStyle(canvas));
        }
        else
        {
            Assert.Empty(canvas.FindAll(".marquee-select"));
            Assert.Contains("translate(50px, 30px)", ContentStyle(canvas));
        }
    }

    [Fact]
    public async Task AFastClickOnEmptyCanvasNeverLeavesTheCanvasPanning()
    {
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, new Board()));

        await canvas.ClickCanvas(10, 10);
        await canvas.Move(200, 200);

        Assert.Contains("translate(0px, 0px)", ContentStyle(canvas));
        Assert.Empty(canvas.FindAll(".marquee-select"));
    }

    [Fact]
    public async Task AMiddleClickBelowTheThresholdDoesNothing()
    {
        var canvas = RenderSeededBoard(out _);

        await canvas.ClickCanvas(10, 10, PointerPress.MiddleButton);

        Assert.Contains("translate(0px, 0px)", ContentStyle(canvas));
        Assert.Equal("true", AriaSelected(canvas, 0));
        Assert.Empty(canvas.FindAll(".d12-context-menu"));
    }

    [Fact]
    public async Task ThePanIsAnchoredAtThePressSoADroppedMoveCostsNothing()
    {
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, new Board()));

        await canvas.Press(100, 100, PointerPress.MiddleButton);
        await canvas.Move(120, 110);
        await canvas.Move(170, 130);

        Assert.Contains("translate(70px, 30px)", ContentStyle(canvas));
    }

    [Fact]
    public async Task OtherButtonsPressedAndReleasedMidGestureAreDropped()
    {
        var canvas = RenderSeededBoard(out _);

        await canvas.Press(0, 0, PointerPress.MiddleButton);
        await canvas.Move(50, 30);
        await canvas.Press(50, 30, PointerPress.PrimaryButton);
        await canvas.Release(50, 30, PointerPress.PrimaryButton);
        await canvas.Move(80, 50);

        Assert.Contains("translate(80px, 50px)", ContentStyle(canvas));
        Assert.Equal("true", AriaSelected(canvas, 0));
        Assert.Empty(canvas.FindAll(".marquee-select"));
    }

    [Fact]
    public async Task DeleteUndoAndGroupDoNothingWhileAPressIsHeldAndEscapeAndPageUpStillAct()
    {
        var board = new Board();
        AddInstance(board, 100, 100);
        AddInstance(board, 200, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var containers = canvas.FindAll(".component-container");
        containers[0].Click();
        containers[1].Click(new MouseEventArgs { ShiftKey = true });
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());
        Assert.Single(board.Groups);
        await canvas.InvokeAsync(() => canvas.Instance.OnUngroupPressed());
        Assert.Empty(board.Groups);

        await canvas.Press(0, 0, PointerPress.MiddleButton);
        await canvas.Move(50, 30);

        await canvas.InvokeAsync(() => canvas.Instance.OnDeletePressed());
        Assert.Equal(2, board.Components.Count);
        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());
        Assert.Empty(board.Groups);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());
        Assert.Empty(board.Groups);
        await canvas.InvokeAsync(() => canvas.Instance.OnZoomIn());
        Assert.Contains("scale(1.1)", ContentStyle(canvas));
        await canvas.InvokeAsync(() => canvas.Instance.OnEscapePressed());
        Assert.Equal("true", AriaSelected(canvas, 0));

        await canvas.Release(50, 30, PointerPress.MiddleButton);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());
        Assert.Single(board.Groups);
    }

    public static TheoryData<string> BoardWritingRows() =>
        [
            "Arrow",
            "ShiftArrow",
            "AltArrow",
            "BringForward",
            "BringToFront",
            "SendBackward",
            "SendToBack",
            "Space",
        ];

    private static Task PressRow(IRenderedComponent<DiagramCanvas> canvas, string row) =>
        canvas.InvokeAsync(() =>
        {
            switch (row)
            {
                case "Arrow":
                    canvas.Instance.OnArrowKeyPressed("ArrowRight", false);
                    break;
                case "ShiftArrow":
                    canvas.Instance.OnArrowKeyPressed("ArrowRight", true);
                    break;
                case "AltArrow":
                    canvas.Instance.OnAltArrowKeyPressed("ArrowRight", false);
                    break;
                case "BringForward":
                    canvas.Instance.OnBringForwardPressed();
                    break;
                case "BringToFront":
                    canvas.Instance.OnBringToFrontPressed();
                    break;
                case "SendBackward":
                    canvas.Instance.OnSendBackwardPressed();
                    break;
                case "SendToBack":
                    canvas.Instance.OnSendToBackPressed();
                    break;
                case "Space":
                    canvas.Instance.OnSpacePressed();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(row), row, null);
            }
        });

    [Theory]
    [MemberData(nameof(BoardWritingRows))]
    public async Task ABoardWritingRowDoesNothingWhileAPressIsHeldAndTheGestureCarriesOn(string row)
    {
        var board = new Board();
        var first = AddInstance(board, 100, 100);
        var second = AddInstance(board, 400, 400);
        second.ZIndex = 1;
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.FindAll(".component-container")[0].Focus();

        await canvas.Press(0, 0, PointerPress.MiddleButton);
        await canvas.Move(50, 30);

        await PressRow(canvas, row);

        Assert.Equal(new Bounds(100, 100, 50, 50), first.Bounds);
        Assert.Equal(0, first.ZIndex);
        Assert.Equal(1, second.ZIndex);
        Assert.Equal("true", AriaSelected(canvas, 0));
        Assert.NotEqual("true", AriaSelected(canvas, 1));

        await canvas.Move(80, 50);
        Assert.Contains("translate(80px, 50px)", ContentStyle(canvas));
    }

    // A palette click is a board write from outside the keyboard. Refused while a press is held,
    // it must also leave the selection alone, rather than selecting an instance never placed.
    [Fact]
    public async Task ClickToAddDuringAHeldPressAddsNothingAndLeavesTheSelectionAlone()
    {
        var canvas = RenderSeededBoard(out var board);

        await canvas.Press(0, 0, PointerPress.MiddleButton);
        await canvas.Move(50, 30);
        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd(ComponentTypeKey));

        Assert.Equal(2, board.Components.Count);
        Assert.Equal("true", AriaSelected(canvas, 0));

        await canvas.Release(50, 30, PointerPress.MiddleButton);
        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd(ComponentTypeKey));

        Assert.Equal(3, board.Components.Count);
    }

    [Fact]
    public async Task AHostReplacingTheBoardMidPressCancelsWithoutRestoringTheSnapshot()
    {
        var canvas = RenderSeededBoard(out var first);
        var second = new Board();
        AddInstance(second, 0, 0);

        await canvas.Press(600, 600);
        await canvas.Move(700, 700);
        Assert.Null(AriaSelected(canvas, 0));
        Assert.Single(canvas.FindAll(".marquee-select"));

        canvas.Render(parameters => parameters.Add(p => p.Board, second));
        Assert.Empty(canvas.FindAll(".marquee-select"));

        await canvas.Release(700, 700);
        canvas.Render(parameters => parameters.Add(p => p.Board, first));

        Assert.Null(AriaSelected(canvas, 0));
        Assert.Null(AriaSelected(canvas, 1));
    }

    [Fact]
    public void TheCanvasIsFocusableByScriptButNotByTab()
    {
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, new Board()));

        Assert.Equal("-1", canvas.Find(".diagram-canvas").GetAttribute("tabindex"));
    }

    [Fact]
    public async Task FocusLandingOnTheCanvasDropsTheKeyboardsAnchorSoCtrlTabStartsOver()
    {
        var board = new Board();
        AddInstance(board, 0, 0);
        AddInstance(board, 100, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.FindAll(".component-container")[0].Focus();

        canvas.Find(".diagram-canvas").Focus();
        await canvas.InvokeAsync(() => canvas.Instance.OnCtrlTabPressed());

        var invocation = Assert.Single(JSInterop.Invocations["focusTabStopAt"]);
        Assert.Equal(0, invocation.Arguments[1]);
    }

    [Fact]
    public async Task ASelectionChangedByAMarqueeMovesNoFocus()
    {
        var canvas = RenderSeededBoard(out _);
        var focusCallsBefore = JSInterop.Invocations.Count(invocation =>
            invocation.Identifier.StartsWith("focus")
        );

        await canvas.Marquee(from: (380, 380), to: (460, 460));

        Assert.Equal("true", AriaSelected(canvas, 1));
        Assert.Equal(
            focusCallsBefore,
            JSInterop.Invocations.Count(invocation => invocation.Identifier.StartsWith("focus"))
        );
    }
}
