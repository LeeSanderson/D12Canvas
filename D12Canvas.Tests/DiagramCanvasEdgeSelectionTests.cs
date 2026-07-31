using System.Threading.Tasks;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// Edges join the selection model - clicking an edge selects it, with a visible affordance and
// aria-selected, mirroring component-instance selection. An edge's selection lives in its own
// exclusive slot (_selectedEdgeId), never mixed into _selectedInstanceIds, since edges don't
// participate in multi-select/grouping.
public class DiagramCanvasEdgeSelectionTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasEdgeSelectionTests()
    {
        SetupDiagramCanvasJsModule();
        SetupComponentContainerJsModule();

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

    [Fact]
    public void ClickingAnEdgeSelectsItWithAVisibleAffordanceAndAriaSelected()
    {
        var board = new Board();
        AddEdgeBetween(board, AddInstance(board, 100, 100), AddInstance(board, 250, 100));
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find(".edge-line").Click();

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

        // Re-fetched between clicks: the first click's selection change re-renders every
        // .edge-line element with a freshly-captured onclick lambda, which invalidates any
        // element reference obtained before that render (bUnit's stale-element pitfall).
        canvas.FindAll(".edge-line")[0].Click();
        canvas.FindAll(".edge-line")[1].Click();

        var lines = canvas.FindAll(".edge-line");
        Assert.Null(lines[0].GetAttribute("aria-selected"));
        Assert.Equal("true", lines[1].GetAttribute("aria-selected"));
    }

    [Fact]
    public void ClickingEmptyCanvasClearsAnEdgeSelection()
    {
        var board = new Board();
        AddEdgeBetween(board, AddInstance(board, 100, 100), AddInstance(board, 250, 100));
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find(".edge-line").Click();
        Assert.Equal("true", canvas.Find(".edge-line").GetAttribute("aria-selected"));

        canvas.Find(".diagram-canvas").Click();

        Assert.Null(canvas.Find(".edge-line").GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task EscapeClearsAnEdgeSelection()
    {
        var board = new Board();
        AddEdgeBetween(board, AddInstance(board, 100, 100), AddInstance(board, 250, 100));
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find(".edge-line").Click();
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

        canvas.Find(".edge-line").Click();
        Assert.Equal("true", canvas.Find(".edge-line").GetAttribute("aria-selected"));

        canvas.FindAll(".component-container")[0].Click();

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

        canvas.FindAll(".component-container")[0].Click();
        Assert.Equal(
            "true",
            canvas.FindAll(".component-container")[0].GetAttribute("aria-selected")
        );

        canvas.Find(".edge-line").Click();

        Assert.Null(canvas.FindAll(".component-container")[0].GetAttribute("aria-selected"));
        Assert.Equal("true", canvas.Find(".edge-line").GetAttribute("aria-selected"));
    }
}
