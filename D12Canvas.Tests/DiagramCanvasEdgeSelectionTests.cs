using System.Threading.Tasks;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// Clicking an edge selects it, with a visible affordance and aria-selected, mirroring
// component-instance selection. A plain click replaces the whole selection with the edge; Shift
// adds an edge to a mixed selection, which DiagramCanvasMixedSelectionTests covers.
public class DiagramCanvasEdgeSelectionTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasEdgeSelectionTests()
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
    public void AnUnselectedEdgeHasNoAriaSelectedAttributeOrSelectedClass()
    {
        var board = new Board();
        AddEdgeBetween(board, AddInstance(board, 100, 100), AddInstance(board, 250, 100));
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var line = canvas.Find(".edge-line");
        Assert.Null(line.GetAttribute("aria-selected"));
        Assert.DoesNotContain("selected", line.ClassList);
    }

    // The line paints and takes no presses; the stroke over it is the hit region, sized against
    // the scale the content publishes so it stays 20 screen pixels wide at any zoom.
    [Fact]
    public void AnEdgeIsHitByItsOwnRegionAndNotByTheLineThatPaintsIt()
    {
        var board = new Board();
        var edge = AddEdgeBetween(
            board,
            AddInstance(board, 100, 100),
            AddInstance(board, 250, 100)
        );
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        Assert.Null(canvas.Find(".edge-line").GetAttribute("data-d12-role"));
        var region = canvas.Find(".edge-hit");
        Assert.Equal("edge", region.GetAttribute("data-d12-role"));
        Assert.Equal(edge.Id.ToString(), region.GetAttribute("data-d12-entity"));
        Assert.Contains("--d12-scale: 1;", canvas.Find(".canvas-content").GetAttribute("style"));
    }

    [Fact]
    public void ClickingAnEdgesLabelSelectsTheEdge()
    {
        var board = new Board();
        var edge = AddEdgeBetween(
            board,
            AddInstance(board, 100, 100),
            AddInstance(board, 250, 100)
        );
        edge.Label = new ComponentInstance(
            ComponentTypeKey,
            new TestProps(),
            new Bounds(0, 0, 40, 20)
        );
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.ClickElement(canvas.Find(".edge-label"));

        Assert.Equal("true", canvas.Find(".edge-line").GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task APressOnAnEdgeThatBecomesADragSelectsNothing()
    {
        var board = new Board();
        AddEdgeBetween(board, AddInstance(board, 100, 100), AddInstance(board, 250, 100));
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.PressElement(canvas.Find(".edge-hit"), (200, 125));
        await canvas.Move(400, 300);
        await canvas.Release(400, 300);

        Assert.Null(canvas.Find(".edge-line").GetAttribute("aria-selected"));
    }

    [Fact]
    public void ClickingAnEdgeSelectsItWithAVisibleAffordanceAndAriaSelected()
    {
        var board = new Board();
        AddEdgeBetween(board, AddInstance(board, 100, 100), AddInstance(board, 250, 100));
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.ClickElement(canvas.Find(".edge-hit"));

        var line = canvas.Find(".edge-line");
        Assert.Equal("true", line.GetAttribute("aria-selected"));
        Assert.Contains("selected", line.ClassList);
    }

    [Fact]
    public void ClickingASecondEdgeMovesSelectionOffTheFirst()
    {
        var board = new Board();
        AddEdgeBetween(board, AddInstance(board, 0, 0), AddInstance(board, 100, 0));
        AddEdgeBetween(board, AddInstance(board, 0, 200), AddInstance(board, 100, 200));
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.ClickElement(canvas.FindAll(".edge-hit")[0]);
        canvas.ClickElement(canvas.FindAll(".edge-hit")[1]);

        var lines = canvas.FindAll(".edge-line");
        Assert.Null(lines[0].GetAttribute("aria-selected"));
        Assert.Equal("true", lines[1].GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task ClickingEmptyCanvasClearsAnEdgeSelection()
    {
        var board = new Board();
        AddEdgeBetween(board, AddInstance(board, 100, 100), AddInstance(board, 250, 100));
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.ClickElement(canvas.Find(".edge-hit"));
        Assert.Equal("true", canvas.Find(".edge-line").GetAttribute("aria-selected"));

        await canvas.ClickCanvas(400, 400);

        Assert.Null(canvas.Find(".edge-line").GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task EscapeClearsAnEdgeSelection()
    {
        var board = new Board();
        AddEdgeBetween(board, AddInstance(board, 100, 100), AddInstance(board, 250, 100));
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.ClickElement(canvas.Find(".edge-hit"));
        Assert.Equal("true", canvas.Find(".edge-line").GetAttribute("aria-selected"));

        await canvas.InvokeAsync(() => canvas.Instance.OnEscapePressed());

        Assert.Null(canvas.Find(".edge-line").GetAttribute("aria-selected"));
    }

    [Fact]
    public void SelectingAComponentClearsAnExistingEdgeSelection()
    {
        var board = new Board();
        AddEdgeBetween(board, AddInstance(board, 100, 100), AddInstance(board, 250, 100));
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.ClickElement(canvas.Find(".edge-hit"));
        Assert.Equal("true", canvas.Find(".edge-line").GetAttribute("aria-selected"));

        canvas.ClickOn(canvas.FindAll(".component-container")[0]);

        Assert.Null(canvas.Find(".edge-line").GetAttribute("aria-selected"));
        Assert.Equal(
            "true",
            canvas.FindAll(".component-container")[0].GetAttribute("aria-selected")
        );
    }

    [Fact]
    public void SelectingAnEdgeClearsAnExistingComponentSelection()
    {
        var board = new Board();
        AddEdgeBetween(board, AddInstance(board, 100, 100), AddInstance(board, 250, 100));
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.ClickOn(canvas.FindAll(".component-container")[0]);
        Assert.Equal(
            "true",
            canvas.FindAll(".component-container")[0].GetAttribute("aria-selected")
        );

        canvas.ClickElement(canvas.Find(".edge-hit"));

        Assert.Null(canvas.FindAll(".component-container")[0].GetAttribute("aria-selected"));
        Assert.Equal("true", canvas.Find(".edge-line").GetAttribute("aria-selected"));
    }
}
