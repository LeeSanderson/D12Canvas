using AngleSharp.Dom;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// Each shape type carries its own accessible name, so the ring reads as the list of names a
// screen reader would announce on each Tab.
public class DiagramCanvasEdgeTabStopTests : ComponentTestBase
{
    private readonly Board _board = new();

    public DiagramCanvasEdgeTabStopTests()
    {
        SetupDiagramCanvasJsModule();

        var registry = new ComponentRegistry();
        foreach (var name in new[] { "A", "B", "C" })
        {
            registry.Register(
                new ComponentRegistration(
                    Key: name,
                    ComponentType: typeof(TestPropsComponent),
                    PropsType: typeof(TestProps),
                    DisplayName: name,
                    AccessibleName: name,
                    DefaultProps: new TestProps(),
                    Icon: null,
                    Role: "group",
                    DefaultSize: null,
                    Category: null
                )
            );
        }
        Services.AddSingleton<IComponentRegistry>(registry);
    }

    private ComponentInstance AddShape(string name, double x, double y)
    {
        var instance = new ComponentInstance(name, new TestProps(), new Bounds(x, y, 50, 50));
        _board.AddComponent(instance);
        return instance;
    }

    private Edge Connect(ComponentInstance source, ComponentInstance target) =>
        AddEdge(
            new PortEndpoint(source.Id, PortId.Right),
            new PortEndpoint(target.Id, PortId.Left)
        );

    private Edge AddEdge(IEdgeEndpoint source, IEdgeEndpoint target)
    {
        var edge = new Edge(source, target);
        _board.AddEdge(edge);
        return edge;
    }

