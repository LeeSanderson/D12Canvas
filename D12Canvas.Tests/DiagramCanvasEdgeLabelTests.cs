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
    public void DoubleClickingAnEdgeWithNoLabelAddsADefaultTextLabelOpenForTyping()
    {
        var board = new Board();
        var edge = AddEdgeBetween(board, AddInstance(board, 0, 0), AddInstance(board, 200, 0));
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.DoubleClickElement(canvas.Find(".edge-hit"));

        Assert.NotNull(edge.Label);
        Assert.Equal("text", edge.Label!.ComponentTypeKey);
        Assert.Single(canvas.FindAll(".edge-label"));
        Assert.Single(canvas.FindAll(".edge-label textarea.d12-text-editor"));
    }

    [Fact]
    public void DoubleClickingAnEdgeThatAlreadyHasALabelDoesNotCreateASecondOne()
    {
        var board = new Board();
        var edge = AddEdgeBetween(board, AddInstance(board, 0, 0), AddInstance(board, 200, 0));
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.DoubleClickElement(canvas.Find(".edge-hit"));
        var firstLabel = edge.Label;

        canvas.DoubleClickElement(canvas.Find(".edge-hit"));

        Assert.Same(firstLabel, edge.Label);
        Assert.Single(canvas.FindAll(".edge-label"));
    }

    [Fact]
    public void AddingALabelIsAnUndoableGesture()
    {
        var board = new Board();
        var edge = AddEdgeBetween(board, AddInstance(board, 0, 0), AddInstance(board, 200, 0));
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.DoubleClickElement(canvas.Find(".edge-hit"));
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
        canvas.DoubleClickElement(canvas.Find(".edge-hit"));
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
    // edge's own normal line is suppressed in favour of the pending line - the label must follow
    // that same line, not the edge's last-committed (pre-drag) endpoints, or it would visually
    // freeze and detach for the drag's duration.
    [Fact]
    public void TheLabelFollowsTheLiveDragPreviewWhileAnEndpointIsBeingRepositioned()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100); // right port at (150, 125)
        var target = AddInstance(board, 250, 100); // left port at (250, 125)
        AddEdgeBetween(board, source, target);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ReturnToOrigin();
        canvas.DoubleClickElement(canvas.Find(".edge-hit"));
        var styleBeforeDrag = canvas.Find(".edge-label").GetAttribute("style");
        Assert.Equal("left: 160px; top: 113px; width: 80px; height: 24px;", styleBeforeDrag);

        // Re-grab the source port (it already anchors an edge, so this carries that edge's end)
        // and move away from it without releasing yet.
        canvas.PressPort(source.Id, "Right", (150, 125));
        canvas.MoveTo((150, 400));

        // Midpoint of the fixed target port (250, 125) and the live drag point (150, 400).
        var styleDuringDrag = canvas.Find(".edge-label").GetAttribute("style");
        Assert.Equal("left: 160px; top: 250.5px; width: 80px; height: 24px;", styleDuringDrag);
    }

    [Fact]
    public void ALabelOnAnOrthogonalEdgeSitsHalfwayAlongItsPathRatherThanAtTheChordMidpoint()
    {
        var board = new Board();
        var source = AddInstance(board, 0, 100);
        var target = AddInstance(board, 300, 0);
        board.AddEdge(
            new Edge(
                new PortEndpoint(source.Id, PortId.Top),
                new PortEndpoint(target.Id, PortId.Left),
                routingStyle: EdgeRouting.Orthogonal
            )
        );
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.DoubleClickElement(canvas.Find(".edge-hit"));

        // Up from (25, 100) to (25, 25), then right to (300, 25): 350 long, so halfway is (125, 25).
        Assert.Equal("M 25 100 L 25 25 L 300 25", canvas.Find(".edge-line").GetAttribute("d"));
        Assert.Equal(
            "left: 85px; top: 13px; width: 80px; height: 24px;",
            canvas.Find(".edge-label").GetAttribute("style")
        );
    }

    [Fact]
    public void ACarriedEndOfAnOrthogonalEdgePreviewsInTheEdgesOwnStyle()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var target = AddInstance(board, 250, 100);
        board.AddEdge(
            new Edge(
                new PortEndpoint(source.Id, PortId.Right),
                new PortEndpoint(target.Id, PortId.Left),
                routingStyle: EdgeRouting.Orthogonal
            )
        );
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ReturnToOrigin();

        canvas.PressPort(source.Id, "Right", (150, 125));
        canvas.MoveTo((150, 400));

        var preview = canvas.Find(".connector-drag-preview");
        Assert.Equal("path", preview.TagName, ignoreCase: true);
        Assert.StartsWith("M 150 400 L 150 ", preview.GetAttribute("d"));
        Assert.EndsWith(" 125 L 250 125", preview.GetAttribute("d"));
    }

    [Fact]
    public void EditingTheLabelsTextCommitsOneMutateEntityCommand()
    {
        var board = new Board();
        var edge = AddEdgeBetween(board, AddInstance(board, 0, 0), AddInstance(board, 200, 0));
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.DoubleClickElement(canvas.Find(".edge-hit"));
        canvas.Find("textarea.d12-text-editor").Blur();

        canvas.DoubleClickElement(canvas.Find(".edge-label"));
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
    public void EscapeOutOfALabelEditCommitsAndReturnsFocusToTheEdgesStop()
    {
        var board = new Board();
        var edge = AddEdgeBetween(board, AddInstance(board, 0, 0), AddInstance(board, 200, 0));
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.DoubleClickElement(canvas.Find(".edge-hit"));

        var editor = canvas.Find("textarea.d12-text-editor");
        editor.Input("Kept");
        editor.KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Equal("Kept", ((TextProps)edge.Label!.Props).Text);
        var focus = Assert.Single(CanvasModule.Invocations["focusTabStopAt"]);
        Assert.Equal(1, focus.Arguments[1]);
    }

    [Fact]
    public void LabellingAnEdgeWhoseMidpointIsOffScreenPansTheNewLabelIntoView()
    {
        var board = new Board();
        AddEdgeBetween(board, AddInstance(board, 0, 0), AddInstance(board, 2000, 0));
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ReturnToOrigin();

        canvas.DoubleClickElement(canvas.Find(".edge-hit"));

        var viewport = canvas.Instance.ZoomPanTracker.Viewport;
        var label = board.Edges.Single().Label!;
        Assert.Equal(1, canvas.Instance.ZoomPanTracker.Scale);
        Assert.True(viewport.X > 0);
        Assert.Single(canvas.FindAll(".edge-label textarea.d12-text-editor"));
        Assert.True(label.Bounds.Width <= viewport.Width);
    }

    [Fact]
    public void DeletingTheEdgeRemovesItsLabelAndUndoRestoresBothTogether()
    {
        var board = new Board();
        var edge = AddEdgeBetween(board, AddInstance(board, 0, 0), AddInstance(board, 200, 0));
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.DoubleClickElement(canvas.Find(".edge-hit"));
        var label = edge.Label;
        Assert.NotNull(label);

        canvas.ClickElement(canvas.Find(".edge-hit"));
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
        canvas.DoubleClickElement(canvas.Find(".edge-hit"));
        canvas.DoubleClickElement(canvas.Find(".edge-label"));
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
