using AngleSharp.Dom;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Pointer;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// The canvas content holds four layers in a fixed order, each its own stacking context, and
// ZIndex orders only the inside of the instance layer. These tests pin which layer each kind of
// element lives in; the browser's stacking does the rest.
public class DiagramCanvasPaintLayerTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    private static readonly string[] LayersBottomToTop =
    [
        "edge-band",
        "instance-layer",
        "guide-layer",
        "selection-chrome",
    ];

    public DiagramCanvasPaintLayerTests()
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
        int zIndex = 0,
        double size = 50
    )
    {
        var instance = new ComponentInstance(
            ComponentTypeKey,
            new TestProps(),
            new Bounds(x, y, size, size),
            zIndex
        );
        board.AddComponent(instance);
        return instance;
    }

    private static string LayerOf(IElement element)
    {
        var layer = LayersBottomToTop.SingleOrDefault(name =>
            element.Closest($".canvas-content > .{name}") is not null
        );
        return layer ?? throw new InvalidOperationException($"{element.TagName} is in no layer.");
    }

    [Fact]
    public void TheContentHoldsExactlyTheFourLayersBottomToTop()
    {
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, new Board()));

        var children = canvas.Find(".canvas-content").Children.Select(child => child.ClassName);

        Assert.Equal(LayersBottomToTop, children);
    }

    [Fact]
    public void EdgeLinesHitStrokesAndLabelsPaintInTheEdgeBand()
    {
        var board = new Board();
        var source = AddInstance(board, 0, 0);
        var target = AddInstance(board, 300, 0);
        board.AddEdge(
            new Edge(
                new PortEndpoint(source.Id, PortId.Right),
                new PortEndpoint(target.Id, PortId.Left),
                label: new ComponentInstance(
                    ComponentTypeKey,
                    new TestProps("label"),
                    new Bounds(0, 0, 40, 20)
                )
            )
        );

        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        Assert.Equal("edge-band", LayerOf(canvas.Find(".edge-line")));
        Assert.Equal("edge-band", LayerOf(canvas.Find(".edge-hit")));
        Assert.Equal("edge-band", LayerOf(canvas.Find(".edge-label")));
    }

    [Fact]
    public void InstancesPlaceholdersGroupTabStopsAndHostContentPaintInTheInstanceLayer()
    {
        var board = new Board();
        var first = AddInstance(board, 0, 0);
        var second = AddInstance(board, 100, 0);
        AddInstance(board, 400, 0, size: 2);
        board.AddGroup(new Group([first.Id, second.Id]));

        var canvas = Render<DiagramCanvas>(parameters =>
            parameters
                .Add(p => p.Board, board)
                .Add(
                    p => p.ChildContent,
                    (RenderFragment)(
                        builder =>
                        {
                            builder.OpenElement(0, "div");
                            builder.AddAttribute(1, "class", "host-content");
                            builder.CloseElement();
                        }
                    )
                )
        );

        Assert.All(
            canvas.FindAll(".component-container"),
            container => Assert.Equal("instance-layer", LayerOf(container))
        );
        Assert.Equal("instance-layer", LayerOf(canvas.Find(".lod-placeholder")));
        Assert.Equal("instance-layer", LayerOf(canvas.Find(".group-tab-stop")));
        Assert.Equal("instance-layer", LayerOf(canvas.Find(".host-content")));
    }

    [Fact]
    public async Task TheMarqueePaintsInSelectionChrome()
    {
        var board = new Board();
        AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        await canvas.Press(10, 10);
        await canvas.Move(200, 200);

        Assert.Equal("selection-chrome", LayerOf(canvas.Find(".marquee-select")));
    }

    [Fact]
    public async Task TheSelectionBoxAndItsHandlesPaintInSelectionChrome()
    {
        var board = new Board();
        AddInstance(board, 100, 100, zIndex: 5000);
        AddInstance(board, 200, 100, zIndex: 5001);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ReturnToOrigin();

        await canvas.Marquee((10, 10), (400, 300));

        Assert.Equal("selection-chrome", LayerOf(canvas.Find(".selection-bounding-box")));
        Assert.All(
            canvas.FindAll(".group-resize-handle"),
            handle => Assert.Equal("selection-chrome", LayerOf(handle))
        );
    }

    [Fact]
    public void FloatingEndpointsPaintInSelectionChrome()
    {
        var board = new Board();
        var source = AddInstance(board, 0, 0);
        board.AddEdge(
            new Edge(new PortEndpoint(source.Id, PortId.Right), new FloatingEndpoint(25, 25))
        );

        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        Assert.Equal("selection-chrome", LayerOf(canvas.Find(".floating-endpoint")));
    }

    [Fact]
    public void TheConnectorDragPreviewPaintsInSelectionChrome()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.PressPort(source.Id, "Right", (150, 125));
        canvas.MoveTo((180, 130));

        Assert.Equal("selection-chrome", LayerOf(canvas.Find(".connector-drag-preview")));
    }

    [Fact]
    public void ZIndexStillOrdersInstancesInsideTheInstanceLayer()
    {
        var board = new Board();
        AddInstance(board, 0, 0, zIndex: -50);
        AddInstance(board, 100, 0, zIndex: 5000);

        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var styles = canvas
            .FindAll(".instance-layer > .component-container")
            .Select(container => container.GetAttribute("style"));
        Assert.Contains(styles, style => style!.Contains("z-index: -50"));
        Assert.Contains(styles, style => style!.Contains("z-index: 5000"));
    }
}