    private IRenderedComponent<DiagramCanvas> RenderCanvas() =>
        Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, _board));

    private static List<string?> Ring(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas
            .FindAll(".instance-layer [tabindex='0']")
            .Select(stop => stop.GetAttribute("aria-label"))
            .ToList();

    private static IElement EdgeStop(IRenderedComponent<DiagramCanvas> canvas, string label) =>
        canvas.Find($".edge-tab-stop[aria-label='{label}']");

    private static IElement Container(IRenderedComponent<DiagramCanvas> canvas, Guid id) =>
        canvas.Find($".component-container[data-d12-entity='{id}']");

    private static IElement EdgeLine(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.Find(".edge-line");

    [Fact]
    public void AnEdgeFollowsItsSourceWhateverItsPositionOnTheBoard()
    {
        var a = AddShape("A", 0, 200);
        var b = AddShape("B", 200, 0);
        var c = AddShape("C", 400, 100);
        Connect(a, b);
        Connect(b, c);

        var canvas = RenderCanvas();

        Assert.Equal(
            ["B", "Connector from B to C", "C", "A", "Connector from A to B"],
            Ring(canvas)
        );
    }

    [Fact]
    public void EdgesLeavingOneShapeFollowItInTheirTargetsReadingOrder()
    {
        var a = AddShape("A", 0, 0);
        var b = AddShape("B", 200, 100);
        var c = AddShape("C", 0, 300);
        Connect(a, c);
        Connect(a, b);

        var canvas = RenderCanvas();

        Assert.Equal(
            ["A", "Connector from A to B", "Connector from A to C", "B", "C"],
            Ring(canvas)
        );
    }

    [Fact]
    public void AnEdgeFromAGroupedMemberFollowsTheGroupsStopAndReadsTheGroupsLabel()
    {
        var a = AddShape("A", 0, 0);
        var b = AddShape("B", 100, 0);
        var c = AddShape("C", 0, 300);
        _board.AddGroup(new Group([a.Id, b.Id]));
        Connect(a, c);

        var canvas = RenderCanvas();

        Assert.Equal(["Group (2 items)", "Connector from Group (2 items) to C", "C"], Ring(canvas));
    }

    [Fact]
    public async Task EnteringAGroupRemovesEveryEdgeStopAndLeavingRestoresThem()
    {
        var a = AddShape("A", 0, 0);
        var b = AddShape("B", 100, 0);
        var c = AddShape("C", 0, 300);
        _board.AddGroup(new Group([a.Id, b.Id]));
        Connect(a, c);
        Connect(c, b);
        var canvas = RenderCanvas();

        canvas.DoubleClickElement(Container(canvas, a.Id), (10, 10));

        Assert.Empty(canvas.FindAll(".edge-tab-stop"));
        Assert.Equal(["A", "B", "C"], Ring(canvas));

        await canvas.InvokeAsync(() => canvas.Instance.OnEscapePressed());

        Assert.Equal(2, canvas.FindAll(".edge-tab-stop").Count);
    }

    [Fact]
    public void AnEdgeWithAFloatingSourceSortsAtItsSourcePointAndReadsUnattachedEnd()
    {
        AddShape("A", 0, 0);
        var c = AddShape("C", 0, 300);
        AddEdge(new FloatingEndpoint(10, 150), new PortEndpoint(c.Id, PortId.Top));

        var canvas = RenderCanvas();

        Assert.Equal(["A", "Connector from unattached end to C", "C"], Ring(canvas));
    }

    [Fact]
    public void AFloatingTargetReadsUnattachedEnd()
    {
        var a = AddShape("A", 0, 0);
        AddEdge(new PortEndpoint(a.Id, PortId.Right), new FloatingEndpoint(300, 300));

        var canvas = RenderCanvas();

        Assert.Equal(["A", "Connector from A to unattached end"], Ring(canvas));
    }

    [Fact]
    public async Task AnEdgeWhoseSourceIsOutsideTheViewportPlusOverscanHasNoStop()
    {
        var a = AddShape("A", 0, 0);
        var farAway = AddShape("B", 3000, 3000);
        AddEdge(new FloatingEndpoint(2500, 2500), new PortEndpoint(a.Id, PortId.Top));
        Connect(farAway, a);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, _board).Add(p => p.Overscan, 0)
        );
        canvas.ReturnToOrigin();
        await canvas.InvokeAsync(() => canvas.Instance.OnContainerResized(800, 600));

        Assert.Equal(["A"], Ring(canvas));
    }

    [Fact]
    public void AnEdgeWhoseSourceIsPlaceholderedKeepsItsStop()
    {
        var a = AddShape("A", 0, 0);
        var b = AddShape("B", 0, 300);
        Connect(a, b);

        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, _board).Add(p => p.LodSizeThreshold, 1000)
        );

        Assert.Single(canvas.FindAll(".edge-tab-stop[aria-label='Connector from A to B']"));
    }

    [Fact]
    public void AnEdgeWhoseEndCannotBeResolvedHasNoStop()
    {
        var a = AddShape("A", 0, 0);
        AddEdge(
            new PortEndpoint(a.Id, PortId.Right),
            new PortEndpoint(Guid.NewGuid(), PortId.Left)
        );

        var canvas = RenderCanvas();

        Assert.Empty(canvas.FindAll(".edge-tab-stop"));
    }

    [Fact]
    public void TheStopIsAProxyWithNoRoleSizedToTheUnionOfItsEnds()
    {
        var a = AddShape("A", 0, 0);
        var b = AddShape("B", 200, 100);
        Connect(a, b);

        var stop = RenderCanvas().Find(".edge-tab-stop");

        Assert.False(stop.HasAttribute("role"));
        Assert.Equal("0", stop.GetAttribute("tabindex"));
        Assert.Null(stop.GetAttribute("aria-selected"));
        Assert.Equal(
            "left: 50px; top: 25px; width: 150px; height: 100px;",
            stop.GetAttribute("style")
        );
    }

    [Fact]
    public void FocusingAnEdgeStopSelectsThatEdgeAlone()
    {
        var a = AddShape("A", 0, 0);
        var b = AddShape("B", 200, 100);
        Connect(a, b);
        var canvas = RenderCanvas();
        canvas.ClickOn(Container(canvas, a.Id));

        EdgeStop(canvas, "Connector from A to B").Focus();

        Assert.Equal(
            "true",
            EdgeStop(canvas, "Connector from A to B").GetAttribute("aria-selected")
        );
        Assert.Equal("true", EdgeLine(canvas).GetAttribute("aria-selected"));
        Assert.Null(Container(canvas, a.Id).GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task SpaceOnAFocusedEdgeStopTogglesItIntoAnInstanceSelection()
    {
        var a = AddShape("A", 0, 0);
        var b = AddShape("B", 200, 100);
        Connect(a, b);
        var canvas = RenderCanvas();
        Container(canvas, a.Id).Focus();
        await canvas.InvokeAsync(() => canvas.Instance.OnSpacePressed());
        EdgeStop(canvas, "Connector from A to B").Focus();

        await canvas.InvokeAsync(() => canvas.Instance.OnSpacePressed());

        Assert.Equal("true", Container(canvas, a.Id).GetAttribute("aria-selected"));
        Assert.Equal("true", EdgeLine(canvas).GetAttribute("aria-selected"));

        await canvas.InvokeAsync(() => canvas.Instance.OnSpacePressed());

        Assert.Equal("true", Container(canvas, a.Id).GetAttribute("aria-selected"));
        Assert.Null(EdgeLine(canvas).GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task AFocusHandoffCountsEdgeStopsAtTheirRenderedPositions()
    {
        var a = AddShape("A", 0, 0);
        var b = AddShape("B", 200, 100);
        var c = AddShape("C", 300, 100);
        _board.AddGroup(new Group([b.Id, c.Id]));
        Connect(a, b);
        var canvas = RenderCanvas();
        canvas.Find(".group-tab-stop").Focus();
        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed());

        await canvas.InvokeAsync(() => canvas.Instance.OnEscapePressed());

        var handoffsAfterEnteringTheGroup = JSInterop.Invocations["focusTabStopAt"].Skip(1);
        var invocation = Assert.Single(handoffsAfterEnteringTheGroup);
        Assert.Equal(2, invocation.Arguments[1]);
        Assert.Equal(["A", "Connector from A to Group (2 items)", "Group (2 items)"], Ring(canvas));
    }

    [Fact]
    public async Task EnterOnAnEdgeStopDoesNothing()
    {
        var a = AddShape("A", 0, 0);
        var b = AddShape("B", 200, 100);
        Connect(a, b);
        var canvas = RenderCanvas();
        EdgeStop(canvas, "Connector from A to B").Focus();

        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed());

        Assert.Empty(canvas.FindAll(".port.port-focused"));
        Assert.Equal("true", EdgeLine(canvas).GetAttribute("aria-selected"));
    }
}
