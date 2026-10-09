using AngleSharp.Dom;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// The move lands through focusTabStopAt, which the browser turns into a real focus on the stop at
// that index. These tests read the index back against the rendered stops and focus that stop the
// way the browser would. DirectionalFocusProbes shows the real keys arriving.
public class DiagramCanvasDirectionalFocusTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    private readonly Board _board = new();
    private readonly ComponentInstance _origin;
    private readonly ComponentInstance _sameRowFarther;
    private readonly ComponentInstance _offRowNearer;
    private readonly ComponentInstance _below;

    public DiagramCanvasDirectionalFocusTests()
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

        _origin = AddInstance(0, 0);
        _sameRowFarther = AddInstance(300, 0);
        _offRowNearer = AddInstance(100, 60);
        _below = AddInstance(0, 200);
    }

    private ComponentInstance AddInstance(double x, double y)
    {
        var instance = new ComponentInstance(
            ComponentTypeKey,
            new TestProps(),
            new Bounds(x, y, 50, 50)
        );
        _board.AddComponent(instance);
        return instance;
    }

    private Edge AddEdge(ComponentInstance source, ComponentInstance target)
    {
        var edge = new Edge(
            new PortEndpoint(source.Id, PortId.Right),
            new PortEndpoint(target.Id, PortId.Left)
        );
        _board.AddEdge(edge);
        return edge;
    }

    private IRenderedComponent<DiagramCanvas> RenderCanvas() =>
        Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, _board)).ReturnToOrigin();

    private static IElement ContainerOf(IRenderedComponent<DiagramCanvas> canvas, Guid id) =>
        canvas.Find($".component-container[data-d12-entity='{id}']");

    private static void Focus(
        IRenderedComponent<DiagramCanvas> canvas,
        ComponentInstance instance
    ) => ContainerOf(canvas, instance.Id).Focus();

    private static HashSet<Guid> Selected(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.Instance.SelectedComponents.Select(instance => instance.Id).ToHashSet();

    private static Task Press(IRenderedComponent<DiagramCanvas> canvas, string code) =>
        canvas.InvokeAsync(() => canvas.Instance.OnDirectionalFocusPressed(code));

    private int FocusMoves => JSInterop.Invocations["focusTabStopAt"].Count;

    private IElement LandedStop(IRenderedComponent<DiagramCanvas> canvas)
    {
        var index = (int)JSInterop.Invocations["focusTabStopAt"].Last().Arguments[1]!;
        return canvas.FindAll(".instance-layer [tabindex=\"0\"]")[index];
    }

    private IElement Land(IRenderedComponent<DiagramCanvas> canvas)
    {
        var stop = LandedStop(canvas);
        stop.Focus();
        return stop;
    }

    private static string? EntityOf(IElement stop) => stop.GetAttribute("data-d12-entity");

    [Fact]
    public async Task RightFromAFocusedShapeLandsOnTheNearestInItsRowAndSelectsIt()
    {
        var canvas = RenderCanvas();
        Focus(canvas, _origin);

        await Press(canvas, "ArrowRight");

        Assert.Equal(_sameRowFarther.Id.ToString(), EntityOf(Land(canvas)));
        Assert.Equal([_sameRowFarther.Id], Selected(canvas));
    }

    [Fact]
    public async Task OffTheRowTheNearestShapeWins()
    {
        var canvas = RenderCanvas();
        Focus(canvas, _below);

        await Press(canvas, "ArrowRight");

        Assert.Equal(_offRowNearer.Id.ToString(), EntityOf(LandedStop(canvas)));
    }

    [Fact]
    public async Task NothingInThatDirectionLeavesFocusWhereItIs()
    {
        var canvas = RenderCanvas();
        Focus(canvas, _origin);

        await Press(canvas, "ArrowLeft");

        Assert.Equal(0, FocusMoves);
        Assert.Equal([_origin.Id], Selected(canvas));
    }

    [Fact]
    public async Task AnEdgeStopIsNeverATarget()
    {
        AddEdge(_origin, _sameRowFarther);
        var canvas = RenderCanvas();
        Focus(canvas, _origin);

        await Press(canvas, "ArrowRight");

        Assert.Equal(_sameRowFarther.Id.ToString(), EntityOf(LandedStop(canvas)));
    }

    [Fact]
    public async Task AFocusedEdgeMeasuresFromItsSourcesStop()
    {
        AddEdge(_origin, _sameRowFarther);
        var canvas = RenderCanvas();
        canvas.Find(".edge-tab-stop").Focus();

        await Press(canvas, "ArrowDown");

        Assert.Equal(_below.Id.ToString(), EntityOf(LandedStop(canvas)));
    }

    [Fact]
    public async Task WithNothingFocusedTheMoveMeasuresFromTheSelection()
    {
        var canvas = RenderCanvas();
        canvas.ClickElement(ContainerOf(canvas, _below.Id), at: (10, 10));
        canvas.Find(".diagram-canvas").Focus();

        await Press(canvas, "ArrowUp");

        Assert.Equal(_origin.Id.ToString(), EntityOf(Land(canvas)));
        Assert.Equal([_origin.Id], Selected(canvas));
    }

    [Fact]
    public async Task WithASelectedEdgeTheMoveMeasuresFromItsSource()
    {
        var edge = AddEdge(_origin, _sameRowFarther);
        var canvas = RenderCanvas();
        canvas.Find(".edge-tab-stop").Focus();
        canvas.Find(".diagram-canvas").Focus();

        await Press(canvas, "ArrowDown");

        Assert.Equal([edge.Id], canvas.Instance.SelectedEdges.Select(selected => selected.Id));
        Assert.Equal(_below.Id.ToString(), EntityOf(LandedStop(canvas)));
    }

    [Fact]
    public async Task WithNothingSelectedTheMoveMeasuresFromTheViewportCentre()
    {
        var canvas = RenderCanvas();

        await Press(canvas, "ArrowUp");

        Assert.Equal(_sameRowFarther.Id.ToString(), EntityOf(LandedStop(canvas)));
    }

    [Fact]
    public async Task InsideAdditiveTraversalTheMoveChangesFocusAndNotTheSelectionAndTheModeSurvives()
    {
        var canvas = RenderCanvas();
        Focus(canvas, _origin);
        await canvas.InvokeAsync(() => canvas.Instance.OnSpacePressed());

        await Press(canvas, "ArrowRight");
        Land(canvas);

        Assert.Equal([_origin.Id], Selected(canvas));

        Focus(canvas, _below);

        Assert.Equal([_origin.Id], Selected(canvas));
    }

    [Fact]
    public async Task AGroupStopIsATarget()
    {
        _board.AddGroup(new Group([_sameRowFarther.Id, _offRowNearer.Id]));
        var canvas = RenderCanvas();
        Focus(canvas, _origin);

        await Press(canvas, "ArrowRight");

        Assert.Contains("group-tab-stop", Land(canvas).ClassList);
        Assert.Equal("true", canvas.Find(".group-tab-stop").GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task InsideAnEnteredGroupOnlyItsMembersAreCandidates()
    {
        _board.AddGroup(new Group([_origin.Id, _offRowNearer.Id]));
        var canvas = RenderCanvas();
        canvas.Find(".group-tab-stop").Focus();
        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed());
        Focus(canvas, _origin);
        var movesBefore = FocusMoves;

        await Press(canvas, "ArrowRight");

        Assert.Equal(movesBefore + 1, FocusMoves);
        Assert.Equal(_offRowNearer.Id.ToString(), EntityOf(LandedStop(canvas)));
    }

    [Fact]
    public async Task APressThatOwnsTheBoardMakesItANoOp()
    {
        var canvas = RenderCanvas();
        Focus(canvas, _origin);
        await canvas.Press(400, 400);

        await Press(canvas, "ArrowRight");

        Assert.Equal(0, FocusMoves);

        await canvas.Release(400, 400);
        Focus(canvas, _origin);
        await Press(canvas, "ArrowRight");

        Assert.Equal(1, FocusMoves);
    }

    [Fact]
    public async Task FocusLeavingTheContainerDropsTheFocusedStopAsTheOrigin()
    {
        var canvas = RenderCanvas();
        Focus(canvas, _below);
        await canvas.InvokeAsync(() => canvas.Instance.OnFocusLeftContainer());
        await canvas.InvokeAsync(() => canvas.Instance.OnEscapePressed());

        await Press(canvas, "ArrowUp");

        Assert.Equal(_sameRowFarther.Id.ToString(), EntityOf(LandedStop(canvas)));
    }

    [Fact]
    public async Task PortPickingMakesItANoOp()
    {
        var canvas = RenderCanvas();
        Focus(canvas, _origin);
        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed());

        await Press(canvas, "ArrowRight");

        Assert.Equal(0, FocusMoves);
        Assert.NotEmpty(canvas.FindAll(".port-focused"));
    }
}
