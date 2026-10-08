using AngleSharp.Dom;
using Bunit;
using D12Canvas.BuiltIns;
using D12Canvas.History;
using D12Canvas.Model;
using D12Canvas.Persistence;
using D12Canvas.Pointer;
using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// A locked entity takes no primary press, no marquee and no command, a secondary press reaches it
// and offers Unlock, the keyboard still lands on it, and a copy of it is locked. An operation on a
// partly locked group acts on its unlocked members.
public class DiagramCanvasLockingTests : ComponentTestBase
{
    public DiagramCanvasLockingTests()
    {
        SetupDiagramCanvasJsModule();
        var options = new D12CanvasOptions();
        BuiltInComponents.RegisterAll(options);
        Services.AddSingleton(options.Registry);
    }

    private static ComponentInstance AddShape(
        Board board,
        double x,
        double y = 0,
        bool locked = false,
        double width = 100,
        double height = 50
    )
    {
        var instance = new ComponentInstance(
            "rectangle",
            new RectangleProps(null, null, 2),
            new Bounds(x, y, width, height),
            locked: locked
        );
        board.AddComponent(instance);
        return instance;
    }

    private static Group AddGroup(Board board, params ComponentInstance[] members)
    {
        var group = new Group(members.Select(member => member.Id).ToList());
        board.AddGroup(group);
        return group;
    }

    private IRenderedComponent<DiagramCanvas> RenderCanvas(Board board) =>
        Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

    private static Task Run(
        IRenderedComponent<DiagramCanvas> canvas,
        Action<DiagramCanvas> action
    ) => canvas.InvokeAsync(() => action(canvas.Instance));

    private static async Task RightClick(IRenderedComponent<DiagramCanvas> canvas, Guid entityId)
    {
        await canvas.Press(
            10,
            10,
            PointerPress.SecondaryButton,
            role: HitRole.Instance,
            entityId: entityId
        );
        await canvas.Release(10, 10, PointerPress.SecondaryButton);
    }

    private static async Task RightClickEdge(IRenderedComponent<DiagramCanvas> canvas, Edge edge)
    {
        await canvas.Press(
            10,
            10,
            PointerPress.SecondaryButton,
            role: HitRole.Edge,
            entityId: edge.Id
        );
        await canvas.Release(10, 10, PointerPress.SecondaryButton);
    }

    private static async Task RightClickCanvas(IRenderedComponent<DiagramCanvas> canvas)
    {
        await canvas.Press(700, 500, PointerPress.SecondaryButton);
        await canvas.Release(700, 500, PointerPress.SecondaryButton);
    }

    private static async Task PrimaryClick(IRenderedComponent<DiagramCanvas> canvas, Guid entityId)
    {
        await canvas.Press(10, 10, role: HitRole.Instance, entityId: entityId);
        await canvas.Release(10, 10);
    }

