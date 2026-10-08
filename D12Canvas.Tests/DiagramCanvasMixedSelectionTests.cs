using AngleSharp.Dom;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// Edges and shapes share one selection. Shift+click on an edge toggles it, a marquee takes an edge
// by closure over what it selected, Ctrl+A takes every top-level entity and every edge, a move
// carries a selected edge's floating ends, and a command that cannot act on an edge skips it.
public class DiagramCanvasMixedSelectionTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasMixedSelectionTests()
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

    private static Edge AddEdge(Board board, IEdgeEndpoint source, IEdgeEndpoint target)
    {
        var edge = new Edge(source, target);
        board.AddEdge(edge);
        return edge;
    }

    private static Edge Connect(Board board, ComponentInstance source, ComponentInstance target) =>
        AddEdge(
            board,
            new PortEndpoint(source.Id, PortId.Right),
            new PortEndpoint(target.Id, PortId.Left)
        );

    private static IElement ContainerOf(
        IRenderedComponent<DiagramCanvas> canvas,
        ComponentInstance instance
    ) => canvas.Find($".component-container[data-d12-entity='{instance.Id}']");

    private static IElement HitOf(IRenderedComponent<DiagramCanvas> canvas, Edge edge) =>
        canvas.Find($".edge-hit[data-d12-entity='{edge.Id}']");

    private static IReadOnlySet<Guid> SelectedEdgeIds(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.Instance.SelectedEdges.Select(edge => edge.Id).ToHashSet();

    private static IReadOnlySet<Guid> SelectedInstanceIds(
        IRenderedComponent<DiagramCanvas> canvas
    ) => canvas.Instance.SelectedComponents.Select(instance => instance.Id).ToHashSet();

    [Fact]
    public void ShiftClickOnAnEdgeAddsItToASelectionOfShapesAndAgainRemovesIt()
    {
        var board = new Board();
        var first = AddInstance(board, 100, 100);
        var second = AddInstance(board, 250, 100);
        var edge = Connect(board, first, second);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ClickOn(ContainerOf(canvas, first));
        canvas.ClickOn(ContainerOf(canvas, second), shift: true);

        canvas.ClickElement(HitOf(canvas, edge), shift: true);

        Assert.Equal(new HashSet<Guid> { first.Id, second.Id }, SelectedInstanceIds(canvas));
        Assert.Equal(new HashSet<Guid> { edge.Id }, SelectedEdgeIds(canvas));
        Assert.Equal("true", canvas.Find(".edge-line").GetAttribute("aria-selected"));

        canvas.ClickElement(HitOf(canvas, edge), shift: true);

        Assert.Equal(new HashSet<Guid> { first.Id, second.Id }, SelectedInstanceIds(canvas));
        Assert.Empty(canvas.Instance.SelectedEdges);
        Assert.Null(canvas.Find(".edge-line").GetAttribute("aria-selected"));
    }

    [Fact]
    public void ShiftClickOnAShapeKeepsTheSelectedEdges()
    {
        var board = new Board();
        var first = AddInstance(board, 100, 100);
        var second = AddInstance(board, 250, 100);
        var edge = Connect(board, first, second);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ClickElement(HitOf(canvas, edge));

        canvas.ClickOn(ContainerOf(canvas, first), shift: true);

        Assert.Equal(new HashSet<Guid> { first.Id }, SelectedInstanceIds(canvas));
        Assert.Equal(new HashSet<Guid> { edge.Id }, SelectedEdgeIds(canvas));
    }

    [Fact]
    public async Task AMarqueeTakesTheEdgeBetweenTheShapesItSelectsAndNeverOneThatOnlyPassesBehind()
    {
        var board = new Board();
        var first = AddInstance(board, 100, 100);
        var second = AddInstance(board, 250, 100);
        var interior = Connect(board, first, second);
        var farLeft = AddInstance(board, 0, 200);
        var farRight = AddInstance(board, 400, 0);
        var passingBehind = Connect(board, farLeft, farRight);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        await canvas.Marquee((90, 90), (310, 160));

        Assert.Equal(new HashSet<Guid> { first.Id, second.Id }, SelectedInstanceIds(canvas));
        Assert.Equal(new HashSet<Guid> { interior.Id }, SelectedEdgeIds(canvas));

        await canvas.InvokeAsync(() => canvas.Instance.OnDeletePressed());

        Assert.Equal(
            new HashSet<Guid> { farLeft.Id, farRight.Id },
            board.Components.Select(c => c.Id).ToHashSet()
        );
        Assert.Equal(passingBehind, Assert.Single(board.Edges));
    }

    [Fact]
    public async Task AMarqueeTakesAnEdgeWhoseFloatingEndsLieInsideTheBand()
    {
        var board = new Board();
        var shape = AddInstance(board, 100, 100);
        var floatingInside = AddEdge(
            board,
            new FloatingEndpoint(120, 200),
            new FloatingEndpoint(200, 200)
        );
        var attachedThenOutside = AddEdge(
            board,
            new PortEndpoint(shape.Id, PortId.Bottom),
            new FloatingEndpoint(125, 400)
        );
        var attachedThenInside = AddEdge(
            board,
            new PortEndpoint(shape.Id, PortId.Right),
            new FloatingEndpoint(220, 120)
        );
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        await canvas.Marquee((90, 90), (250, 250));

        Assert.Equal(
            new HashSet<Guid> { floatingInside.Id, attachedThenInside.Id },
            SelectedEdgeIds(canvas)
        );
        Assert.DoesNotContain(attachedThenOutside.Id, SelectedEdgeIds(canvas));
    }

    [Fact]
    public async Task AnEdgeAttachedToAGroupMemberIsClosedOverWhenTheBandTakesTheGroup()
    {
        var board = new Board();
        var swept = AddInstance(board, 100, 100);
        var unswept = AddInstance(board, 500, 100);
        var outside = AddInstance(board, 500, 400);
        board.AddGroup(new Group([swept.Id, unswept.Id]));
        var withinGroup = Connect(board, swept, unswept);
        var leavingGroup = Connect(board, unswept, outside);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        await canvas.Marquee((90, 90), (160, 160));

        Assert.Equal(new HashSet<Guid> { withinGroup.Id }, SelectedEdgeIds(canvas));
        Assert.DoesNotContain(leavingGroup.Id, SelectedEdgeIds(canvas));
    }

    [Fact]
    public async Task AShiftMarqueeUnionsItsEdgesIntoThePressTimeSelection()
    {
        var board = new Board();
        var first = AddInstance(board, 100, 100);
        var second = AddInstance(board, 250, 100);
        var edge = Connect(board, first, second);
        var elsewhere = AddEdge(
            board,
            new FloatingEndpoint(600, 600),
            new FloatingEndpoint(700, 600)
        );
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ClickElement(HitOf(canvas, elsewhere));

        await canvas.Marquee((90, 90), (310, 160), shift: true);

        Assert.Equal(new HashSet<Guid> { edge.Id, elsewhere.Id }, SelectedEdgeIds(canvas));
    }

    [Fact]
    public async Task CancellingAMarqueeRestoresTheSelectedEdges()
    {
        var board = new Board();
        var first = AddInstance(board, 100, 100);
        var second = AddInstance(board, 250, 100);
        var edge = Connect(board, first, second);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ClickElement(HitOf(canvas, edge));

        await canvas.Press(600, 600);
        await canvas.Move(700, 700);
        Assert.Empty(canvas.Instance.SelectedEdges);
        await canvas.InvokeAsync(() => canvas.Instance.OnEscapePressed());

        Assert.Equal(new HashSet<Guid> { edge.Id }, SelectedEdgeIds(canvas));
    }

    [Fact]
    public async Task SelectAllTakesEveryTopLevelEntityAndEveryEdgeAndDeleteThenClearsTheBoardInOneEntry()
    {
        var board = new Board();
        var first = AddInstance(board, 100, 100);
        var second = AddInstance(board, 250, 100);
        var loose = AddInstance(board, 400, 300);
        var group = new Group([first.Id, second.Id]);
        board.AddGroup(group);
        var connected = Connect(board, first, second);
        var floating = AddEdge(board, new FloatingEndpoint(0, 0), new FloatingEndpoint(40, 0));
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        await canvas.InvokeAsync(() => canvas.Instance.OnSelectAllPressed());

        Assert.Equal(
            new HashSet<Guid> { first.Id, second.Id, loose.Id },
            SelectedInstanceIds(canvas)
        );
        Assert.Equal(new HashSet<Guid> { connected.Id, floating.Id }, SelectedEdgeIds(canvas));
        Assert.Equal("true", canvas.Find(".group-tab-stop").GetAttribute("aria-selected"));

        await canvas.InvokeAsync(() => canvas.Instance.OnDeletePressed());

        Assert.Empty(board.Components);
        Assert.Empty(board.Edges);
        Assert.Empty(board.Groups);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal(3, board.Components.Count);
        Assert.Equal(2, board.Edges.Count);
        Assert.Single(board.Groups);
    }

    [Fact]
    public async Task SelectAllDoesNothingWhileAPointerGestureOwnsThePress()
    {
        var board = new Board();
        AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        await canvas.Press(600, 600, button: Pointer.PointerPress.MiddleButton);
        await canvas.InvokeAsync(() => canvas.Instance.OnSelectAllPressed());

        Assert.Empty(canvas.Instance.SelectedComponents);
    }

    [Fact]
    public async Task MovingAMixedSelectionCarriesFloatingEndsLiveAndCommitsThemWithTheShapesInOneEntry()
    {
        var board = new Board();
        var shape = AddInstance(board, 100, 100);
        var halfFloating = AddEdge(
            board,
            new PortEndpoint(shape.Id, PortId.Right),
            new FloatingEndpoint(300, 300)
        );
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ClickOn(ContainerOf(canvas, shape));
        canvas.ClickElement(HitOf(canvas, halfFloating), shift: true);

        canvas.PressOn(ContainerOf(canvas, shape), (125, 125));
        canvas.MoveTo((145, 135));

        var marker = canvas.Find(".floating-endpoint");
        Assert.Equal("320", marker.GetAttribute("cx"));
        Assert.Equal("310", marker.GetAttribute("cy"));
        var line = canvas.Find(".edge-line");
        Assert.Equal("170", line.GetAttribute("x1"));
        Assert.Equal("320", line.GetAttribute("x2"));
        Assert.Equal(new FloatingEndpoint(300, 300), halfFloating.Target);

        canvas.ReleaseAt((145, 135));

        Assert.Equal(new Bounds(120, 110, 50, 50), shape.Bounds);
        Assert.Equal(new PortEndpoint(shape.Id, PortId.Right), halfFloating.Source);
        Assert.Equal(new FloatingEndpoint(320, 310), halfFloating.Target);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal(new Bounds(100, 100, 50, 50), shape.Bounds);
        Assert.Equal(new FloatingEndpoint(300, 300), halfFloating.Target);
    }

    [Fact]
    public void AnEdgeWithBothEndsFloatingMovesWhole()
    {
        var board = new Board();
        var shape = AddInstance(board, 100, 100);
        var connector = AddEdge(
            board,
            new FloatingEndpoint(300, 300),
            new FloatingEndpoint(380, 300)
        );
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ClickOn(ContainerOf(canvas, shape));
        canvas.ClickElement(HitOf(canvas, connector), shift: true);

        canvas.DragOn(ContainerOf(canvas, shape), (125, 125), (135, 145));

        Assert.Equal(new FloatingEndpoint(310, 320), connector.Source);
        Assert.Equal(new FloatingEndpoint(390, 320), connector.Target);
    }

    [Fact]
    public async Task CancellingAMoveLeavesTheFloatingEndsWhereTheyWere()
    {
        var board = new Board();
        var shape = AddInstance(board, 100, 100);
        var connector = AddEdge(
            board,
            new FloatingEndpoint(300, 300),
            new FloatingEndpoint(380, 300)
        );
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ClickOn(ContainerOf(canvas, shape));
        canvas.ClickElement(HitOf(canvas, connector), shift: true);

        canvas.PressOn(ContainerOf(canvas, shape), (125, 125));
        canvas.MoveTo((135, 145));
        await canvas.InvokeAsync(() => canvas.Instance.OnEscapePressed());
        canvas.ReleaseAt((135, 145));

        Assert.Equal(new FloatingEndpoint(300, 300), connector.Source);
        Assert.Equal("300", canvas.FindAll(".floating-endpoint")[0].GetAttribute("cx"));
    }

    [Fact]
    public async Task GroupingAShapesAndEdgeSelectionGroupsTheShapesAndLeavesTheEdgeSelected()
    {
        var board = new Board();
        var first = AddInstance(board, 100, 100);
        var second = AddInstance(board, 250, 100);
        var edge = Connect(board, first, second);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ClickOn(ContainerOf(canvas, first));
        canvas.ClickOn(ContainerOf(canvas, second), shift: true);
        canvas.ClickElement(HitOf(canvas, edge), shift: true);

        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        var group = Assert.Single(board.Groups);
        Assert.Equal(new HashSet<Guid> { first.Id, second.Id }, group.MemberIds.ToHashSet());
        Assert.Equal(new HashSet<Guid> { edge.Id }, SelectedEdgeIds(canvas));
        Assert.Equal("true", canvas.Find(".edge-line").GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task OneShapeAndAnEdgeAreBelowTheGroupingThreshold()
    {
        var board = new Board();
        var first = AddInstance(board, 100, 100);
        var second = AddInstance(board, 250, 100);
        var edge = Connect(board, first, second);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ClickOn(ContainerOf(canvas, first));
        canvas.ClickElement(HitOf(canvas, edge), shift: true);

        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        Assert.Empty(board.Groups);
    }

    [Fact]
    public void ResizeHandlesReflectTheInstancesOnlyBox()
    {
        var board = new Board();
        var first = AddInstance(board, 100, 100);
        var second = AddInstance(board, 250, 100);
        var edge = AddEdge(
            board,
            new PortEndpoint(first.Id, PortId.Right),
            new FloatingEndpoint(600, 600)
        );
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ClickOn(ContainerOf(canvas, first));
        canvas.ClickElement(HitOf(canvas, edge), shift: true);

        Assert.Empty(canvas.FindAll(".selection-bounding-box"));
        Assert.NotEmpty(ContainerOf(canvas, first).QuerySelectorAll(".resize-handle"));

        canvas.ClickOn(ContainerOf(canvas, second), shift: true);

        var box = canvas.Find(".selection-bounding-box").GetAttribute("style");
        Assert.Contains("left: 100px; top: 100px; width: 200px; height: 50px;", box);
    }

    [Fact]
    public async Task BringToFrontRestacksTheShapesAndSkipsTheEdge()
    {
        var board = new Board();
        var shape = AddInstance(board, 100, 100);
        var above = AddInstance(board, 400, 100);
        above.ZIndex = 5;
        var edge = Connect(board, shape, above);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ClickOn(ContainerOf(canvas, shape));
        canvas.ClickElement(HitOf(canvas, edge), shift: true);

        await canvas.InvokeAsync(() => canvas.Instance.OnBringToFrontPressed());

        Assert.Equal(6, shape.ZIndex);
    }

    [Fact]
    public async Task ASecondaryPressOnASelectedEdgeKeepsTheMixedSelection()
    {
        var board = new Board();
        var first = AddInstance(board, 100, 100);
        var second = AddInstance(board, 250, 100);
        var edge = Connect(board, first, second);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ClickOn(ContainerOf(canvas, first));
        canvas.ClickElement(HitOf(canvas, edge), shift: true);

        await canvas.Press(
            200,
            125,
            Pointer.PointerPress.SecondaryButton,
            role: Pointer.HitRole.Edge,
            entityId: edge.Id
        );
        await canvas.Release(200, 125, Pointer.PointerPress.SecondaryButton);

        Assert.Equal(new HashSet<Guid> { first.Id }, SelectedInstanceIds(canvas));
        Assert.Equal(new HashSet<Guid> { edge.Id }, SelectedEdgeIds(canvas));
        Assert.NotEmpty(canvas.FindAll(".d12-context-menu, [role='menu']"));
    }

    [Fact]
    public void SelectionChangedFiresWhenOnlyTheEdgesChange()
    {
        var board = new Board();
        var first = AddInstance(board, 100, 100);
        var second = AddInstance(board, 250, 100);
        var edge = Connect(board, first, second);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ClickOn(ContainerOf(canvas, first));
        var notifications = 0;
        canvas.Instance.SelectionChanged += (_, _) => notifications++;

        canvas.ClickElement(HitOf(canvas, edge), shift: true);
        canvas.ClickElement(HitOf(canvas, edge), shift: true);

        Assert.Equal(2, notifications);
    }
}
