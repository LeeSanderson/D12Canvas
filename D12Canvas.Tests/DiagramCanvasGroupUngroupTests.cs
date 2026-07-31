using System.Threading.Tasks;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// Ctrl+G promotes a 2+ selection into a persistent Group entity, which becomes the new selection;
// Ctrl+Shift+G dissolves it back. Once a Group exists, clicking any one of its members selects
// the whole group as a unit - selection and group membership converge.
public class DiagramCanvasGroupUngroupTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasGroupUngroupTests()
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

    private static void SelectBoth(IRenderedComponent<DiagramCanvas> canvas)
    {
        var containers = canvas.FindAll(".component-container");
        containers[0].Click();
        containers[1].Click(new MouseEventArgs { ShiftKey = true });
    }

    [Fact]
    public async Task GroupPressedWithTwoSelectedPromotesTheSelectionIntoAGroupThatBecomesTheSelection()
    {
        var board = new Board();
        var first = AddInstance(board, 0);
        var second = AddInstance(board, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        SelectBoth(canvas);

        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        var group = Assert.Single(board.Groups);
        Assert.Equal(new HashSet<Guid> { first.Id, second.Id }, group.MemberIds.ToHashSet());

        var containers = canvas.FindAll(".component-container");
        Assert.Equal("true", containers[0].GetAttribute("aria-selected"));
        Assert.Equal("true", containers[1].GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task GroupPressedWithFewerThanTwoSelectedIsANoOp()
    {
        var board = new Board();
        AddInstance(board, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Find(".component-container").Click();

        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        Assert.Empty(board.Groups);
    }

    [Fact]
    public async Task GroupPressedWithNoSelectionIsANoOp()
    {
        var board = new Board();
        AddInstance(board, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        Assert.Empty(board.Groups);
    }

    [Fact]
    public async Task ClickingAnyMemberOfAGroupSelectsTheWholeGroup()
    {
        var board = new Board();
        AddInstance(board, 0);
        AddInstance(board, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        SelectBoth(canvas);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());
        canvas.Find(".diagram-canvas").Click();

        // A plain click on just the second member - not shift-click - still selects both, since
        // it now belongs to a Group.
        canvas.FindAll(".component-container")[1].Click();

        var containers = canvas.FindAll(".component-container");
        Assert.Equal("true", containers[0].GetAttribute("aria-selected"));
        Assert.Equal("true", containers[1].GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task UngroupPressedDissolvesTheGroupAndMembersBecomeIndependentlySelectableAgain()
    {
        var board = new Board();
        AddInstance(board, 0);
        AddInstance(board, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        SelectBoth(canvas);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        await canvas.InvokeAsync(() => canvas.Instance.OnUngroupPressed());

        Assert.Empty(board.Groups);

        canvas.Find(".diagram-canvas").Click();
        canvas.FindAll(".component-container")[0].Click();

        var containers = canvas.FindAll(".component-container");
        Assert.Equal("true", containers[0].GetAttribute("aria-selected"));
        Assert.Null(containers[1].GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task UngroupPressedWithNoGroupSelectedIsANoOp()
    {
        var board = new Board();
        AddInstance(board, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Find(".component-container").Click();

        await canvas.InvokeAsync(() => canvas.Instance.OnUngroupPressed());

        Assert.Empty(board.Groups);
    }

    [Fact]
    public async Task UndoAfterGroupingRemovesTheGroup()
    {
        var board = new Board();
        AddInstance(board, 0);
        AddInstance(board, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        SelectBoth(canvas);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Empty(board.Groups);
    }

    [Fact]
    public async Task RedoAfterUndoingAGroupRestoresItUnderTheSameIdAndMemberIds()
    {
        var board = new Board();
        var first = AddInstance(board, 0);
        var second = AddInstance(board, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        SelectBoth(canvas);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());
        var groupId = Assert.Single(board.Groups).Id;
        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        await canvas.InvokeAsync(() => canvas.Instance.OnRedoPressed());

        var restored = Assert.Single(board.Groups);
        Assert.Equal(groupId, restored.Id);
        Assert.Equal(new HashSet<Guid> { first.Id, second.Id }, restored.MemberIds.ToHashSet());
    }

    [Fact]
    public async Task UndoAfterUngroupingRestoresTheGroupAndItsConvergedSelectionBehaviour()
    {
        var board = new Board();
        AddInstance(board, 0);
        AddInstance(board, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        SelectBoth(canvas);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());
        await canvas.InvokeAsync(() => canvas.Instance.OnUngroupPressed());

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Single(board.Groups);

        // Clicking one member alone once again selects both, since the group is back.
        canvas.Find(".diagram-canvas").Click();
        canvas.FindAll(".component-container")[0].Click();
        var containers = canvas.FindAll(".component-container");
        Assert.Equal("true", containers[0].GetAttribute("aria-selected"));
        Assert.Equal("true", containers[1].GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task GroupingASelectionThatAlreadyContainsAGroupNestsIt()
    {
        var board = new Board();
        var first = AddInstance(board, 0);
        var second = AddInstance(board, 100);
        var third = AddInstance(board, 200);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        SelectBoth(canvas);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());
        var innerGroup = Assert.Single(board.Groups);

        // The inner group is now the whole selection; shift-click the third (ungrouped) instance
        // to add it alongside the group.
        canvas.FindAll(".component-container")[2].Click(new MouseEventArgs { ShiftKey = true });

        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        Assert.Equal(2, board.Groups.Count);
        var outerGroup = Assert.Single(board.Groups, g => g.Id != innerGroup.Id);
        Assert.Equal(
            new HashSet<Guid> { innerGroup.Id, third.Id },
            outerGroup.MemberIds.ToHashSet()
        );
        // The inner group survives unchanged, nested inside the outer one.
        Assert.NotNull(board.GetGroup(innerGroup.Id));
    }

    [Fact]
    public async Task UngroupingTheOuterGroupOfANestedPairLeavesTheInnerGroupIntact()
    {
        var board = new Board();
        var first = AddInstance(board, 0);
        var second = AddInstance(board, 100);
        AddInstance(board, 200);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        SelectBoth(canvas);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());
        var innerGroup = Assert.Single(board.Groups);
        canvas.FindAll(".component-container")[2].Click(new MouseEventArgs { ShiftKey = true });
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        // The outer (nesting) group is the whole selection right now - ungroup dissolves only it.
        await canvas.InvokeAsync(() => canvas.Instance.OnUngroupPressed());

        var remaining = Assert.Single(board.Groups);
        Assert.Equal(innerGroup.Id, remaining.Id);
        Assert.Equal(new HashSet<Guid> { first.Id, second.Id }, remaining.MemberIds.ToHashSet());

        // The inner group still converges selection on click; the third instance (a former outer
        // member, never itself grouped) is independently selectable again.
        canvas.Find(".diagram-canvas").Click();
        canvas.FindAll(".component-container")[0].Click();
        var containers = canvas.FindAll(".component-container");
        Assert.Equal("true", containers[0].GetAttribute("aria-selected"));
        Assert.Equal("true", containers[1].GetAttribute("aria-selected"));
        Assert.Null(containers[2].GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task UngroupPressedLeavesANonGroupEntryInAMixedSelectionUntouched()
    {
        var board = new Board();
        AddInstance(board, 0);
        AddInstance(board, 100);
        AddInstance(board, 200);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        SelectBoth(canvas);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());
        Assert.Single(board.Groups);
        // Selection is now the group alone; shift-click the third (ungrouped) instance to select
        // the group *and* a plain, non-grouped instance together.
        canvas.FindAll(".component-container")[2].Click(new MouseEventArgs { ShiftKey = true });

        await canvas.InvokeAsync(() => canvas.Instance.OnUngroupPressed());

        Assert.Empty(board.Groups);
        // Every former member of the dissolved group, plus the untouched loose instance, is still
        // part of the selection right after ungroup.
        var containers = canvas.FindAll(".component-container");
        Assert.Equal("true", containers[0].GetAttribute("aria-selected"));
        Assert.Equal("true", containers[1].GetAttribute("aria-selected"));
        Assert.Equal("true", containers[2].GetAttribute("aria-selected"));

        // And the three are now independently selectable - clicking just the loose instance
        // selects only it.
        canvas.Find(".diagram-canvas").Click();
        canvas.FindAll(".component-container")[2].Click();
        containers = canvas.FindAll(".component-container");
        Assert.Null(containers[0].GetAttribute("aria-selected"));
        Assert.Null(containers[1].GetAttribute("aria-selected"));
        Assert.Equal("true", containers[2].GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task TheSharedBoundingBoxAndResizeHandlesShowForASelectedGroupOfTwoMembers()
    {
        var board = new Board();
        AddInstance(board, 0);
        AddInstance(board, 300);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        SelectBoth(canvas);

        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        Assert.Single(canvas.FindAll(".selection-bounding-box"));
        Assert.Equal(8, canvas.FindAll(".group-resize-handle").Count);
        Assert.Empty(canvas.FindAll(".component-container .resize-handle"));
    }
}
