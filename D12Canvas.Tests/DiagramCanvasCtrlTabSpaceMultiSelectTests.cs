using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// Ctrl+Tab + Space keyboard multi-select: Ctrl+Tab moves DOM focus to the next entity in reading
// order without collapsing the current selection (a one-off suspension of focus-follows-
// selection's own auto-replace), and Space toggles the focused entity's membership - the keyboard
// equivalent of a shift-click, via a different key.
// These tests establish "currently focused" via .Focus() (a real AngleSharp focus event, routing
// through ComponentContainer.HandleFocus/DiagramCanvas.FocusEntity) rather than .Click() - a plain
// click's own DOM-focus side effect goes through a stubbed JS focusElement call in this harness,
// which never fires a real onfocus the way production's real .focus() call does (see
// DiagramCanvasFocusFollowsSelectionTests for the same convention).
public class DiagramCanvasCtrlTabSpaceMultiSelectTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasCtrlTabSpaceMultiSelectTests()
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

    private static ComponentInstance AddInstance(Board board, string text, double x, double y)
    {
        var instance = new ComponentInstance(
            ComponentTypeKey,
            new TestProps(text),
            new Bounds(x, y, 50, 50)
        );
        board.AddComponent(instance);
        return instance;
    }

    [Fact]
    public async Task CtrlTabMovesFocusToTheNextStopWithoutSelectingIt()
    {
        var board = new Board();
        AddInstance(board, "First", x: 0, y: 0);
        AddInstance(board, "Second", x: 100, y: 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.FindAll(".component-container")[0].Focus();

        await canvas.InvokeAsync(() => canvas.Instance.OnCtrlTabPressed());

        var containers = canvas.FindAll(".component-container");
        Assert.Equal("true", containers[0].GetAttribute("aria-selected"));
        Assert.Null(containers[1].GetAttribute("aria-selected"));
        var invocation = Assert.Single(JSInterop.Invocations["focusTabStopAt"]);
        Assert.Equal(1, invocation.Arguments[1]);
    }

    [Fact]
    public async Task CtrlTabWrapsAroundFromTheLastStopToTheFirst()
    {
        var board = new Board();
        AddInstance(board, "First", x: 0, y: 0);
        AddInstance(board, "Second", x: 100, y: 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.FindAll(".component-container")[1].Focus();

        await canvas.InvokeAsync(() => canvas.Instance.OnCtrlTabPressed());

        var invocation = Assert.Single(JSInterop.Invocations["focusTabStopAt"]);
        Assert.Equal(0, invocation.Arguments[1]);
    }

    [Fact]
    public async Task RepeatedCtrlTabAdvancesOneStopAtATimeStartingFromNothingFocused()
    {
        var board = new Board();
        AddInstance(board, "First", x: 0, y: 0);
        AddInstance(board, "Second", x: 100, y: 0);
        AddInstance(board, "Third", x: 200, y: 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        await canvas.InvokeAsync(() => canvas.Instance.OnCtrlTabPressed());
        await canvas.InvokeAsync(() => canvas.Instance.OnCtrlTabPressed());

        var invocations = JSInterop.Invocations["focusTabStopAt"];
        Assert.Equal(2, invocations.Count);
        Assert.Equal(0, invocations[0].Arguments[1]);
        Assert.Equal(1, invocations[1].Arguments[1]);
    }

    [Fact]
    public async Task CtrlTabIsANoOpWhenTheFocusedInstanceIsTheOnlyStop()
    {
        var board = new Board();
        AddInstance(board, "Only", x: 0, y: 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Find(".component-container").Focus();

        await canvas.InvokeAsync(() => canvas.Instance.OnCtrlTabPressed());

        Assert.Empty(JSInterop.Invocations["focusTabStopAt"]);
    }

    [Fact]
    public async Task SpaceAddsTheFocusedInstanceToAnExistingSelection()
    {
        var board = new Board();
        AddInstance(board, "First", x: 0, y: 0);
        AddInstance(board, "Second", x: 100, y: 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.FindAll(".component-container")[0].Focus();
        await canvas.InvokeAsync(() => canvas.Instance.OnCtrlTabPressed());

        await canvas.InvokeAsync(() => canvas.Instance.OnSpacePressed());

        var containers = canvas.FindAll(".component-container");
        Assert.Equal("true", containers[0].GetAttribute("aria-selected"));
        Assert.Equal("true", containers[1].GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task SpaceRemovesAnAlreadySelectedFocusedInstanceFromTheSelection()
    {
        var board = new Board();
        AddInstance(board, "First", x: 0, y: 0);
        AddInstance(board, "Second", x: 100, y: 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var containers = canvas.FindAll(".component-container");
        containers[0].Focus();
        containers[1].Click(new MouseEventArgs { ShiftKey = true });

        await canvas.InvokeAsync(() => canvas.Instance.OnSpacePressed());

        containers = canvas.FindAll(".component-container");
        Assert.Null(containers[0].GetAttribute("aria-selected"));
        Assert.Equal("true", containers[1].GetAttribute("aria-selected"));
    }

    [Fact]
    public void SpaceWithNothingFocusedIsANoOp()
    {
        var board = new Board();
        AddInstance(board, "Only", x: 0, y: 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Instance.OnSpacePressed();

        Assert.Null(canvas.Find(".component-container").GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task ACtrlTabPlusSpaceBuiltMultiSelectionGroupsExactlyLikeAPointerBuiltOne()
    {
        var board = new Board();
        AddInstance(board, "First", x: 0, y: 0);
        AddInstance(board, "Second", x: 100, y: 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.FindAll(".component-container")[0].Focus();
        await canvas.InvokeAsync(() => canvas.Instance.OnCtrlTabPressed());
        await canvas.InvokeAsync(() => canvas.Instance.OnSpacePressed());

        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        var group = Assert.Single(board.Groups);
        Assert.Equal(board.Components.Select(c => c.Id).ToHashSet(), group.MemberIds.ToHashSet());
    }

    [Fact]
    public async Task ACtrlTabPlusSpaceBuiltMultiSelectionMovesAsOneUnit()
    {
        var board = new Board();
        var first = AddInstance(board, "First", x: 0, y: 0);
        var second = AddInstance(board, "Second", x: 100, y: 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.FindAll(".component-container")[0].Focus();
        await canvas.InvokeAsync(() => canvas.Instance.OnCtrlTabPressed());
        await canvas.InvokeAsync(() => canvas.Instance.OnSpacePressed());

        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed("ArrowRight", false));

        Assert.Equal(1, board.GetComponent(first.Id)!.Bounds.X);
        Assert.Equal(101, board.GetComponent(second.Id)!.Bounds.X);
    }

    [Fact]
    public async Task ACtrlTabPlusSpaceBuiltMultiSelectionDeletesBothMembers()
    {
        var board = new Board();
        var first = AddInstance(board, "First", x: 0, y: 0);
        var second = AddInstance(board, "Second", x: 100, y: 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.FindAll(".component-container")[0].Focus();
        await canvas.InvokeAsync(() => canvas.Instance.OnCtrlTabPressed());
        await canvas.InvokeAsync(() => canvas.Instance.OnSpacePressed());

        await canvas.InvokeAsync(() => canvas.Instance.OnDeletePressed());

        Assert.Null(board.GetComponent(first.Id));
        Assert.Null(board.GetComponent(second.Id));
    }

    [Fact]
    public async Task SpaceAfterCtrlTabbingToAGroupsOwnStopTogglesTheWholeGroup()
    {
        var board = new Board();
        AddInstance(board, "First", x: 0, y: 0);
        AddInstance(board, "Second", x: 100, y: 0);
        AddInstance(board, "Third", x: 200, y: 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var containers = canvas.FindAll(".component-container");
        containers[0].Click();
        containers[1].Click(new MouseEventArgs { ShiftKey = true });
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());
        canvas.Find(".diagram-canvas").Click(); // clear the selection first
        canvas.FindAll(".component-container")[2].Focus();

        // Reading order: group-tab-stop (bounds start at First's origin), then Third.
        await canvas.InvokeAsync(() => canvas.Instance.OnCtrlTabPressed());
        await canvas.InvokeAsync(() => canvas.Instance.OnSpacePressed());

        Assert.Equal("true", canvas.Find(".group-tab-stop").GetAttribute("aria-selected"));
        containers = canvas.FindAll(".component-container");
        Assert.Equal("true", containers[0].GetAttribute("aria-selected"));
        Assert.Equal("true", containers[1].GetAttribute("aria-selected"));
        Assert.Equal("true", containers[2].GetAttribute("aria-selected"));
    }
}
