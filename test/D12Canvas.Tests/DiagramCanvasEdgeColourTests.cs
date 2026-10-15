using Bunit;
using D12Canvas.History;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// An edge paints its own colour when it has one and the theme's edge token otherwise; selection
// adds a translucent accent halo beneath the line and never repaints it.
public class DiagramCanvasEdgeColourTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasEdgeColourTests()
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

    private static Edge AddEdge(
        Board board,
        string? color = null,
        EdgeRouting routingStyle = EdgeRouting.Straight
    )
    {
        var source = new ComponentInstance(
            ComponentTypeKey,
            new TestProps(),
            new Bounds(100, 100, 50, 50)
        );
        var target = new ComponentInstance(
            ComponentTypeKey,
            new TestProps(),
            new Bounds(300, 200, 50, 50)
        );
        board.AddComponent(source);
        board.AddComponent(target);
        var edge = new Edge(
            new PortEndpoint(source.Id, PortId.Right),
            new PortEndpoint(target.Id, PortId.Left),
            routingStyle: routingStyle,
            color: color
        );
        board.AddEdge(edge);
        return edge;
    }

    [Fact]
    public void AnUncolouredEdgeEmitsNoOverride()
    {
        var board = new Board();
        AddEdge(board);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        Assert.Null(canvas.Find(".edge-line").GetAttribute("style"));
    }

    [Fact]
    public void AColouredEdgeRebindsTheOverrideInline()
    {
        var board = new Board();
        AddEdge(board, color: "#e5246b");
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        Assert.Equal(
            "--d12-edge-override: #e5246b",
            canvas.Find(".edge-line").GetAttribute("style")
        );
    }

    [Theory]
    [InlineData(EdgeRouting.Straight)]
    [InlineData(EdgeRouting.Orthogonal)]
    public void ASelectedEdgeDrawsAHaloBeneathItsUnchangedLineInTheEdgeBand(
        EdgeRouting routingStyle
    )
    {
        var board = new Board();
        AddEdge(board, color: "#e5246b", routingStyle: routingStyle);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var unselected = canvas.Find(".edge-line").OuterHtml;

        canvas.ClickElement(canvas.Find(".edge-hit"));

        var halo = canvas.Find(".edge-halo");
        var line = canvas.Find(".edge-line");
        Assert.Equal("edge-line", halo.NextElementSibling?.GetAttribute("class"));
        Assert.Equal(line.TagName, halo.TagName);
        Assert.Equal(line.GetAttribute("d"), halo.GetAttribute("d"));
        Assert.Equal(line.GetAttribute("x1"), halo.GetAttribute("x1"));
        Assert.Equal(line.GetAttribute("x2"), halo.GetAttribute("x2"));
        Assert.NotNull(halo.Closest(".canvas-content > .edge-band"));
        Assert.Null(halo.GetAttribute("marker-end"));
        Assert.Equal(
            unselected.Replace(" aria-selected=\"true\"", ""),
            line.OuterHtml.Replace(" aria-selected=\"true\"", "")
        );
    }

    [Fact]
    public void EachSelectedEdgeCarriesItsOwnHalo()
    {
        var board = new Board();
        AddEdge(board);
        AddEdge(board);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.ClickElement(canvas.FindAll(".edge-hit")[0]);
        Assert.Single(canvas.FindAll(".edge-halo"));
        canvas.ClickElement(canvas.FindAll(".edge-hit")[1], shift: true);

        Assert.Equal(2, canvas.FindAll(".edge-halo").Count);
    }

    [Fact]
    public void CommittingAColourRepaintsTheEdgeAndUndoClearsTheOverride()
    {
        var board = new Board();
        var edge = AddEdge(board);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var before = new EdgeStyle(edge.RoutingStyle, edge.SourceArrow, edge.TargetArrow);

        canvas.InvokeAsync(
            () =>
                canvas.Instance.CommitEdgeStyleChange(
                    edge.Id,
                    before,
                    before with
                    {
                        Color = "#2ecc71",
                    }
                )
        );

        Assert.Equal(
            "--d12-edge-override: #2ecc71",
            canvas.Find(".edge-line").GetAttribute("style")
        );

        canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Null(edge.Color);
        Assert.Null(canvas.Find(".edge-line").GetAttribute("style"));
    }
}