    private static string[] MenuLabels(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.FindAll(".d12-context-menu-label").Select(label => label.TextContent).ToArray();

    private static IElement MenuRow(IRenderedComponent<DiagramCanvas> canvas, string label) =>
        canvas
            .FindAll(".d12-context-menu-item")
            .Single(item => item.QuerySelector(".d12-context-menu-label")!.TextContent == label);

    private static HashSet<Guid> SelectedIds(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.Instance.SelectedComponents.Select(instance => instance.Id).ToHashSet();

    [Fact]
    public async Task TheChordLocksTheSelectionAgainUnlocksItAndUndoReversesEach()
    {
        var board = new Board();
        var shape = AddShape(board, 0);
        var canvas = RenderCanvas(board);
        await PrimaryClick(canvas, shape.Id);

        await Run(canvas, c => c.OnToggleLockPressed());
        Assert.True(shape.Locked);

        await Run(canvas, c => c.OnToggleLockPressed());
        Assert.False(shape.Locked);

        await Run(canvas, c => c.OnUndoPressed());
        Assert.True(shape.Locked);

        await Run(canvas, c => c.OnUndoPressed());
        Assert.False(shape.Locked);
    }

    [Fact]
    public async Task APrimaryPressOnALockedShapeSelectsNothingAndItsDragMovesNothing()
    {
        var board = new Board();
        var locked = AddShape(board, 0, locked: true);
        var canvas = RenderCanvas(board);

        await PrimaryClick(canvas, locked.Id);
        Assert.Empty(SelectedIds(canvas));

        await canvas.Press(10, 10, role: HitRole.Instance, entityId: locked.Id);
        await canvas.Move(300, 300);
        await canvas.Release(300, 300);

        Assert.Equal(new Bounds(0, 0, 100, 50), locked.Bounds);
        Assert.Empty(SelectedIds(canvas));
    }

    [Fact]
    public async Task AMarqueeAndSelectAllLeaveLockedEntitiesOut()
    {
        var board = new Board();
        var free = AddShape(board, 0);
        AddShape(board, 200, locked: true);
        var lockedGroup = AddGroup(
            board,
            AddShape(board, 400, locked: true),
            AddShape(board, 600, locked: true)
        );
        var edge = new Edge(new FloatingEndpoint(0, 200), new FloatingEndpoint(50, 200))
        {
            Locked = true,
        };
        board.AddEdge(edge);
        var canvas = RenderCanvas(board);

        await canvas.Marquee((-10, -10), (790, 590));
        Assert.Equal([free.Id], SelectedIds(canvas));
        Assert.Empty(canvas.Instance.SelectedEdges);

        await Run(canvas, c => c.OnSelectAllPressed());
        Assert.Equal([free.Id], SelectedIds(canvas));
        Assert.Empty(canvas.Instance.SelectedEdges);
        Assert.DoesNotContain(lockedGroup.MemberIds[0], SelectedIds(canvas));
    }

    [Fact]
    public async Task ALockedShapeSelectedByRightClickIsNotNudgedDeletedOrResized()
    {
        var board = new Board();
        var locked = AddShape(board, 0, locked: true);
        var canvas = RenderCanvas(board);
        await RightClick(canvas, locked.Id);
        await Run(canvas, c => c.OnEscapePressed());
        Assert.Equal([locked.Id], SelectedIds(canvas));

        await Run(canvas, c => c.OnArrowKeyPressed("ArrowRight", shiftKey: false));
        await Run(canvas, c => c.OnAltArrowKeyPressed("ArrowRight", shiftKey: false));
        await Run(canvas, c => c.OnDeletePressed());

        Assert.Equal(new Bounds(0, 0, 100, 50), locked.Bounds);
        Assert.Same(locked, board.GetComponent(locked.Id));
        Assert.Equal([locked.Id], SelectedIds(canvas));
        Assert.Empty(canvas.FindAll(".resize-handle"));
        Assert.Empty(canvas.FindAll(".port-span"));
    }

    [Fact]
    public async Task ARightClickOnALockedShapeSelectsItAndItsUnlockRowUnlocksIt()
    {
        var board = new Board();
        var locked = AddShape(board, 0, locked: true);
        var canvas = RenderCanvas(board);

        await RightClick(canvas, locked.Id);

        Assert.Equal([locked.Id], SelectedIds(canvas));
        Assert.Contains("Unlock", MenuLabels(canvas));
        Assert.DoesNotContain("Lock", MenuLabels(canvas));
        Assert.DoesNotContain("Delete", MenuLabels(canvas));

        MenuRow(canvas, "Unlock").Click();

        Assert.False(locked.Locked);
    }

    [Fact]
    public async Task TheLockRowLocksAnUnlockedShape()
    {
        var board = new Board();
        var shape = AddShape(board, 0);
        var canvas = RenderCanvas(board);

        await RightClick(canvas, shape.Id);
        MenuRow(canvas, "Lock").Click();

        Assert.True(shape.Locked);
    }

    [Fact]
    public async Task APartlyLockedGroupReadsLockAndLockingItLocksTheRest()
    {
        var board = new Board();
        var free = AddShape(board, 0);
        var locked = AddShape(board, 200, locked: true);
        AddGroup(board, free, locked);
        var canvas = RenderCanvas(board);

        await RightClick(canvas, free.Id);
        Assert.Contains("Lock", MenuLabels(canvas));

        MenuRow(canvas, "Lock").Click();
        Assert.True(free.Locked);

        await RightClick(canvas, free.Id);
        Assert.Contains("Unlock", MenuLabels(canvas));
    }

    [Fact]
    public async Task UnlockAllUnlocksEverythingInOneEntryAndIsAbsentWithNothingLocked()
    {
        var board = new Board();
        var first = AddShape(board, 0, locked: true);
        var second = AddShape(board, 200, locked: true);
        var edge = new Edge(new FloatingEndpoint(0, 200), new FloatingEndpoint(50, 200))
        {
            Locked = true,
        };
        board.AddEdge(edge);
        var canvas = RenderCanvas(board);

        await RightClickCanvas(canvas);
        MenuRow(canvas, "Unlock All").Click();

        Assert.False(first.Locked || second.Locked || edge.Locked);

        await RightClickCanvas(canvas);
        Assert.DoesNotContain("Unlock All", MenuLabels(canvas));

        await Run(canvas, c => c.OnUndoPressed());
        Assert.True(first.Locked && second.Locked && edge.Locked);
    }

    [Fact]
    public async Task TabbingToALockedShapeSelectsIt()
    {
        var board = new Board();
        var locked = AddShape(board, 0, locked: true);
        var canvas = RenderCanvas(board);

        canvas.Find(".component-container").Focus();

        Assert.Equal([locked.Id], SelectedIds(canvas));
        Assert.Equal("0", canvas.Find(".component-container").GetAttribute("tabindex"));
    }

    [Fact]
    public async Task DraggingAGroupWithALockedMemberMovesTheOthersAndLeavesItInPlace()
    {
        var board = new Board();
        var free = AddShape(board, 0);
        var locked = AddShape(board, 200, locked: true);
        AddGroup(board, free, locked);
        var canvas = RenderCanvas(board);

        await canvas.Press(10, 10, role: HitRole.Instance, entityId: free.Id);
        await canvas.Move(40, 70);
        await canvas.Release(40, 70);

        Assert.Equal(new Bounds(30, 60, 100, 50), free.Bounds);
        Assert.Equal(new Bounds(200, 0, 100, 50), locked.Bounds);
    }

    [Fact]
    public async Task CloneDraggingAPartlyLockedGroupCopiesItWholeWithTheLockedMembersCopyLocked()
    {
        var board = new Board();
        var free = AddShape(board, 0);
        var locked = AddShape(board, 200, locked: true);
        var group = AddGroup(board, free, locked);
        var canvas = RenderCanvas(board);

        await canvas.Press(10, 10, role: HitRole.Instance, entityId: free.Id);
        await canvas.Move(10, 210, alt: true);
        await canvas.Release(10, 210);

        var copies = board
            .Components.Where(instance => instance.Id != free.Id && instance.Id != locked.Id)
            .ToList();
        Assert.Equal(2, copies.Count);
        var lockedCopy = Assert.Single(copies, copy => copy.Locked);
        Assert.Equal(new Bounds(200, 200, 100, 50), lockedCopy.Bounds);
        Assert.Equal(2, board.Groups.Count);
        Assert.Equal(new Bounds(200, 0, 100, 50), locked.Bounds);
        Assert.Same(group, board.GetGroup(group.Id));
    }

    [Fact]
    public async Task AFullyLockedGroupsSelectionBoxTakesNoPressAndShowsNoHandles()
    {
        var board = new Board();
        var first = AddShape(board, 0, locked: true);
        AddGroup(board, first, AddShape(board, 200, locked: true));
        var canvas = RenderCanvas(board);

        await RightClick(canvas, first.Id);

        var box = canvas.Find(".selection-bounding-box");
        Assert.Null(box.GetAttribute("data-d12-role"));
        Assert.Contains("locked", box.ClassList);
        Assert.All(
            canvas.FindAll(".group-resize-handle"),
            handle => Assert.Null(handle.GetAttribute("data-d12-role"))
        );
    }

    [Fact]
    public async Task APartlyLockedGroupsSelectionBoxKeepsItsHandles()
    {
        var board = new Board();
        var free = AddShape(board, 0);
        AddGroup(board, free, AddShape(board, 200, locked: true));
        var canvas = RenderCanvas(board);

        await PrimaryClick(canvas, free.Id);

        var box = canvas.Find(".selection-bounding-box");
        Assert.Equal(HitRole.SelectionBounds, box.GetAttribute("data-d12-role"));
        Assert.DoesNotContain("locked", box.ClassList);
    }

    [Fact]
    public async Task NudgingAGroupWithALockedMemberMovesOnlyTheOthers()
    {
        var board = new Board();
        var free = AddShape(board, 0);
        var locked = AddShape(board, 200, locked: true);
        AddGroup(board, free, locked);
        var canvas = RenderCanvas(board);
        await PrimaryClick(canvas, free.Id);

        await Run(canvas, c => c.OnArrowKeyPressed("ArrowDown", shiftKey: true));

        Assert.Equal(10, free.Bounds.Y);
        Assert.Equal(0, locked.Bounds.Y);
    }

    [Fact]
    public async Task ResizingAGroupWithALockedMemberScalesTheOthersInsideTheRealBounds()
    {
        var board = new Board();
        var free = AddShape(board, 0, width: 100, height: 100);
        var locked = AddShape(board, 100, width: 100, height: 100, locked: true);
        AddGroup(board, free, locked);
        var canvas = RenderCanvas(board);
        await PrimaryClick(canvas, free.Id);

        await canvas.Press(200, 50, role: HitRole.SelectionHandle, part: "right");
        await canvas.Move(400, 50);

        Assert.Contains(
            "width: 400px",
            canvas.Find(".selection-bounding-box").GetAttribute("style")
        );

        await canvas.Release(400, 50);

        Assert.Equal(new Bounds(0, 0, 200, 100), free.Bounds);
        Assert.Equal(new Bounds(100, 0, 100, 100), locked.Bounds);
    }

    [Fact]
    public async Task AligningASelectionWithAPartlyLockedGroupMeasuresItsUnlockedMembers()
    {
        var board = new Board();
        var lockedLeft = AddShape(board, 0, locked: true);
        var member = AddShape(board, 100, 100);
        AddGroup(board, lockedLeft, member);
        var other = AddShape(board, 300, 200);
        var canvas = RenderCanvas(board);
        await Run(canvas, c => c.OnSelectAllPressed());

        await Run(canvas, c => c.OnAlignLeftPressed());

        Assert.Equal(100, member.Bounds.X);
        Assert.Equal(100, other.Bounds.X);
        Assert.Equal(0, lockedLeft.Bounds.X);
    }

    [Fact]
    public async Task AFullyLockedGroupDoesNotCountTowardTheAlignThreshold()
    {
        var board = new Board();
        var lockedGroup = AddGroup(
            board,
            AddShape(board, 0, locked: true),
            AddShape(board, 0, 100, locked: true)
        );
        var free = AddShape(board, 300, 200);
        var canvas = RenderCanvas(board);
        await RightClick(canvas, lockedGroup.MemberIds[0]);
        await Run(canvas, c => c.OnEscapePressed());
        await canvas.Press(10, 10, shift: true, role: HitRole.Instance, entityId: free.Id);
        await canvas.Release(10, 10);
        Assert.Equal(3, SelectedIds(canvas).Count);

        await RightClick(canvas, free.Id);

        Assert.Empty(canvas.FindAll(".d12-context-menu-glyph"));
    }

    [Fact]
    public async Task DeletingAPartlyLockedGroupRemovesTheUnlockedMembersAndTheGroupRepairs()
    {
        var board = new Board();
        var free = AddShape(board, 0);
        var locked = AddShape(board, 200, locked: true);
        var group = AddGroup(board, free, locked);
        var canvas = RenderCanvas(board);
        await PrimaryClick(canvas, free.Id);

        await Run(canvas, c => c.OnDeletePressed());

        Assert.Null(board.GetComponent(free.Id));
        Assert.Same(locked, board.GetComponent(locked.Id));
        Assert.Null(board.GetGroup(group.Id));

        await Run(canvas, c => c.OnUndoPressed());

        Assert.NotNull(board.GetComponent(free.Id));
        Assert.NotNull(board.GetGroup(group.Id));
    }

    [Fact]
    public async Task DeletingAFullyLockedGroupLeavesItUntouched()
    {
        var board = new Board();
        var first = AddShape(board, 0, locked: true);
        var second = AddShape(board, 200, locked: true);
        var group = AddGroup(board, first, second);
        var canvas = RenderCanvas(board);
        await RightClick(canvas, first.Id);
        await Run(canvas, c => c.OnEscapePressed());

        await Run(canvas, c => c.OnDeletePressed());

        Assert.Equal(2, board.Components.Count);
        Assert.NotNull(board.GetGroup(group.Id));
        Assert.Equal(2, canvas.Instance.SelectedComponents.Count);
    }

    [Fact]
    public async Task CopyDuplicateAndPasteOfALockedShapeProduceALockedCopy()
    {
        var board = new Board();
        var locked = AddShape(board, 0, locked: true);
        var canvas = RenderCanvas(board);
        await RightClick(canvas, locked.Id);
        await Run(canvas, c => c.OnEscapePressed());

        await Run(canvas, c => c.OnDuplicatePressed());
        var duplicate = board.Components.Single(instance => instance.Id != locked.Id);
        Assert.True(duplicate.Locked);
        Assert.Equal(new Bounds(20, 20, 100, 50), duplicate.Bounds);

        var payload = await canvas.InvokeAsync(() => canvas.Instance.OnCopyRequested());
        await canvas.InvokeAsync(() => canvas.Instance.OnPasteReceived(payload!, 400, 300));

        Assert.Equal(3, board.Components.Count);
        Assert.All(board.Components, instance => Assert.True(instance.Locked));
    }

    [Fact]
    public async Task CutWithOnlyLockedEntitiesSelectedIsIneligible()
    {
        ReportPlatform(applePlatform: false, asyncClipboard: true);
        var board = new Board();
        var locked = AddShape(board, 0, locked: true);
        var canvas = RenderCanvas(board);
        await RightClick(canvas, locked.Id);

        Assert.Contains("Copy", MenuLabels(canvas));
        Assert.Contains("Duplicate", MenuLabels(canvas));
        Assert.DoesNotContain("Cut", MenuLabels(canvas));

        await Run(canvas, c => c.OnEscapePressed());
        var payload = await canvas.InvokeAsync(() => canvas.Instance.OnCutRequested());

        Assert.Null(payload);
        Assert.Same(locked, board.GetComponent(locked.Id));
    }

    [Fact]
    public async Task CuttingAPartlyLockedGroupCarriesOnlyWhatItsDeleteRemoves()
    {
        var board = new Board();
        var free = AddShape(board, 0);
        var locked = AddShape(board, 200, locked: true);
        AddGroup(board, free, locked);
        var canvas = RenderCanvas(board);
        await PrimaryClick(canvas, free.Id);

        var payload = await canvas.InvokeAsync(() => canvas.Instance.OnCutRequested());

        var carried = new BoardJsonSerializer(
            Services.GetRequiredService<IComponentRegistry>()
        ).Deserialize(payload!);
        Assert.Equal([free.Id], carried.Components.Select(instance => instance.Id));
        Assert.Empty(carried.Groups);
        Assert.Null(board.GetComponent(free.Id));
        Assert.Same(locked, board.GetComponent(locked.Id));
    }

    [Fact]
    public async Task ALockedEdgeIsNotRepositionedRestyledOrRelabelled()
    {
        var board = new Board();
        var edge = new Edge(new FloatingEndpoint(0, 0), new FloatingEndpoint(100, 0))
        {
            Locked = true,
        };
        board.AddEdge(edge);
        var canvas = RenderCanvas(board);

        await canvas.Press(100, 0, role: HitRole.EdgeEndpoint, entityId: edge.Id, part: "target");
        await canvas.Move(300, 300);
        await canvas.Release(300, 300);
        var style = new EdgeStyle(edge.RoutingStyle, edge.SourceArrow, edge.TargetArrow);
        await Run(
            canvas,
            c => c.CommitEdgeStyleChange(edge.Id, style, style with { Color = "#ff0000" })
        );
        await Run(
            canvas,
            c =>
                c.OnPointerPressed(
                    PointerEvents.Press(
                        HitRole.Edge,
                        PointerPress.PrimaryButton,
                        50,
                        0,
                        edge.Id,
                        pressCount: 2
                    )
                )
        );
        await canvas.Release(50, 0);

        Assert.Equal(new FloatingEndpoint(100, 0), edge.Target);
        Assert.Null(edge.Color);
        Assert.Null(edge.Label);
        Assert.Empty(canvas.Instance.SelectedEdges);
    }

    [Fact]
    public async Task ARightClickOnALockedEdgeSelectsItAndOffersUnlock()
    {
        var board = new Board();
        var edge = new Edge(new FloatingEndpoint(0, 0), new FloatingEndpoint(100, 0))
        {
            Locked = true,
        };
        board.AddEdge(edge);
        var canvas = RenderCanvas(board);

        await RightClickEdge(canvas, edge);

        Assert.Equal([edge], canvas.Instance.SelectedEdges);
        Assert.Contains("Unlock", MenuLabels(canvas));
    }

    [Fact]
    public async Task ALockedShapeIsMarkedForTheListenerAndKeepsItsHitRole()
    {
        var board = new Board();
        AddShape(board, 0, locked: true);
        var canvas = RenderCanvas(board);

        var container = canvas.Find(".component-container");

        Assert.Equal(HitRole.Instance, container.GetAttribute("data-d12-role"));
        Assert.Equal("true", container.GetAttribute("data-d12-locked"));
    }

    [Fact]
    public async Task APropsEditOfALockedShapeCommitsNothing()
    {
        var board = new Board();
        var locked = AddShape(board, 0, locked: true);
        var before = locked.Props;
        var canvas = RenderCanvas(board);

        await Run(
            canvas,
            c => c.CommitPropsChange(locked.Id, before, new RectangleProps("#ff0000", null, 2))
        );

        Assert.Same(before, locked.Props);
    }
}
