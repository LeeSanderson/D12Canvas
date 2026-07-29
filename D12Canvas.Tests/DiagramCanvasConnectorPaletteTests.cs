using System.Threading.Tasks;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// Ticket 54/ADR 0009: the built-in "Connector" palette entry - not a registry registration (Edge
// isn't a component type) - flows through the exact same BeginPaletteDrag/ClickToAdd gesture
// plumbing every other palette entry uses (tickets 27/28), but drops a new Edge with both endpoints
// floating instead of a ComponentInstance.
public class DiagramCanvasConnectorPaletteTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasConnectorPaletteTests()
    {
        SetupDiagramCanvasJsModule();
        JSInterop.SetupModule("./_content/D12Canvas/ComponentContainer.razor.js");

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

    private static ComponentInstance AddInstance(
        Board board,
        double x,
        double y,
        double width = 50,
        double height = 50
    )
    {
        var instance = new ComponentInstance(
            ComponentTypeKey,
            new TestProps(),
            new Bounds(x, y, width, height)
        );
        board.AddComponent(instance);
        return instance;
    }

    [Fact]
    public void DroppingTheConnectorEntryCreatesAnEdgeWithBothEndsFloatingCenteredOnTheDropPoint()
    {
        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Instance.BeginPaletteDrag(DiagramCanvas.ConnectorPaletteKey);
        canvas.Find(".diagram-canvas").Drop(new DragEventArgs { ClientX = 300, ClientY = 250 });

        var edge = Assert.Single(board.Edges);
        Assert.Equal(new FloatingEndpoint(260, 250), edge.Source);
        Assert.Equal(new FloatingEndpoint(340, 250), edge.Target);
    }

    [Fact]
    public void DroppingWithoutAPrecedingConnectorDragLeavesTheBoardUnchanged()
    {
        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find(".diagram-canvas").Drop(new DragEventArgs { ClientX = 300, ClientY = 250 });

        Assert.Empty(board.Edges);
    }

    [Fact]
    public async Task ClickToAddPlacesAFloatingEdgeCenteredOnTheViewport()
    {
        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        // Container is 800x600 (see ComponentTestBase.SetupDiagramCanvasJsModule) at scale 1 with
        // no pan, so the viewport center in board coordinates is (400, 300).
        await canvas.InvokeAsync(
            () => canvas.Instance.ClickToAdd(DiagramCanvas.ConnectorPaletteKey)
        );

        var edge = Assert.Single(board.Edges);
        Assert.Equal(new FloatingEndpoint(360, 300), edge.Source);
        Assert.Equal(new FloatingEndpoint(440, 300), edge.Target);
    }

    // The cascade counter (ADR 0009) is shared, global, and never resets across gesture kinds
    // (ticket 28) - a click-to-add connector right after a click-to-add shape still bumps the same
    // counter, so neither stacks invisibly on the other.
    [Fact]
    public async Task ConsecutiveClickToAddsCascadeTheConnectorPlacementTooSharingTheSameCounterAsComponents()
    {
        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd(ComponentTypeKey));
        await canvas.InvokeAsync(
            () => canvas.Instance.ClickToAdd(DiagramCanvas.ConnectorPaletteKey)
        );

        Assert.Single(board.Components);
        var edge = Assert.Single(board.Edges);
        Assert.Equal(new FloatingEndpoint(380, 320), edge.Source);
        Assert.Equal(new FloatingEndpoint(460, 320), edge.Target);
    }

    [Fact]
    public async Task DroppingTheConnectorIsAnUndoableGesture()
    {
        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Instance.BeginPaletteDrag(DiagramCanvas.ConnectorPaletteKey);
        canvas.Find(".diagram-canvas").Drop(new DragEventArgs { ClientX = 300, ClientY = 250 });
        Assert.Single(board.Edges);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());
        Assert.Empty(board.Edges);

        await canvas.InvokeAsync(() => canvas.Instance.OnRedoPressed());
        Assert.Single(board.Edges);
    }

    // Ticket 49's own reattach gesture, exercised end-to-end starting from a connector-dropped edge
    // rather than a port-drag-created one - the mechanics are identical either way, since both
    // endpoints are already ordinary FloatingEndpoints once the edge exists.
    [Fact]
    public void BothFloatingEndsOfADroppedConnectorCanLaterBeAttachedToAPort()
    {
        var board = new Board();
        var target = AddInstance(board, 500, 400); // left port at (500, 425)
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Instance.BeginPaletteDrag(DiagramCanvas.ConnectorPaletteKey);
        canvas.Find(".diagram-canvas").Drop(new DragEventArgs { ClientX = 300, ClientY = 250 });
        Assert.Single(board.Edges);

        // FloatingEndpoints() yields an edge's source before its target (DiagramCanvas.razor) - the
        // dropped connector's target end sits at (340, 250).
        var markers = canvas.FindAll(".floating-endpoint");
        Assert.Equal(2, markers.Count);
        var targetMarker = markers[1];

        targetMarker.MouseDown(new MouseEventArgs { ClientX = 340, ClientY = 250 });
        var background = canvas.Find(".diagram-canvas");
        background.MouseMove(new MouseEventArgs { ClientX = 500, ClientY = 425 });
        background.MouseUp(new MouseEventArgs { ClientX = 500, ClientY = 425 });

        var edge = Assert.Single(board.Edges);
        Assert.Equal(new PortEndpoint(target.Id, PortId.Left), edge.Target);
        Assert.Equal(new FloatingEndpoint(260, 250), edge.Source); // the other end is left untouched
        Assert.Single(canvas.FindAll(".floating-endpoint"));
    }
}
