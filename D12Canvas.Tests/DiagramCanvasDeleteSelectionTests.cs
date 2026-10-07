using System.Threading.Tasks;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// Delete removes every currently selected instance from Board and clears the selection - single
// and multi-selection are the same code path (unlike move/resize, deletion has no "as one unit"
// delta to apply). Undo-wrapping is covered by DiagramCanvasUndoRedoTests.
// Delete also removes a selected edge - its own exclusive branch, since an edge selection is
// never mixed into the instance-selection set.
public class DiagramCanvasDeleteSelectionTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasDeleteSelectionTests()
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

    private static ComponentInstance AddInstance(Board board, double x)
    {
        var instance = new ComponentInstance(
            ComponentTypeKey,
            new TestProps(),
            new Bounds(x, 0, 50, 50)
        );
        board.AddComponent(instance);
        return instance;
    }

    [Fact]
    public async Task DeletePressedRemovesTheSingleSelectedInstanceFromBoardAndClearsSelection()
    {
        var board = new Board();
        var instance = AddInstance(board, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.ClickOn(canvas.Find(".component-container"));
        Assert.Equal("true", canvas.Find(".component-container").GetAttribute("aria-selected"));

        await canvas.InvokeAsync(() => canvas.Instance.OnDeletePressed());

        Assert.Null(board.GetComponent(instance.Id));
        Assert.Empty(canvas.FindAll(".component-container"));
    }

    [Fact]
    public async Task DeletePressedRemovesEveryMultiSelectedInstanceAndClearsSelection()
    {
        var board = new Board();
        var first = AddInstance(board, 0);
        var second = AddInstance(board, 100);
        var untouched = AddInstance(board, 200);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var containers = canvas.FindAll(".component-container");
        canvas.ClickOn(containers[0]);
        canvas.ClickOn(containers[1], shift: true);

        await canvas.InvokeAsync(() => canvas.Instance.OnDeletePressed());

        Assert.Null(board.GetComponent(first.Id));
        Assert.Null(board.GetComponent(second.Id));
        Assert.NotNull(board.GetComponent(untouched.Id));
        Assert.Single(canvas.FindAll(".component-container"));
        Assert.Null(canvas.Find(".component-container").GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task DeletePressedOnAGroupRemovesTheGroupWithItsMembersAndOneUndoRestoresBoth()
    {
        var board = new Board();
        var first = AddInstance(board, 0);
        var second = AddInstance(board, 100);
        var group = new Group([first.Id, second.Id]);
        board.AddGroup(group);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.ClickOn(canvas.Find(".component-container"));
        await canvas.InvokeAsync(() => canvas.Instance.OnDeletePressed());

        Assert.Empty(board.Components);
        Assert.Empty(board.Groups);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal(2, board.Components.Count);
        Assert.Same(group, Assert.Single(board.Groups));
        Assert.Equal([first.Id, second.Id], group.MemberIds);
    }

    [Fact]
    public async Task DeletePressedWithNoSelectionIsANoOp()
    {
        var board = new Board();
        var instance = AddInstance(board, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        await canvas.InvokeAsync(() => canvas.Instance.OnDeletePressed());

        Assert.NotNull(board.GetComponent(instance.Id));
        Assert.Single(canvas.FindAll(".component-container"));
    }

    [Fact]
    public async Task DeletePressedRemovesTheSelectedEdgeAndClearsItsSelection()
    {
        var board = new Board();
        var source = AddInstance(board, 100);
        var target = AddInstance(board, 250);
        var edge = new Edge(
            new PortEndpoint(source.Id, PortId.Right),
            new PortEndpoint(target.Id, PortId.Left)
        );
        board.AddEdge(edge);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ClickElement(canvas.Find(".edge-hit"));

        await canvas.InvokeAsync(() => canvas.Instance.OnDeletePressed());

        Assert.Null(board.GetEdge(edge.Id));
        Assert.Empty(canvas.FindAll(".edge-line"));
        Assert.NotNull(board.GetComponent(source.Id));
        Assert.NotNull(board.GetComponent(target.Id));
    }
}
