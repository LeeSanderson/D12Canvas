using AngleSharp.Dom;
using Bunit;
using D12Canvas.BuiltIns;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

public class DiagramCanvasQuickCreateTests : ComponentTestBase
{
    private const double Gap = 2 * DiagramCanvas.GridBaseSpacing;

    private readonly Board _board = new();

    public DiagramCanvasQuickCreateTests()
    {
        SetupDiagramCanvasJsModule();
        var options = new D12CanvasOptions();
        BuiltInComponents.RegisterAll(options);
        Services.AddSingleton(options.Registry);
    }

    private ComponentInstance AddRectangle(double x, double y)
    {
        var instance = new ComponentInstance(
            "rectangle",
            new RectangleProps("#123456", null, 3),
            new Bounds(x, y, 160, 100)
        );
        _board.AddComponent(instance);
        return instance;
    }

    private ComponentInstance AddText(string text, double x, double y)
    {
        var instance = new ComponentInstance(
            "text",
            new TextProps(text, null, 16, "normal", "left"),
            new Bounds(x, y, 200, 40)
        );
        _board.AddComponent(instance);
        return instance;
    }

    private IRenderedComponent<DiagramCanvas> RenderCanvas(bool snapToGrid = false) =>
        Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, _board).Add(p => p.SnapToGrid, snapToGrid)
        );

    private static Task CtrlArrow(IRenderedComponent<DiagramCanvas> canvas, string code) =>
        canvas.InvokeAsync(() => canvas.Instance.OnQuickCreatePressed(code));

    private static Task Undo(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

    private static Task Redo(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.InvokeAsync(() => canvas.Instance.OnRedoPressed());

    private ComponentInstance CopyOf(ComponentInstance source) =>
        Assert.Single(_board.Components, instance => instance.Id != source.Id);

    private IElement LastFocusedStop(IRenderedComponent<DiagramCanvas> canvas)
    {
        var index = (int)CanvasModule.Invocations["focusTabStopAt"].Last().Arguments[1]!;
        return canvas.FindAll(".instance-layer [tabindex=\"0\"]")[index];
    }

    private int SelectAllCalls =>
        JSInterop.Invocations.Count(invocation => invocation.Identifier == "focusAndSelectAll");

    [Fact]
    public async Task ClickingTheRightPortMakesAConnectedSelectedFocusedCopyToTheRightInOneEntry()
    {
        var source = AddRectangle(100, 100);
        var canvas = RenderCanvas();

        canvas.ClickElement(canvas.SelectedPortSpan(source.Id, "Right"), at: (260, 150));

        var copy = CopyOf(source);
        Assert.Equal(new Bounds(source.Bounds.Right + Gap, 100, 160, 100), copy.Bounds);
        Assert.Equal(source.ComponentTypeKey, copy.ComponentTypeKey);
        Assert.Equal(source.Props, copy.Props);
        var edge = Assert.Single(_board.Edges);
        Assert.Equal(new PortEndpoint(source.Id, PortId.Right), edge.Source);
        Assert.Equal(new AutoPortEndpoint(copy.Id), edge.Target);
        Assert.Equal([copy], canvas.Instance.SelectedComponents);
        Assert.Empty(canvas.Instance.SelectedEdges);
        Assert.Equal(copy.Id.ToString(), LastFocusedStop(canvas).GetAttribute("data-d12-entity"));
        Assert.Equal(0, SelectAllCalls);

        await Undo(canvas);

        Assert.Same(source, Assert.Single(_board.Components));
        Assert.Empty(_board.Edges);

        await Redo(canvas);

        Assert.Equal(2, _board.Components.Count);
        Assert.Single(_board.Edges);
    }

    public static TheoryData<string, PortId, double, double> Arrows =>
        new()
        {
            { "ArrowRight", PortId.Right, 260 + Gap, 100 },
            { "ArrowDown", PortId.Bottom, 100, 200 + Gap },
            { "ArrowLeft", PortId.Left, 100 - Gap - 160, 100 },
            { "ArrowUp", PortId.Top, 100, 100 - Gap - 100 },
        };

    [Theory]
    [MemberData(nameof(Arrows))]
    public async Task CtrlArrowOnAFocusedShapeQuickCreatesInTheArrowsDirection(
        string code,
        PortId side,
        double expectedX,
        double expectedY
    )
    {
        var source = AddRectangle(100, 100);
        var canvas = RenderCanvas();
        canvas.ContainerOf(source.Id).Focus();

        await CtrlArrow(canvas, code);

        var copy = CopyOf(source);
        Assert.Equal(new Bounds(expectedX, expectedY, 160, 100), copy.Bounds);
        var edge = Assert.Single(_board.Edges);
        Assert.Equal(new PortEndpoint(source.Id, side), edge.Source);
        Assert.Equal(new AutoPortEndpoint(copy.Id), edge.Target);
        Assert.Equal([copy], canvas.Instance.SelectedComponents);
        Assert.Equal(copy.Id.ToString(), LastFocusedStop(canvas).GetAttribute("data-d12-entity"));
    }

    [Fact]
    public async Task ASecondQuickCreateFromTheNewShapeStepsPastTheOccupiedSlot()
    {
        var first = AddRectangle(100, 100);
        var canvas = RenderCanvas();
        canvas.ContainerOf(first.Id).Focus();

        await CtrlArrow(canvas, "ArrowRight");
        var second = CopyOf(first);
        await CtrlArrow(canvas, "ArrowLeft");

        var third = Assert.Single(
            _board.Components,
            instance => instance.Id != first.Id && instance.Id != second.Id
        );
        Assert.Equal(first.Bounds.Y, third.Bounds.Y);
        Assert.True(third.Bounds.Right < first.Bounds.X);
        Assert.Equal(0, (first.Bounds.X - third.Bounds.X) % Gap);
        Assert.Equal(2, _board.Edges.Count);
    }

    [Fact]
    public async Task AQuickCreatedTextOpensForTypingWithItsLabelSelected()
    {
        var source = AddText("Login", 100, 100);
        var canvas = RenderCanvas();
        canvas.ContainerOf(source.Id).Focus();

        await CtrlArrow(canvas, "ArrowDown");

        var copy = CopyOf(source);
        var editor = canvas.Find("textarea.d12-text-editor");
        Assert.Equal("Login", editor.GetAttribute("value"));
        Assert.Equal(
            copy.Id.ToString(),
            editor.Closest("[data-d12-entity]")!.GetAttribute("data-d12-entity")
        );
        Assert.Equal(1, SelectAllCalls);
    }

    [Fact]
    public async Task AnAbandonedQuickCreatedTextLeavesNoEntryAndHandsFocusBackToTheSource()
    {
        var moved = AddRectangle(400, 400);
        var source = AddText("Login", 100, 100);
        var canvas = RenderCanvas();
        canvas.ClickOn(canvas.ContainerOf(moved.Id));
        canvas.DragOn(canvas.ContainerOf(moved.Id), (450, 450), (480, 470));
        var movedBounds = moved.Bounds;
        canvas.ContainerOf(source.Id).Focus();

        await CtrlArrow(canvas, "ArrowRight");
        canvas.Find("textarea.d12-text-editor").Input("");
        canvas.Find("textarea.d12-text-editor").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Equal(2, _board.Components.Count);
        Assert.Empty(_board.Edges);
        Assert.Equal([source], canvas.Instance.SelectedComponents);
        Assert.Equal(source.Id.ToString(), LastFocusedStop(canvas).GetAttribute("data-d12-entity"));
        Assert.Empty(CanvasModule.Invocations["focusCanvas"]);

        await Undo(canvas);
        Assert.NotEqual(movedBounds, moved.Bounds);
        await Redo(canvas);
        Assert.Equal(movedBounds, moved.Bounds);
        await Redo(canvas);
        Assert.Equal(2, _board.Components.Count);
    }

    [Fact]
    public async Task AQuickCreatedTextKeptAfterAnotherCommandIsRemovedWithItsOwnEntryInstead()
    {
        var source = AddText("Login", 100, 100);
        var canvas = RenderCanvas();
        canvas.ContainerOf(source.Id).Focus();

        await CtrlArrow(canvas, "ArrowRight");
        canvas.Find("textarea.d12-text-editor").Input("Next");
        canvas.Find("textarea.d12-text-editor").Blur();

        Assert.Equal("Next", ((TextProps)CopyOf(source).Props).Text);
        await Undo(canvas);
        Assert.Equal("Login", ((TextProps)CopyOf(source).Props).Text);
        await Undo(canvas);
        Assert.Same(source, Assert.Single(_board.Components));
    }

    [Fact]
    public void APortPressThatCrossesTheThresholdIsAnOrdinaryConnectorDrag()
    {
        var source = AddRectangle(100, 100);
        var canvas = RenderCanvas();

        canvas.DragConnector(canvas.SelectedPortSpan(source.Id, "Right"), (260, 150), (600, 500));

        Assert.Same(source, Assert.Single(_board.Components));
        var edge = Assert.Single(_board.Edges);
        Assert.Equal(new FloatingEndpoint(600, 500), edge.Target);
    }

    [Fact]
    public async Task EscapeDuringAPortPressMakesTheReleaseCreateNothing()
    {
        var source = AddRectangle(100, 100);
        var canvas = RenderCanvas();

        canvas.PressElement(canvas.SelectedPortSpan(source.Id, "Right"), (260, 150));
        await canvas.InvokeAsync(() => canvas.Instance.OnEscapePressed());
        canvas.ReleaseAt((260, 150));

        Assert.Same(source, Assert.Single(_board.Components));
        Assert.Empty(_board.Edges);
    }

    [Fact]
    public void ClickingACustomPortQuickCreatesOnItsSideWithFreshPortIds()
    {
        var source = AddRectangle(100, 100);
        var port = new PortDef(0.8, 1.0);
        source.CustomPorts.Add(port);
        var canvas = RenderCanvas();
        canvas.ClickOn(canvas.ContainerOf(source.Id));

        canvas.ClickElement(canvas.PortSpanOf(source.Id, port.Id.ToString()), at: (228, 200));

        var copy = CopyOf(source);
        Assert.Equal(new Bounds(100, 200 + Gap, 160, 100), copy.Bounds);
        Assert.Equal(
            new CustomPortEndpoint(source.Id, port.Id),
            Assert.Single(_board.Edges).Source
        );
        var copiedPort = Assert.Single(copy.CustomPorts);
        Assert.NotEqual(port.Id, copiedPort.Id);
        Assert.Equal(
            (port.FractionX, port.FractionY),
            (copiedPort.FractionX, copiedPort.FractionY)
        );
    }

    [Fact]
    public async Task QuickCreateStartsNoDuplicateRun()
    {
        var source = AddRectangle(100, 100);
        var canvas = RenderCanvas();
        canvas.ContainerOf(source.Id).Focus();

        await CtrlArrow(canvas, "ArrowRight");
        var copy = CopyOf(source);
        await canvas.InvokeAsync(() => canvas.Instance.OnDuplicatePressed());

        var duplicate = Assert.Single(
            _board.Components,
            instance => instance.Id != source.Id && instance.Id != copy.Id
        );
        Assert.Equal(
            copy.Bounds with
            {
                X = copy.Bounds.X + 20,
                Y = copy.Bounds.Y + 20,
            },
            duplicate.Bounds
        );
    }

    [Fact]
    public async Task TheGapIsTwoCellsOfTheDominantGridAtTheCurrentZoom()
    {
        const double dominantSpacingAtThreeTenths = 10 * DiagramCanvas.GridBaseSpacing;
        var source = AddRectangle(100, 100);
        var canvas = RenderCanvas();
        await canvas.InvokeAsync(() => canvas.Instance.ZoomPanTracker.Scale = 0.3);
        canvas.ContainerOf(source.Id).Focus();

        await CtrlArrow(canvas, "ArrowRight");

        Assert.Equal(
            source.Bounds.Right + 2 * dominantSpacingAtThreeTenths,
            CopyOf(source).Bounds.X
        );
    }

    [Fact]
    public async Task UnderSnapTheCopyOfAnOffGridShapeLandsOnTheGrid()
    {
        var source = AddRectangle(103, 97);
        var canvas = RenderCanvas(snapToGrid: true);
        canvas.ContainerOf(source.Id).Focus();

        await CtrlArrow(canvas, "ArrowRight");

        var copy = CopyOf(source);
        Assert.Equal(0, copy.Bounds.X % DiagramCanvas.GridBaseSpacing);
        Assert.Equal(0, copy.Bounds.Y % DiagramCanvas.GridBaseSpacing);
    }

    [Fact]
    public async Task CtrlArrowDoesNothingOnALockedShapeAMultiSelectionOrNoSelection()
    {
        var locked = AddRectangle(100, 100);
        locked.Locked = true;
        var first = AddRectangle(400, 100);
        var second = AddRectangle(400, 400);
        var canvas = RenderCanvas();

        await CtrlArrow(canvas, "ArrowRight");
        canvas.ContainerOf(locked.Id).Focus();
        await CtrlArrow(canvas, "ArrowRight");
        canvas.ClickOn(canvas.ContainerOf(first.Id));
        canvas.ClickOn(canvas.ContainerOf(second.Id), shift: true);
        await CtrlArrow(canvas, "ArrowRight");

        Assert.Equal(2, canvas.Instance.SelectedComponents.Count);
        Assert.Equal(3, _board.Components.Count);
        Assert.Empty(_board.Edges);
    }

    [Fact]
    public async Task CtrlArrowWhilePickingAPortDoesNothing()
    {
        var source = AddRectangle(100, 100);
        var canvas = RenderCanvas();
        canvas.ContainerOf(source.Id).Focus();
        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed());

        await CtrlArrow(canvas, "ArrowRight");

        Assert.Same(source, Assert.Single(_board.Components));
    }
}
