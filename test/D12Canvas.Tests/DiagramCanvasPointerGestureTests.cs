using Bunit;
using D12Canvas.Model;
using D12Canvas.Pointer;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

public class DiagramCanvasPointerGestureTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasPointerGestureTests()
    {
        SetupDiagramCanvasJsModule();

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

    public static IEnumerable<object[]> EveryGestureKind() =>
        Enum.GetNames<GestureKind>().Select(name => new object[] { name });

    [Fact]
    public void TheGestureSetIsClosedAtEight() =>
        Assert.Equal(
            [
                "Pan",
                "MarqueeSelect",
                "MoveSelection",
                "ResizeSelection",
                "DragEdgeEnd",
                "SelectEdge",
                "Native",
                "MinimapPan",
            ],
            Enum.GetNames<GestureKind>()
        );

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

    private static (double X, double Y) PanOf(IRenderedComponent<DiagramCanvas> canvas) =>
        (canvas.Instance.ZoomPanTracker.PanX, canvas.Instance.ZoomPanTracker.PanY);

    private static void AssertPanNear(
        (double X, double Y) expected,
        IRenderedComponent<DiagramCanvas> canvas
    )
    {
        var (x, y) = PanOf(canvas);
        Assert.Equal(expected.X, x, 6);
        Assert.Equal(expected.Y, y, 6);
    }

    private IRenderedComponent<Minimap>? _minimap;

    private async Task<IRenderedComponent<Minimap>> RenderMinimap(
        IRenderedComponent<DiagramCanvas> canvas
    )
    {
        var minimap = Render<Minimap>(parameters => parameters.Add(p => p.Canvas, canvas.Instance));
        await minimap.Resize(200, 150);
        _minimap = minimap;
        return minimap;
    }

    private Task Move(
        IRenderedComponent<DiagramCanvas> canvas,
        GestureKind kind,
        double x,
        double y
    ) => kind == GestureKind.MinimapPan ? _minimap!.Move(x, y) : canvas.Move(x, y);

    private Task Release(
        IRenderedComponent<DiagramCanvas> canvas,
        GestureKind kind,
        double x,
        double y
    ) =>
        kind == GestureKind.MinimapPan
            ? _minimap!.Release(x, y)
            : canvas.Release(x, y, ClaimingButton(kind));

    private Task Cancel(
        IRenderedComponent<DiagramCanvas> canvas,
        GestureKind kind,
        string reason
    ) => kind == GestureKind.MinimapPan ? _minimap!.Cancel(reason) : canvas.Cancel(reason);

    private IRenderedComponent<DiagramCanvas> RenderSeededBoard(out Board board)
    {
        var seeded = new Board();
        var first = AddInstance(seeded, 100, 100);
        var second = AddInstance(seeded, 400, 400);
        seeded.AddEdge(
            new Edge(
                new PortEndpoint(first.Id, PortId.Right),
                new PortEndpoint(second.Id, PortId.Left)
            )
        );
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, seeded).Add(p => p.SnapToGrid, false)
        );
        canvas.ReturnToOrigin();
        canvas.ClickOn(canvas.FindAll(".component-container")[0]);
        board = seeded;
        return canvas;
    }

    private async Task StartAndDrag(IRenderedComponent<DiagramCanvas> canvas, GestureKind kind)
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
            case GestureKind.MoveSelection:
                await canvas.Press(410, 410, role: HitRole.Instance, entityId: SecondId(canvas));
                await canvas.Move(460, 450);
                break;
            case GestureKind.ResizeSelection:
                await canvas.Press(
                    150,
                    150,
                    role: HitRole.ResizeHandle,
                    entityId: FirstId(canvas),
                    part: "bottom-right"
                );
                await canvas.Move(200, 190);
                break;
            case GestureKind.DragEdgeEnd:
                await canvas.Press(
                    425,
                    400,
                    role: HitRole.Port,
                    entityId: SecondId(canvas),
                    part: "Top"
                );
                await canvas.Move(300, 300);
                break;
            case GestureKind.SelectEdge:
                await canvas.Press(275, 275, role: HitRole.Edge, entityId: EdgeId(canvas));
                await canvas.Move(300, 300);
                break;
            case GestureKind.Native:
                await canvas.Press(
                    410,
                    410,
                    role: HitRole.AuthorContent,
                    entityId: SecondId(canvas)
                );
                break;
            case GestureKind.MinimapPan:
                var minimap = await RenderMinimap(canvas);
                await minimap.Press(100, 75);
                await minimap.Move(55, 41.25);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "No case for this kind.");
        }
    }

    private static Guid FirstId(IRenderedComponent<DiagramCanvas> canvas) =>
        Guid.Parse(canvas.FindAll(".component-container")[0].GetAttribute("data-d12-entity")!);

    private static Guid EdgeId(IRenderedComponent<DiagramCanvas> canvas) =>
        Guid.Parse(canvas.Find(".edge-hit").GetAttribute("data-d12-entity")!);

    private static string FirstStyle(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.FindAll(".component-container")[0].GetAttribute("style")!;

    private static Guid SecondId(IRenderedComponent<DiagramCanvas> canvas) =>
        Guid.Parse(canvas.FindAll(".component-container")[1].GetAttribute("data-d12-entity")!);

    private static string SecondStyle(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.FindAll(".component-container")[1].GetAttribute("style")!;

    private static int ClaimingButton(GestureKind kind) =>
        kind switch
        {
            GestureKind.Pan => PointerPress.MiddleButton,
            GestureKind.MarqueeSelect => PointerPress.PrimaryButton,
            GestureKind.MoveSelection => PointerPress.PrimaryButton,
            GestureKind.ResizeSelection => PointerPress.PrimaryButton,
            GestureKind.DragEdgeEnd => PointerPress.PrimaryButton,
            GestureKind.SelectEdge => PointerPress.PrimaryButton,
            GestureKind.Native => PointerPress.PrimaryButton,
            GestureKind.MinimapPan => PointerPress.PrimaryButton,
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
            case GestureKind.MoveSelection:
                Assert.Contains("left: 450px; top: 440px", SecondStyle(canvas));
                Assert.Null(AriaSelected(canvas, 0));
                Assert.Equal("true", AriaSelected(canvas, 1));
                break;
            case GestureKind.ResizeSelection:
                Assert.Contains("width: 100px; height: 90px", FirstStyle(canvas));
                Assert.Equal("true", AriaSelected(canvas, 0));
                Assert.Null(AriaSelected(canvas, 1));
                break;
            case GestureKind.DragEdgeEnd:
                Assert.Single(canvas.FindAll(".connector-drag-preview"));
                Assert.Equal("true", AriaSelected(canvas, 0));
                break;
            case GestureKind.SelectEdge:
                Assert.Equal("true", AriaSelected(canvas, 0));
                Assert.Null(canvas.Find(".edge-line").GetAttribute("aria-selected"));
                break;
            case GestureKind.Native:
                Assert.Equal("true", AriaSelected(canvas, 0));
                Assert.Equal("true", AriaSelected(canvas, 1));
                break;
            case GestureKind.MinimapPan:
                AssertPanNear((200, 150), canvas);
                Assert.Equal("true", AriaSelected(canvas, 0));
                Assert.Null(AriaSelected(canvas, 1));
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
                Assert.Contains("translate(50px, 30px)", ContentStyle(canvas));
                break;
            case GestureKind.MarqueeSelect:
                Assert.Empty(canvas.FindAll(".marquee-select"));
                Assert.Equal("true", AriaSelected(canvas, 0));
                Assert.Null(AriaSelected(canvas, 1));
                break;
            case GestureKind.MoveSelection:
                Assert.Contains("left: 400px; top: 400px", SecondStyle(canvas));
                Assert.Equal("true", AriaSelected(canvas, 0));
                Assert.Null(AriaSelected(canvas, 1));
                break;
            case GestureKind.ResizeSelection:
                Assert.Contains("width: 50px; height: 50px", FirstStyle(canvas));
                Assert.Equal("true", AriaSelected(canvas, 0));
                Assert.Null(AriaSelected(canvas, 1));
                break;
            case GestureKind.DragEdgeEnd:
                Assert.Empty(canvas.FindAll(".connector-drag-preview"));
                Assert.Single(canvas.FindAll(".edge-line"));
                Assert.Equal("true", AriaSelected(canvas, 0));
                break;
            case GestureKind.SelectEdge:
                Assert.Null(canvas.Find(".edge-line").GetAttribute("aria-selected"));
                Assert.Equal("true", AriaSelected(canvas, 0));
                break;
            case GestureKind.MinimapPan:
                AssertPanNear((200, 150), canvas);
                Assert.Equal("true", AriaSelected(canvas, 0));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "No case for this kind.");
        }
    }

    private static async Task AssertNothingIsHeld(
        IRenderedComponent<DiagramCanvas> canvas,
        Board board
    )
    {
        await canvas.InvokeAsync(() => canvas.Instance.OnDeletePressed());
        Assert.Empty(board.Components);
    }

    [Theory]
    [MemberData(nameof(EveryGestureKind))]
    public async Task AReleaseEndsTheGestureAndAButtonlessMoveAfterwardsDoesNothing(string kindName)
    {
        var kind = Enum.Parse<GestureKind>(kindName);
        var canvas = RenderSeededBoard(out _);

        await StartAndDrag(canvas, kind);
        AssertDragTookEffect(canvas, kind);

        await Release(canvas, kind, -20, -20);
        var styleAfterRelease = ContentStyle(canvas);
        var selectionAfterRelease = (AriaSelected(canvas, 0), AriaSelected(canvas, 1));

        await Move(canvas, kind, 300, 300);

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
        if (kind == GestureKind.Native)
        {
            await canvas.InvokeAsync(() => canvas.Instance.OnEscapePressed());
            Assert.Null(AriaSelected(canvas, 0));
            Assert.Null(AriaSelected(canvas, 1));
            return;
        }

        await canvas.InvokeAsync(() => canvas.Instance.OnEscapePressed());
        AssertDragWasCancelled(canvas, kind);

        await canvas.InvokeAsync(() => canvas.Instance.OnEscapePressed());
        await canvas.InvokeAsync(() => canvas.Instance.OnDeletePressed());
        AssertDragWasCancelled(canvas, kind);
        Assert.Equal(2, board.Components.Count);

        await Move(canvas, kind, 10, 10);
        await Release(canvas, kind, 10, 10);
        AssertDragWasCancelled(canvas, kind);

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
        if (kind == GestureKind.Native)
        {
            await canvas.Cancel("blur");
            AssertDragTookEffect(canvas, kind);
            await AssertNothingIsHeld(canvas, board);
            return;
        }

        await Cancel(canvas, kind, "blur");
        AssertDragWasCancelled(canvas, kind);

        await Move(canvas, kind, 300, 300);
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
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, new Board()).Add(p => p.SnapToGrid, false)
        );

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
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, new Board()).Add(p => p.SnapToGrid, false)
        );

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
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, new Board()).Add(p => p.SnapToGrid, false)
        );

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
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        var containers = canvas.FindAll(".component-container");
        canvas.ClickOn(containers[0]);
        canvas.ClickOn(containers[1], shift: true);
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
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        canvas.ReturnToOrigin();
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
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, new Board()).Add(p => p.SnapToGrid, false)
        );

        Assert.Equal("-1", canvas.Find(".diagram-canvas").GetAttribute("tabindex"));
    }

    [Fact]
    public async Task FocusLandingOnTheCanvasDropsTheKeyboardsAnchorSoSpaceHasNoTarget()
    {
        var board = new Board();
        AddInstance(board, 0, 0);
        AddInstance(board, 100, 0);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        canvas.FindAll(".component-container")[0].Focus();

        canvas.Find(".diagram-canvas").Focus();
        await canvas.InvokeAsync(() => canvas.Instance.OnSpacePressed());
        canvas.FindAll(".component-container")[1].Focus();

        Assert.Null(AriaSelected(canvas, 0));
        Assert.Equal("true", AriaSelected(canvas, 1));
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
