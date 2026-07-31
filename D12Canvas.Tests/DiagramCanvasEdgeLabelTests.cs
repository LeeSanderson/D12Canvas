using Bunit;
using D12Canvas.BuiltIns;
using D12Canvas.History;
using D12Canvas.Model;
using D12Canvas.Persistence;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// An edge label is a full ComponentInstance (defaulting to Text) embedded directly on its owning
// Edge, not a separate Board entity - added by double-clicking the edge's own line, positioned
// live at the edge's current midpoint, edited in place via the exact same inline WYSIWYG
// mechanism Text already has, and removed automatically when its edge is deleted. Exercised
// through the real DiagramCanvas/ComponentContainer/Text stack, same as
// DiagramCanvasInlineTextEditingTests.
public class DiagramCanvasEdgeLabelTests : ComponentTestBase
{
    private const string TestComponentTypeKey = "test-props";

    public DiagramCanvasEdgeLabelTests()
    {
        SetupDiagramCanvasJsModule();
        SetupComponentContainerJsModule();

        var registry = new ComponentRegistry();
        registry.Register(
            new ComponentRegistration(
                Key: "text",
                ComponentType: typeof(Text),
                PropsType: typeof(TextProps),
                DisplayName: "Text",
                AccessibleName: "Text",
                DefaultProps: new TextProps("", "#000000", 16, "normal", "left"),
                Icon: null,
                Role: "group",
                DefaultSize: new ComponentSize(80, 24),
                Category: null
            )
        );
        registry.Register(
            new ComponentRegistration(
                Key: TestComponentTypeKey,
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
            TestComponentTypeKey,
            new TestProps(),
            new Bounds(x, y, 50, 50)
        );
        board.AddComponent(instance);
        return instance;
    }

    private static Edge AddEdgeBetween(
        Board board,
        ComponentInstance source,
        ComponentInstance target
    )
    {
        var edge = new Edge(
            new PortEndpoint(source.Id, PortId.Right),
            new PortEndpoint(target.Id, PortId.Left)
        );
        board.AddEdge(edge);
        return edge;
    }

    [Fact]
    public void DoubleClickingAnEdgeWithNoLabelAddsADefaultTextLabel()
    {
        var board = new Board();
        var edge = AddEdgeBetween(board, AddInstance(board, 0, 0), AddInstance(board, 200, 0));
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find(".edge-line").DoubleClick();

        Assert.NotNull(edge.Label);
        Assert.Equal("text", edge.Label!.ComponentTypeKey);
        Assert.Single(canvas.FindAll(".edge-label"));
        Assert.Single(canvas.FindAll("p.d12-text"));
    }

    [Fact]
    public void DoubleClickingAnEdgeThatAlreadyHasALabelDoesNotCreateASecondOne()
    {
        var board = new Board();
        var edge = AddEdgeBetween(board, AddInstance(board, 0, 0), AddInstance(board, 200, 0));
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Find(".edge-line").DoubleClick();
        var firstLabel = edge.Label;

        canvas.Find(".edge-line").DoubleClick();

        Assert.Same(firstLabel, edge.Label);
        Assert.Single(canvas.FindAll(".edge-label"));
    }

    [Fact]
    public void AddingALabelIsAnUndoableGesture()
    {
        var board = new Board();
        var edge = AddEdgeBetween(board, AddInstance(board, 0, 0), AddInstance(board, 200, 0));
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find(".edge-line").DoubleClick();
        Assert.NotNull(edge.Label);

        canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());
        Assert.Null(edge.Label);
        Assert.Empty(canvas.FindAll(".edge-label"));

        canvas.InvokeAsync(() => canvas.Instance.OnRedoPressed());
        Assert.NotNull(edge.Label);
    }

    [Fact]
    public void TheLabelStaysPositionedAtTheEdgesMidpointAsAnEndpointInstanceMoves()
    {
        var board = new Board();
        var source = AddInstance(board, 0, 0);
        var target = AddInstance(board, 200, 0);
        AddEdgeBetween(board, source, target);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Find(".edge-line").DoubleClick();
        var styleBefore = canvas.Find(".edge-label").GetAttribute("style");

        // Move the target instance far away - the label's live-derived midpoint must follow.
        // OnContainerResized is a harmless existing StateHasChanged trigger (same dimensions the
        // JS module setup already reports) - forces the re-render this direct model mutation
        // doesn't itself raise any Blazor-visible event for.
        target.Bounds = new Bounds(1000, 1000, target.Bounds.Width, target.Bounds.Height);
        canvas.InvokeAsync(() => canvas.Instance.OnContainerResized(800, 600));

        var styleAfter = canvas.Find(".edge-label").GetAttribute("style");
        Assert.NotEqual(styleBefore, styleAfter);
    }

