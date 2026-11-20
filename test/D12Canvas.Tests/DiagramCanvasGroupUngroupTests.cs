using System.Threading.Tasks;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

public class DiagramCanvasGroupUngroupTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasGroupUngroupTests()
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

    private static void SelectBoth(IRenderedComponent<DiagramCanvas> canvas)
    {
        var containers = canvas.FindAll(".component-container");
        canvas.ClickOn(containers[0]);
        canvas.ClickOn(containers[1], shift: true);
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
        canvas.ClickOn(canvas.Find(".component-container"));

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
        await canvas.ClickCanvas(400, 400);

        canvas.ClickOn(canvas.FindAll(".component-container")[1]);

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

        await canvas.ClickCanvas(400, 400);
        canvas.ClickOn(canvas.FindAll(".component-container")[0]);

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
        canvas.ClickOn(canvas.Find(".component-container"));

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

        await canvas.ClickCanvas(400, 400);
        canvas.ClickOn(canvas.FindAll(".component-container")[0]);
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

        canvas.ClickOn(canvas.FindAll(".component-container")[2], shift: true);

        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        Assert.Equal(2, board.Groups.Count);
        var outerGroup = Assert.Single(board.Groups, g => g.Id != innerGroup.Id);
        Assert.Equal(
            new HashSet<Guid> { innerGroup.Id, third.Id },
            outerGroup.MemberIds.ToHashSet()
        );
        Assert.NotNull(board.GetGroup(innerGroup.Id));
    }

    [Fact]
    public void AGroupsAccessibleLabelCountsOnlyTheMembersThatResolve()
    {
        var board = new Board();
        var onlyLiveMember = AddInstance(board, 0);
        board.AddGroup(new Group([onlyLiveMember.Id, Guid.NewGuid()]));

        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        Assert.Equal("Group (1 items)", canvas.Find(".group-tab-stop").GetAttribute("aria-label"));
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
        canvas.ClickOn(canvas.FindAll(".component-container")[2], shift: true);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        await canvas.InvokeAsync(() => canvas.Instance.OnUngroupPressed());

        var remaining = Assert.Single(board.Groups);
        Assert.Equal(innerGroup.Id, remaining.Id);
        Assert.Equal(new HashSet<Guid> { first.Id, second.Id }, remaining.MemberIds.ToHashSet());

        await canvas.ClickCanvas(400, 400);
        canvas.ClickOn(canvas.FindAll(".component-container")[0]);
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
        canvas.ClickOn(canvas.FindAll(".component-container")[2], shift: true);

        await canvas.InvokeAsync(() => canvas.Instance.OnUngroupPressed());

        Assert.Empty(board.Groups);
        var containers = canvas.FindAll(".component-container");
        Assert.Equal("true", containers[0].GetAttribute("aria-selected"));
        Assert.Equal("true", containers[1].GetAttribute("aria-selected"));
        Assert.Equal("true", containers[2].GetAttribute("aria-selected"));

        await canvas.ClickCanvas(400, 400);
        canvas.ClickOn(canvas.FindAll(".component-container")[2]);
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
