using System.Linq;
using System.Threading.Tasks;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// Four undoable layering commands (bring to front / send to back / bring forward / send
// backward), each an arithmetic-only write to ComponentInstance.ZIndex - no renumbering pass over
// other entities - with tie handling among neighbours sharing a ZIndex.
public class DiagramCanvasZIndexCommandsTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasZIndexCommandsTests()
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

    private static ComponentInstance AddInstance(Board board, double x, int zIndex)
    {
        var instance = new ComponentInstance(
            ComponentTypeKey,
            new TestProps(),
            new Bounds(x, 0, 50, 50),
            zIndex
        );
        board.AddComponent(instance);
        return instance;
    }

    private static void Select(IRenderedComponent<DiagramCanvas> canvas, int index) =>
        canvas.FindAll(".component-container")[index].Click();

    private static void SelectBoth(IRenderedComponent<DiagramCanvas> canvas)
    {
        var containers = canvas.FindAll(".component-container");
        containers[0].Click();
        containers[1].Click(new MouseEventArgs { ShiftKey = true });
    }

    [Fact]
    public async Task BringToFrontPressedMovesTheSelectedInstanceAboveEveryOtherInstance()
    {
        var board = new Board();
        var target = AddInstance(board, 0, zIndex: 2);
        AddInstance(board, 100, zIndex: 5);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        Select(canvas, 0);

        await canvas.InvokeAsync(() => canvas.Instance.OnBringToFrontPressed());

        Assert.Equal(6, target.ZIndex);
    }

    [Fact]
    public async Task SendToBackPressedMovesTheSelectedInstanceBelowEveryOtherInstance()
    {
        var board = new Board();
        AddInstance(board, 0, zIndex: 2);
        var target = AddInstance(board, 100, zIndex: 5);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        Select(canvas, 1);

        await canvas.InvokeAsync(() => canvas.Instance.OnSendToBackPressed());

        Assert.Equal(1, target.ZIndex);
    }

    [Fact]
    public async Task BringForwardPressedMovesToTheNextDistinctHigherRank()
    {
        var board = new Board();
        var target = AddInstance(board, 0, zIndex: 2);
        AddInstance(board, 100, zIndex: 5);
        AddInstance(board, 200, zIndex: 8);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        Select(canvas, 0);

        await canvas.InvokeAsync(() => canvas.Instance.OnBringForwardPressed());

        Assert.Equal(5, target.ZIndex);
    }

    [Fact]
    public async Task BringForwardPressedSkipsPastTiedNeighboursToTheNextDistinctRank()
    {
        var board = new Board();
        var target = AddInstance(board, 0, zIndex: 5);
        AddInstance(board, 100, zIndex: 5); // tied with target at the same rank
        AddInstance(board, 200, zIndex: 8);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        Select(canvas, 0);

        await canvas.InvokeAsync(() => canvas.Instance.OnBringForwardPressed());

        Assert.Equal(8, target.ZIndex);
    }

    [Fact]
    public async Task BringForwardPressedIsANoOpWhenAlreadyAtTheTopRank()
    {
        var board = new Board();
        AddInstance(board, 0, zIndex: 2);
        var target = AddInstance(board, 100, zIndex: 5);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        Select(canvas, 1);

        await canvas.InvokeAsync(() => canvas.Instance.OnBringForwardPressed());

        Assert.Equal(5, target.ZIndex);
        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());
        Assert.Equal(5, target.ZIndex); // nothing was pushed onto history to undo
    }

    [Fact]
    public async Task SendBackwardPressedMovesToTheNextDistinctLowerRank()
    {
        var board = new Board();
        AddInstance(board, 0, zIndex: 2);
        var target = AddInstance(board, 100, zIndex: 5);
        AddInstance(board, 200, zIndex: 8);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        Select(canvas, 1);

        await canvas.InvokeAsync(() => canvas.Instance.OnSendBackwardPressed());

        Assert.Equal(2, target.ZIndex);
    }

    [Fact]
    public async Task SendBackwardPressedIsANoOpWhenAlreadyAtTheBottomRank()
    {
        var board = new Board();
        var target = AddInstance(board, 0, zIndex: 2);
        AddInstance(board, 100, zIndex: 5);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        Select(canvas, 0);

        await canvas.InvokeAsync(() => canvas.Instance.OnSendBackwardPressed());

        Assert.Equal(2, target.ZIndex);
    }

    [Fact]
    public async Task LayeringCommandsAreANoOpWhenNothingIsSelected()
    {
        var board = new Board();
        var only = AddInstance(board, 0, zIndex: 2);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        await canvas.InvokeAsync(() => canvas.Instance.OnBringToFrontPressed());

        Assert.Equal(2, only.ZIndex);
    }

    [Fact]
    public async Task LayeringCommandsAreANoOpWhenNoBoardIsWired()
    {
        var canvas = Render<DiagramCanvas>();

        var exception = await Record.ExceptionAsync(
            () => canvas.InvokeAsync(() => canvas.Instance.OnBringToFrontPressed())
        );

        Assert.Null(exception);
    }

    [Fact]
    public async Task BringToFrontPressedAppliesToEveryMemberOfAMultiSelectionPreservingTheirRelativeOrder()
    {
        var board = new Board();
        var first = AddInstance(board, 0, zIndex: 2);
        var second = AddInstance(board, 100, zIndex: 3);
        AddInstance(board, 200, zIndex: 9);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        SelectBoth(canvas);

        await canvas.InvokeAsync(() => canvas.Instance.OnBringToFrontPressed());

        // Both land above the untouched third instance (9), and first (originally the lower of
        // the two) stays below second - a bulk move never collapses the pair onto the same tied
        // value, which would erase their own relative stacking order.
        Assert.Equal(10, first.ZIndex);
        Assert.Equal(11, second.ZIndex);
    }

    [Fact]
    public async Task SendToBackPressedAppliesToEveryMemberOfAMultiSelectionPreservingTheirRelativeOrder()
    {
        var board = new Board();
        AddInstance(board, 0, zIndex: 0);
        var first = AddInstance(board, 100, zIndex: 5);
        var second = AddInstance(board, 200, zIndex: 8);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var containers = canvas.FindAll(".component-container");
        containers[1].Click();
        containers[2].Click(new MouseEventArgs { ShiftKey = true });

        await canvas.InvokeAsync(() => canvas.Instance.OnSendToBackPressed());

        // Both land below the untouched first instance (0), and first-selected (originally the
        // lower of the two) stays below second-selected.
        Assert.Equal(-2, first.ZIndex);
        Assert.Equal(-1, second.ZIndex);
    }

    [Fact]
    public async Task UndoAfterBringToFrontRestoresThePriorZIndex()
    {
        var board = new Board();
        var target = AddInstance(board, 0, zIndex: 2);
        AddInstance(board, 100, zIndex: 5);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        Select(canvas, 0);
        await canvas.InvokeAsync(() => canvas.Instance.OnBringToFrontPressed());

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal(2, target.ZIndex);
    }

    [Fact]
    public async Task RedoAfterUndoingALayeringCommandReappliesIt()
    {
        var board = new Board();
        var target = AddInstance(board, 0, zIndex: 2);
        AddInstance(board, 100, zIndex: 5);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        Select(canvas, 0);
        await canvas.InvokeAsync(() => canvas.Instance.OnBringToFrontPressed());
        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        await canvas.InvokeAsync(() => canvas.Instance.OnRedoPressed());

        Assert.Equal(6, target.ZIndex);
    }

    [Fact]
    public async Task UndoOfAMultiSelectionLayeringCommandRestoresEveryMembersPriorZIndex()
    {
        var board = new Board();
        var first = AddInstance(board, 0, zIndex: 2);
        var second = AddInstance(board, 100, zIndex: 3);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        SelectBoth(canvas);
        await canvas.InvokeAsync(() => canvas.Instance.OnBringToFrontPressed());

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal(2, first.ZIndex);
        Assert.Equal(3, second.ZIndex);
    }

    [Fact]
    public async Task StackingChangeRendersImmediatelyInTheContainersZIndexStyle()
    {
        var board = new Board();
        var target = AddInstance(board, 0, zIndex: 2);
        AddInstance(board, 100, zIndex: 5);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        Select(canvas, 0);

        await canvas.InvokeAsync(() => canvas.Instance.OnBringToFrontPressed());

        var style = canvas.FindAll(".component-container")[0].GetAttribute("style");
        Assert.Contains($"z-index: {target.ZIndex}", style);
    }
}