    // While an existing edge's endpoint is mid-drag (reposition, not a brand-new connection), the
    // edge's own normal line is suppressed in favour of the live drag preview
    // (DiagramCanvas.ConnectPreviewLine) - the label must follow that same preview, not the edge's
    // last-committed (pre-drag) endpoints, or it would visually freeze and detach for the drag's
    // duration.
    [Fact]
    public void TheLabelFollowsTheLiveDragPreviewWhileAnEndpointIsBeingRepositioned()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100); // right port at (150, 125)
        var target = AddInstance(board, 250, 100); // left port at (250, 125)
        AddEdgeBetween(board, source, target);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Find(".edge-line").DoubleClick();
        var styleBeforeDrag = canvas.Find(".edge-label").GetAttribute("style");
        Assert.Equal("left: 160px; top: 113px; width: 80px; height: 24px;", styleBeforeDrag);

        // Re-grab the source port (it already anchors an edge, so this starts a reposition drag -
        // see StartPortDrag) and move away from it without releasing yet.
        var sourcePort = canvas.FindAll(".component-container")[0].QuerySelector(".port-right")!;
        sourcePort.MouseDown(new MouseEventArgs { ClientX = 150, ClientY = 125 });
        canvas
            .Find(".diagram-canvas")
            .MouseMove(new MouseEventArgs { ClientX = 150, ClientY = 400 });

        // Midpoint of the fixed target port (250, 125) and the live drag point (150, 400).
        var styleDuringDrag = canvas.Find(".edge-label").GetAttribute("style");
        Assert.Equal("left: 160px; top: 250.5px; width: 80px; height: 24px;", styleDuringDrag);
    }

    [Fact]
    public void EditingTheLabelsTextCommitsOneMutateEntityCommand()
    {
        var board = new Board();
        var edge = AddEdgeBetween(board, AddInstance(board, 0, 0), AddInstance(board, 200, 0));
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Find(".edge-line").DoubleClick();

        canvas.Find("p.d12-text").DoubleClick();
        var editor = canvas.Find("textarea.d12-text-editor");
        editor.Input("Connects A to B");
        editor.Blur();

        Assert.Equal("Connects A to B", ((TextProps)edge.Label!.Props).Text);
        Assert.Contains("Connects A to B", canvas.Find("p.d12-text").TextContent);

        canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());
        Assert.Equal("", ((TextProps)edge.Label!.Props).Text);

        canvas.InvokeAsync(() => canvas.Instance.OnRedoPressed());
        Assert.Equal("Connects A to B", ((TextProps)edge.Label!.Props).Text);
    }

    [Fact]
    public void DeletingTheEdgeRemovesItsLabelAndUndoRestoresBothTogether()
    {
        var board = new Board();
        var edge = AddEdgeBetween(board, AddInstance(board, 0, 0), AddInstance(board, 200, 0));
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Find(".edge-line").DoubleClick();
        var label = edge.Label;
        Assert.NotNull(label);

        canvas.Find(".edge-line").Click();
        canvas.InvokeAsync(() => canvas.Instance.OnDeletePressed());

        Assert.Null(board.GetEdge(edge.Id));
        Assert.Empty(canvas.FindAll(".edge-label"));

        canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        var restoredEdge = board.GetEdge(edge.Id);
        Assert.NotNull(restoredEdge);
        Assert.Same(label, restoredEdge!.Label);
        Assert.Single(canvas.FindAll(".edge-label"));
    }

    [Fact]
    public void ALabelledEdgeRoundTripsThroughJsonSerialization()
    {
        var board = new Board();
        AddEdgeBetween(board, AddInstance(board, 0, 0), AddInstance(board, 200, 0));
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Find(".edge-line").DoubleClick();
        canvas.Find("p.d12-text").DoubleClick();
        canvas.Find("textarea.d12-text-editor").Input("Persisted label");
        canvas.Find("textarea.d12-text-editor").Blur();

        var serializer = new BoardJsonSerializer(Services.GetRequiredService<IComponentRegistry>());
        var json = serializer.Serialize(board);
        var reloaded = serializer.Deserialize(json);

        var reloadedEdge = Assert.Single(reloaded.Edges);
        Assert.NotNull(reloadedEdge.Label);
        Assert.Equal("Persisted label", ((TextProps)reloadedEdge.Label!.Props).Text);
    }
}
