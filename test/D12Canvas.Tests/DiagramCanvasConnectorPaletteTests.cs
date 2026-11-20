using System.Threading.Tasks;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

public class DiagramCanvasConnectorPaletteTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasConnectorPaletteTests()
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

        canvas.Instance.BeginPaletteDrag(DiagramCanvas.EdgePaletteKey);
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

        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd(DiagramCanvas.EdgePaletteKey));

        var edge = Assert.Single(board.Edges);
        Assert.Equal(new FloatingEndpoint(360, 300), edge.Source);
        Assert.Equal(new FloatingEndpoint(440, 300), edge.Target);
    }

    [Fact]
    public async Task ConsecutiveClickToAddsCascadeTheConnectorPlacementTooSharingTheSameCounterAsComponents()
    {
        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd(ComponentTypeKey));
        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd(DiagramCanvas.EdgePaletteKey));

        Assert.Single(board.Components);
        var edge = Assert.Single(board.Edges);
        Assert.Equal(new FloatingEndpoint(380, 320), edge.Source);
        Assert.Equal(new FloatingEndpoint(460, 320), edge.Target);
    }

    [Fact]
    public async Task ClickToAddingAConnectorSelectsAndFocusesNothing()
    {
        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd(DiagramCanvas.EdgePaletteKey));

        Assert.Empty(JSInterop.Invocations["focusTabStopAt"]);
    }

    [Fact]
    public async Task DroppingTheConnectorIsAnUndoableGesture()
    {
        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Instance.BeginPaletteDrag(DiagramCanvas.EdgePaletteKey);
        canvas.Find(".diagram-canvas").Drop(new DragEventArgs { ClientX = 300, ClientY = 250 });
        Assert.Single(board.Edges);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());
        Assert.Empty(board.Edges);

        await canvas.InvokeAsync(() => canvas.Instance.OnRedoPressed());
        Assert.Single(board.Edges);
    }

    [Fact]
    public void BothFloatingEndsOfADroppedConnectorCanLaterBeAttachedToAPort()
    {
        var board = new Board();
        var target = AddInstance(board, 500, 400);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ReturnToOrigin();

        canvas.Instance.BeginPaletteDrag(DiagramCanvas.EdgePaletteKey);
        canvas.Find(".diagram-canvas").Drop(new DragEventArgs { ClientX = 300, ClientY = 250 });
        Assert.Single(board.Edges);

        var markers = canvas.FindAll(".floating-endpoint");
        Assert.Equal(2, markers.Count);
        var targetMarker = markers[1];

        canvas.PressElement(targetMarker, (340, 250));
        canvas.MoveTo((500, 425));
        canvas.ReleaseOverPort((500, 425), target.Id, "Left");

        var edge = Assert.Single(board.Edges);
        Assert.Equal(new PortEndpoint(target.Id, PortId.Left), edge.Target);
        Assert.Equal(new FloatingEndpoint(260, 250), edge.Source);
        Assert.Single(canvas.FindAll(".floating-endpoint"));
    }
}
