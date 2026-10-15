using System.Globalization;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Pointer;
using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// The two routes to a custom port: the pointer's Add port here row, which adds one where the
// menu's press landed on the border, and the keyboard's Add port… row, which starts placement.
// The real key path through Shift+F10 is in PortPlacementProbes.
public class DiagramCanvasPortPlacementTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";
    private const string AddPortHere = "Add port here";
    private const string AddPortByKeyboard = "Add port…";

    private readonly Board _board = new();
    private readonly ComponentInstance _shape;
    private readonly ComponentInstance _other;

    public DiagramCanvasPortPlacementTests()
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

        _shape = AddInstance(new Bounds(100, 100, 200, 100));
        _other = AddInstance(new Bounds(400, 100, 200, 100));
    }

    private ComponentInstance AddInstance(Bounds bounds)
    {
        var instance = new ComponentInstance(ComponentTypeKey, new TestProps(), bounds);
        _board.AddComponent(instance);
        return instance;
    }

    private IRenderedComponent<DiagramCanvas> RenderCanvas(bool snapToGrid = true) =>
        Render<DiagramCanvas>(parameters =>
                parameters.Add(p => p.Board, _board).Add(p => p.SnapToGrid, snapToGrid)
            )
            .ReturnToOrigin();

    private static string[] MenuLabels(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.FindAll(".d12-context-menu-label").Select(label => label.TextContent).ToArray();

    private static IElement MenuRow(IRenderedComponent<DiagramCanvas> canvas, string label) =>
        canvas
            .FindAll(".d12-context-menu-item")
            .Single(item => item.QuerySelector(".d12-context-menu-label")!.TextContent == label);

    private static async Task RightClick(
        IRenderedComponent<DiagramCanvas> canvas,
        ComponentInstance instance,
        double x,
        double y,
        string role = HitRole.Instance,
        string? part = null
    )
    {
        await canvas.Press(
            x,
            y,
            PointerPress.SecondaryButton,
            role: role,
            entityId: instance.Id,
            part: part
        );
        await canvas.Release(x, y, PointerPress.SecondaryButton);
    }

    private void FocusShape(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.ContainerOf(_shape.Id).Focus();

    private async Task StartPlacement(IRenderedComponent<DiagramCanvas> canvas)
    {
        FocusShape(canvas);
        await canvas.InvokeAsync(() => canvas.Instance.OnContextMenuKeyPressed());
        MenuRow(canvas, AddPortByKeyboard).Click();
    }

    private static Task Arrow(
        IRenderedComponent<DiagramCanvas> canvas,
        string code,
        bool shift = false
    ) => canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed(code, shift));

    private static Task Enter(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed());

    private static Task Escape(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.InvokeAsync(() => canvas.Instance.OnEscapePressed());

    private IElement? ProvisionalDot(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.ContainerOf(_shape.Id).QuerySelector(".port-provisional");

    private (double X, double Y) ProvisionalFractions(IRenderedComponent<DiagramCanvas> canvas)
    {
        var style = ProvisionalDot(canvas)!.GetAttribute("style")!;
        return (Percent(style, "left") / 100, Percent(style, "top") / 100);
    }

    private static double Percent(string style, string property) =>
        double.Parse(
            Regex.Match(style, property + @": calc\((-?[\d.E-]+)%").Groups[1].Value,
            CultureInfo.InvariantCulture
        );

    private static void AssertFractions((double X, double Y) expected, (double X, double Y) actual)
    {
        Assert.Equal(expected.X, actual.X, 9);
        Assert.Equal(expected.Y, actual.Y, 9);
    }

    [Fact]
    public async Task ARightClickOnASidesResizeSpanAddsAPortAtThatFractionInOneEntry()
    {
        var canvas = RenderCanvas();
        canvas.ClickOn(canvas.ContainerOf(_shape.Id));

        await RightClick(canvas, _shape, 180, 100, HitRole.ResizeHandle, "top");
        Assert.Contains(AddPortHere, MenuLabels(canvas));
        Assert.DoesNotContain(AddPortByKeyboard, MenuLabels(canvas));
        MenuRow(canvas, AddPortHere).Click();

        var port = Assert.Single(_shape.CustomPorts);
        Assert.Equal(0.4, port.FractionX, 9);
        Assert.Equal(0, port.FractionY);
        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());
        Assert.Empty(_shape.CustomPorts);
    }

    [Theory]
    [InlineData("right", 300, 175, 1, 0.75)]
    [InlineData("bottom", 250, 200, 0.75, 1)]
    [InlineData("left", 100, 125, 0, 0.25)]
    public async Task EachSidesResizeSpanAddsThePortOnThatSide(
        string part,
        double x,
        double y,
        double expectedX,
        double expectedY
    )
    {
        var canvas = RenderCanvas();
        canvas.ClickOn(canvas.ContainerOf(_shape.Id));

        await RightClick(canvas, _shape, x, y, HitRole.ResizeHandle, part);
        MenuRow(canvas, AddPortHere).Click();

        var port = Assert.Single(_shape.CustomPorts);
        AssertFractions((expectedX, expectedY), (port.FractionX, port.FractionY));
    }

    [Fact]
    public async Task ARightClickOnAPortSpanAlsoOffersAddPortHere()
    {
        var canvas = RenderCanvas();
        canvas.ClickOn(canvas.ContainerOf(_shape.Id));

        await RightClick(canvas, _shape, 205, 100, HitRole.Port, "Top");

        Assert.Contains(AddPortHere, MenuLabels(canvas));
    }

    [Fact]
    public async Task ARightClickOnTheBodyOrACornerOffersNoAddPortRow()
    {
        var canvas = RenderCanvas();
        canvas.ClickOn(canvas.ContainerOf(_shape.Id));

        await RightClick(canvas, _shape, 200, 150);
        Assert.DoesNotContain(AddPortHere, MenuLabels(canvas));
        Assert.DoesNotContain(AddPortByKeyboard, MenuLabels(canvas));
        await Escape(canvas);

        await RightClick(canvas, _shape, 100, 100, HitRole.ResizeHandle, "top-left");
        Assert.DoesNotContain(AddPortHere, MenuLabels(canvas));
    }

    [Fact]
    public async Task ARightClickOnALockedShapesBorderOffersNoAddPortRow()
    {
        _shape.Locked = true;
        var canvas = RenderCanvas();

        await RightClick(canvas, _shape, 180, 100, HitRole.ResizeHandle, "top");

        Assert.DoesNotContain(AddPortHere, MenuLabels(canvas));
    }

    [Fact]
    public async Task TheKeyboardMenuOnASelectedShapeOffersAddPortAndNotAddPortHere()
    {
        var canvas = RenderCanvas();
        FocusShape(canvas);

        await canvas.InvokeAsync(() => canvas.Instance.OnContextMenuKeyPressed());

        Assert.Contains(AddPortByKeyboard, MenuLabels(canvas));
        Assert.DoesNotContain(AddPortHere, MenuLabels(canvas));
    }

    [Fact]
    public async Task TheKeyboardMenuOnTwoShapesOrALockedShapeOffersNoAddPortRow()
    {
        var canvas = RenderCanvas();
        canvas.ClickOn(canvas.ContainerOf(_shape.Id));
        canvas.ClickOn(canvas.ContainerOf(_other.Id), shift: true);
        await canvas.InvokeAsync(() => canvas.Instance.OnContextMenuKeyPressed());
        Assert.DoesNotContain(AddPortByKeyboard, MenuLabels(canvas));
        await Escape(canvas);

        _shape.Locked = true;
        canvas.ClickOn(canvas.ContainerOf(_other.Id));
        FocusShape(canvas);
        await canvas.InvokeAsync(() => canvas.Instance.OnContextMenuKeyPressed());
        Assert.DoesNotContain(AddPortByKeyboard, MenuLabels(canvas));
    }

    [Fact]
    public async Task AddPortStartsAHollowProvisionalPortOnTheTopSideAndHandsFocusToTheShape()
    {
        var canvas = RenderCanvas();

        await StartPlacement(canvas);

        Assert.Empty(canvas.FindAll(".d12-context-menu"));
        AssertFractions((0.3, 0), ProvisionalFractions(canvas));
        Assert.Contains("port-focused", ProvisionalDot(canvas)!.ClassName);
        Assert.Empty(_shape.CustomPorts);
        var index = (int)JSInterop.Invocations["focusTabStopAt"].Last().Arguments[1]!;
        Assert.Equal(
            _shape.Id.ToString(),
            canvas
                .FindAll(".instance-layer [tabindex=\"0\"]")[index]
                .GetAttribute("data-d12-entity")
        );
    }

    [Fact]
    public async Task WithoutSnapTheStartIsAQuarterAlongTheTopSide()
    {
        var canvas = RenderCanvas(snapToGrid: false);

        await StartPlacement(canvas);

        AssertFractions((0.25, 0), ProvisionalFractions(canvas));
    }

    public static TheoryData<string, double, double> Walks =>
        new()
        {
            { "", 0.3, 0 },
            { "ArrowRight", 0.4, 0 },
            { "ArrowLeft", 0.2, 0 },
            { "ArrowRight*8", 1, 0 },
            { "ArrowRight*8 ArrowDown", 1, 0.2 },
            { "ArrowRight*8 ArrowDown ArrowLeft", 0, 0.2 },
            { "ArrowRight*8 ArrowRight", 1, 0 },
            { "ArrowRight*8 ArrowRight ArrowRight", 1, 0 },
            { "ArrowLeft*4", 0, 0 },
            { "ArrowLeft*4 ArrowLeft ArrowLeft", 0, 0 },
            { "ArrowLeft*4 ArrowDown", 0, 0.2 },
            { "ArrowDown", 0.3, 1 },
            { "ArrowDown ArrowDown", 0.3, 1 },
            { "ArrowDown ArrowUp", 0.3, 0 },
            { "ArrowUp", 0.3, 0 },
            { "ArrowRight*8 ArrowDown*5", 1, 1 },
            { "ArrowRight*8 ArrowDown*5 ArrowDown ArrowLeft", 0.9, 1 },
        };

    [Theory]
    [MemberData(nameof(Walks))]
    public async Task TheArrowsWalkTheProvisionalPortAroundTheBorder(
        string presses,
        double expectedX,
        double expectedY
    )
    {
        var canvas = RenderCanvas();
        await StartPlacement(canvas);

        foreach (var press in presses.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = press.Split('*');
            var times = parts.Length > 1 ? int.Parse(parts[1], CultureInfo.InvariantCulture) : 1;
            for (var i = 0; i < times; i++)
            {
                await Arrow(canvas, parts[0]);
            }
        }

        AssertFractions((expectedX, expectedY), ProvisionalFractions(canvas));
        Assert.Empty(_shape.CustomPorts);
        Assert.Equal(new Bounds(100, 100, 200, 100), _shape.Bounds);
    }

    [Fact]
    public async Task ShiftMovesTenSteps()
    {
        _shape.Bounds = new Bounds(100, 100, 400, 100);
        var canvas = RenderCanvas();
        await StartPlacement(canvas);

        await Arrow(canvas, "ArrowRight", shift: true);

        AssertFractions(((200 + 200 - 100) / 400.0, 0), ProvisionalFractions(canvas));
    }

    [Theory]
    [InlineData(true, 1.0)]
    [InlineData(false, 2.0)]
    [InlineData(false, 0.5)]
    public async Task OneStepIsTheNudgeStepAtTheSameZoomAndSnap(bool snapToGrid, double zoom)
    {
        var canvas = RenderCanvas(snapToGrid);
        await canvas.InvokeAsync(() => canvas.Instance.ZoomPanTracker.Scale = zoom);
        await StartPlacement(canvas);
        var startX = 100 + ProvisionalFractions(canvas).X * 200;

        await Arrow(canvas, "ArrowRight");
        var placementStep = 100 + ProvisionalFractions(canvas).X * 200 - startX;
        await Escape(canvas);
        var origin = new Bounds(startX, 100, 200, 100);
        _shape.Bounds = origin;
        canvas.Render();
        await Arrow(canvas, "ArrowRight");

        Assert.Equal(_shape.Bounds.X - origin.X, placementStep, 9);
    }

    [Fact]
    public async Task EnterAddsThePortInOneEntryWhateverTheSlidesAndKeepsFocusOnTheShape()
    {
        var canvas = RenderCanvas();
        await StartPlacement(canvas);
        var focusMoves = JSInterop.Invocations["focusTabStopAt"].Count;
        await Arrow(canvas, "ArrowRight");
        await Arrow(canvas, "ArrowRight");
        await Arrow(canvas, "ArrowDown");

        await Enter(canvas);

        var port = Assert.Single(_shape.CustomPorts);
        AssertFractions((0.5, 1), (port.FractionX, port.FractionY));
        Assert.Null(ProvisionalDot(canvas));
        Assert.Equal(focusMoves, JSInterop.Invocations["focusTabStopAt"].Count);
        Assert.Equal([_shape.Id], canvas.Instance.SelectedComponents.Select(i => i.Id));

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());
        Assert.Empty(_shape.CustomPorts);
        Assert.Equal(new Bounds(100, 100, 200, 100), _shape.Bounds);
    }

    [Fact]
    public async Task TheAddedPortIsInThePortPicksSpaceCycleAtOnce()
    {
        var canvas = RenderCanvas();
        await StartPlacement(canvas);
        await Enter(canvas);
        var port = Assert.Single(_shape.CustomPorts);

        await Enter(canvas);
        for (var press = 0; press < 5; press++)
        {
            await canvas.InvokeAsync(() => canvas.Instance.OnSpacePressed());
        }

        var focused = canvas.ContainerOf(_shape.Id).QuerySelector(".custom-port.port-focused");
        Assert.NotNull(focused);
        Assert.Contains(
            FormattableString.Invariant($"{port.FractionX * 100}%"),
            focused!.GetAttribute("style")
        );
    }

    [Fact]
    public async Task EscapeEndsPlacementKeepingTheSelectionAndTheNextEscapeClearsIt()
    {
        var canvas = RenderCanvas();
        await StartPlacement(canvas);
        await Arrow(canvas, "ArrowRight");

        await Escape(canvas);

        Assert.Null(ProvisionalDot(canvas));
        Assert.Empty(_shape.CustomPorts);
        Assert.Equal([_shape.Id], canvas.Instance.SelectedComponents.Select(i => i.Id));

        await Escape(canvas);
        Assert.Empty(canvas.Instance.SelectedComponents);
    }

    [Fact]
    public async Task FocusMovingToAnotherStopEndsPlacementWithoutAddingAPort()
    {
        var canvas = RenderCanvas();
        await StartPlacement(canvas);
        FocusShape(canvas);

        canvas.ContainerOf(_other.Id).Focus();

        Assert.Null(ProvisionalDot(canvas));
        Assert.Empty(_shape.CustomPorts);
        Assert.Empty(_other.CustomPorts);
    }

    [Fact]
    public async Task APointerPressEndsPlacementWithoutAddingAPort()
    {
        var canvas = RenderCanvas();
        await StartPlacement(canvas);
        FocusShape(canvas);

        await canvas.ClickCanvas(700, 400);

        Assert.Null(ProvisionalDot(canvas));
        await Enter(canvas);
        Assert.Empty(_shape.CustomPorts);
    }

    [Fact]
    public async Task FocusLeavingTheContainerEndsPlacement()
    {
        var canvas = RenderCanvas();
        await StartPlacement(canvas);

        await canvas.InvokeAsync(() => canvas.Instance.OnFocusLeftContainer());

        Assert.Null(ProvisionalDot(canvas));
    }

    [Fact]
    public async Task TheMenuHandingFocusBackToTheCanvasBeforeTheShapesStopDoesNotEndPlacement()
    {
        var canvas = RenderCanvas();
        canvas.ClickOn(canvas.ContainerOf(_shape.Id));
        await canvas.InvokeAsync(() => canvas.Instance.OnContextMenuKeyPressed());
        MenuRow(canvas, AddPortByKeyboard).Click();

        canvas.Find(".diagram-canvas").Focus();
        FocusShape(canvas);

        Assert.NotNull(ProvisionalDot(canvas));
        canvas.Find(".diagram-canvas").Focus();
        Assert.Null(ProvisionalDot(canvas));
    }

    // Below the LOD cutoff the shape is a placeholder with no stop, so focus could never reach it
    // and nothing would ever end placement. Add port… is not offered there.
    [Fact]
    public async Task AddPortStartsNoPlacementWhenTheShapeHasNoStopToHoldFocus()
    {
        var canvas = RenderCanvas(snapToGrid: false);
        canvas.ClickOn(canvas.ContainerOf(_shape.Id));
        await canvas.InvokeAsync(() => canvas.Instance.ZoomPanTracker.Scale = 0.1);
        await canvas.InvokeAsync(() => canvas.Instance.OnContextMenuKeyPressed());

        Assert.DoesNotContain(AddPortByKeyboard, MenuLabels(canvas));
        await canvas.InvokeAsync(() => canvas.Instance.OnEscapePressed());
        var before = _shape.Bounds;
        await Arrow(canvas, "ArrowRight");

        Assert.NotEqual(before, _shape.Bounds);
    }

    public static TheoryData<string> IgnoredKeys =>
        new()
        {
            "Delete",
            "Space",
            "Undo",
            "Redo",
            "AltArrow",
            "CtrlArrow",
            "CtrlShiftArrow",
            "F2",
            "SelectAll",
            "Group",
            "BringToFront",
            "SendBackward",
            "Lock",
            "Duplicate",
            "Cut",
            "Paste",
            "MenuKey",
        };

    [Theory]
    [MemberData(nameof(IgnoredKeys))]
    public async Task EveryOtherBoardWritingKeyDoesNothingWhilePlacing(string key)
    {
        _shape.Bounds = new Bounds(100, 100, 200, 100);
        var canvas = RenderCanvas();
        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed("ArrowRight", false));
        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyReleased());
        await StartPlacement(canvas);
        var before = (
            _board.Components.Count,
            _board.Edges.Count,
            _shape.Bounds,
            _shape.ZIndex,
            _shape.Locked
        );

        await canvas.InvokeAsync(async () =>
        {
            var instance = canvas.Instance;
            switch (key)
            {
                case "Delete":
                    instance.OnDeletePressed();
                    break;
                case "Space":
                    instance.OnSpacePressed();
                    break;
                case "Undo":
                    instance.OnUndoPressed();
                    break;
                case "Redo":
                    instance.OnRedoPressed();
                    break;
                case "AltArrow":
                    instance.OnAltArrowKeyPressed("ArrowRight", false);
                    break;
                case "CtrlArrow":
                    instance.OnQuickCreatePressed("ArrowRight");
                    break;
                case "CtrlShiftArrow":
                    await instance.OnDirectionalFocusPressed("ArrowRight");
                    break;
                case "F2":
                    instance.OnBeginEditPressed();
                    break;
                case "SelectAll":
                    instance.OnSelectAllPressed();
                    break;
                case "Group":
                    instance.OnGroupPressed();
                    break;
                case "BringToFront":
                    instance.OnBringToFrontPressed();
                    break;
                case "SendBackward":
                    instance.OnSendBackwardPressed();
                    break;
                case "Lock":
                    instance.OnToggleLockPressed();
                    break;
                case "Duplicate":
                    instance.OnDuplicatePressed();
                    break;
                case "Cut":
                    Assert.Null(instance.OnCutRequested());
                    break;
                case "Paste":
                    instance.OnPasteReceived(instance.OnCopyRequested()!, null, null);
                    break;
                case "MenuKey":
                    instance.OnContextMenuKeyPressed();
                    break;
            }
        });

        Assert.Equal(
            before,
            (
                _board.Components.Count,
                _board.Edges.Count,
                _shape.Bounds,
                _shape.ZIndex,
                _shape.Locked
            )
        );
        Assert.Equal([_shape.Id], canvas.Instance.SelectedComponents.Select(i => i.Id));
        Assert.Empty(canvas.FindAll(".d12-context-menu"));
        Assert.NotNull(ProvisionalDot(canvas));
        await Enter(canvas);
        Assert.Single(_shape.CustomPorts);
    }

    [Fact]
    public async Task TheSnapChordDoesNothingWhilePlacing()
    {
        var canvas = RenderCanvas();
        await StartPlacement(canvas);

        await canvas.InvokeAsync(() => canvas.Instance.OnSnapToGridChordPressed());

        Assert.True(canvas.Instance.SnapToGrid);
        Assert.NotNull(ProvisionalDot(canvas));
    }

    [Fact]
    public async Task TheFramingCommandsLeaveTheViewportAloneWhilePlacing()
    {
        var canvas = RenderCanvas();
        await StartPlacement(canvas);
        var tracker = canvas.Instance.ZoomPanTracker;
        await canvas.InvokeAsync(() => tracker.SetScaleAbout(0, 0, 0.5));
        var before = (tracker.Scale, tracker.PanX, tracker.PanY);

        await canvas.InvokeAsync(canvas.Instance.ZoomToFit);
        await canvas.InvokeAsync(canvas.Instance.ZoomToSelection);
        await canvas.InvokeAsync(canvas.Instance.ZoomTo100Percent);

        Assert.Equal(before, (tracker.Scale, tracker.PanX, tracker.PanY));
        Assert.NotNull(ProvisionalDot(canvas));
    }

    [Fact]
    public async Task EnteringPlacementKeepsAnArmedConnectorSource()
    {
        var canvas = RenderCanvas();
        canvas.ContainerOf(_other.Id).Focus();
        await Enter(canvas);
        await Enter(canvas);
        FocusShape(canvas);

        await canvas.InvokeAsync(() => canvas.Instance.OnContextMenuKeyPressed());
        MenuRow(canvas, AddPortByKeyboard).Click();
        await Enter(canvas);
        var port = Assert.Single(_shape.CustomPorts);
        await Enter(canvas);
        for (var press = 0; press < 5; press++)
        {
            await canvas.InvokeAsync(() => canvas.Instance.OnSpacePressed());
        }
        await Enter(canvas);

        var edge = Assert.Single(_board.Edges);
        Assert.Equal(new AutoPortEndpoint(_other.Id), edge.Source);
        Assert.Equal(new CustomPortEndpoint(_shape.Id, port.Id), edge.Target);
    }
}
