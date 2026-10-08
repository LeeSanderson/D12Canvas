using Bunit;
using D12Canvas.Model;
using D12Canvas.Pointer;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// A multi-selection (2+) moves and resizes as a single bounding-box unit. The bounding box is
// computed over every selected member and rendered (with its own resize handles) whenever 2+ are
// selected; individual members' own resize handles are suppressed for that same reason
// (DiagramCanvasResizeTests/ComponentContainerTests cover their single-select case). A move can
// start either by dragging one of the selected members directly, or by dragging the bounding box
// itself, which is the hit target anywhere inside the selection's bounds - either way every
// member moves by the same delta, and the whole gesture (move or resize) commits to Board exactly
// once, on release.
public class DiagramCanvasMultiSelectionMoveResizeTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasMultiSelectionMoveResizeTests()
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
        double width = 50,
        double height = 50
    )
    {
        var instance = new ComponentInstance(
            ComponentTypeKey,
            new TestProps(),
            new Bounds(x, y, width, height)
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
    public void TheBoundingBoxIsNotShownForASingleSelection()
    {
        var board = new Board();
        AddInstance(board, 0, 0);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        canvas.ClickOn(canvas.Find(".component-container"));

        Assert.Empty(canvas.FindAll(".selection-bounding-box"));
    }

    [Fact]
    public void TheBoundingBoxMatchesTheUnionOfMemberBoundsWhenTwoAreSelected()
    {
        var board = new Board();
        AddInstance(board, 0, 0);
        AddInstance(board, 300, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        SelectBoth(canvas);

        var style = canvas.Find(".selection-bounding-box").GetAttribute("style");
        Assert.Contains("left: 0px", style);
        Assert.Contains("top: 0px", style);
        Assert.Contains("width: 350px", style);
        Assert.Contains("height: 150px", style);
    }

    [Fact]
    public void DraggingASelectedMemberMovesEveryMemberByTheSameDelta()
    {
        var board = new Board();
        var first = AddInstance(board, 100, 100);
        var second = AddInstance(board, 300, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        SelectBoth(canvas);

        var containers = canvas.FindAll(".component-container");
        canvas.DragOn(containers[0], (300, 200), (340, 175));

        Assert.Equal(new Bounds(140, 75, 50, 50), first.Bounds);
        Assert.Equal(new Bounds(340, 75, 50, 50), second.Bounds);
    }

    [Fact]
    public void DraggingEmptySpaceWithinTheBoundingBoxMovesTheWholeSelectionAndDoesNotClearItAfterwards()
    {
        var board = new Board();
        var first = AddInstance(board, 0, 0);
        var second = AddInstance(board, 300, 0);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        SelectBoth(canvas);

        // (150, 25) sits inside the combined bounding box (0,0)-(350,50) but isn't over either
        // instance; the box itself is the hit target there.
        canvas.PressOn(
            canvas.Find(".selection-bounding-box"),
            (150, 25),
            role: HitRole.SelectionBounds
        );
        canvas.MoveTo((200, 75));
        canvas.ReleaseAt((200, 75));

        Assert.Equal(new Bounds(50, 50, 50, 50), first.Bounds);
        Assert.Equal(new Bounds(350, 50, 50, 50), second.Bounds);

        var containers = canvas.FindAll(".component-container");
        Assert.Equal("true", containers[0].GetAttribute("aria-selected"));
        Assert.Equal("true", containers[1].GetAttribute("aria-selected"));
    }

    [Fact]
    public void TheBoardIsUnchangedMidGroupMoveAndOnlyUpdatesOnRelease()
    {
        var board = new Board();
        var first = AddInstance(board, 0, 0);
        var second = AddInstance(board, 300, 0);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        SelectBoth(canvas);

        canvas.PressOn(
            canvas.Find(".selection-bounding-box"),
            (150, 25),
            role: HitRole.SelectionBounds
        );
        canvas.MoveTo((200, 75));

        // Mid-drag: this isn't a pan, and no marquee was drawn - the whole gesture instead lives
        // in the gesture preview each member's own style reflects; Board's own
        // Bounds haven't been touched yet.
        Assert.Empty(canvas.FindAll(".marquee-select"));
        Assert.Contains(
            "translate(0px, 0px)",
            canvas.Find(".canvas-content").GetAttribute("style")
        );
        var containers = canvas.FindAll(".component-container");
        Assert.Contains("left: 50px", containers[0].GetAttribute("style"));
        Assert.Contains("top: 50px", containers[0].GetAttribute("style"));
        Assert.Contains("left: 350px", containers[1].GetAttribute("style"));
        Assert.Contains("top: 50px", containers[1].GetAttribute("style"));
        Assert.Equal(new Bounds(0, 0, 50, 50), first.Bounds);
        Assert.Equal(new Bounds(300, 0, 50, 50), second.Bounds);

        canvas.ReleaseAt((200, 75));

        Assert.Equal(new Bounds(50, 50, 50, 50), first.Bounds);
        Assert.Equal(new Bounds(350, 50, 50, 50), second.Bounds);
    }

    [Fact]
    public void GroupMoveViaEmptySpaceScalesTheScreenDeltaByTheCurrentZoom()
    {
        var board = new Board();
        var first = AddInstance(board, 0, 0);
        var second = AddInstance(board, 300, 0);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        canvas.ZoomIn(); // zooms to scale 1.1
        SelectBoth(canvas);

        canvas.PressOn(
            canvas.Find(".selection-bounding-box"),
            (150, 25),
            role: HitRole.SelectionBounds
        );
        canvas.MoveTo((194, 47));
        canvas.ReleaseAt((194, 47));

        // Computed with the same arithmetic ZoomPanTracker uses (1.0 + 0.1), rather than the
        // decimal literal 1.1, so this can't disagree with production code over double rounding.
        var scale = 1.0 + 0.1;
        Assert.Equal(44 / scale, first.Bounds.X, precision: 10);
        Assert.Equal(22 / scale, first.Bounds.Y, precision: 10);
        Assert.Equal(300 + 44 / scale, second.Bounds.X, precision: 10);
        Assert.Equal(22 / scale, second.Bounds.Y, precision: 10);
    }

    [Fact]
    public void ResizingViaTheBottomRightHandleScalesEveryMemberProportionally()
    {
        var board = new Board();
        var first = AddInstance(board, 0, 0, 50, 50);
        var second = AddInstance(board, 100, 0, 100, 50);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        SelectBoth(canvas);

        // Combined bbox starts at (0,0,200,50). Growing it to (0,0,300,100) scales x1.5/x2.
        canvas.DragHandle(canvas.Find(".group-resize-handle.bottom-right"), (300, 200), (400, 250));

        Assert.Equal(new Bounds(0, 0, 75, 100), first.Bounds);
        Assert.Equal(new Bounds(150, 0, 150, 100), second.Bounds);
    }

    [Fact]
    public void ResizingViaTheTopLeftHandleKeepsTheOppositeCornerOfTheBoundingBoxAnchored()
    {
        var board = new Board();
        var first = AddInstance(board, 0, 0, 50, 50);
        var second = AddInstance(board, 100, 0, 100, 50);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        SelectBoth(canvas);

        // Combined bbox starts at (0,0,200,50), bottom-right corner (200,50). Dragging the
        // top-left handle outward (up-and-left) grows the bbox to (-20,-10,220,60) - the
        // opposite (bottom-right) corner must stay at exactly (200,50).
        canvas.DragHandle(canvas.Find(".group-resize-handle.top-left"), (300, 200), (280, 190));

        Assert.Equal(-20, first.Bounds.X, precision: 10);
        Assert.Equal(-10, first.Bounds.Y, precision: 10);
        Assert.Equal(55, first.Bounds.Width, precision: 10);
        Assert.Equal(60, first.Bounds.Height, precision: 10);

        Assert.Equal(90, second.Bounds.X, precision: 10);
        Assert.Equal(-10, second.Bounds.Y, precision: 10);
        Assert.Equal(110, second.Bounds.Width, precision: 10);
        Assert.Equal(60, second.Bounds.Height, precision: 10);
        Assert.Equal(200, second.Bounds.Right, precision: 10);
        Assert.Equal(50, second.Bounds.Bottom, precision: 10);
    }

    [Fact]
    public void TheBoardIsUnchangedMidGroupResizeAndOnlyUpdatesOnRelease()
    {
        var board = new Board();
        var first = AddInstance(board, 0, 0, 50, 50);
        var second = AddInstance(board, 100, 0, 100, 50);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        SelectBoth(canvas);

        canvas.PressHandle(canvas.Find(".group-resize-handle.bottom-right"), (300, 200));
        canvas.MoveTo((400, 250));

        Assert.Equal(new Bounds(0, 0, 50, 50), first.Bounds);
        Assert.Equal(new Bounds(100, 0, 100, 50), second.Bounds);
        var containers = canvas.FindAll(".component-container");
        Assert.Contains("width: 75px", containers[0].GetAttribute("style"));
        Assert.Contains("width: 150px", containers[1].GetAttribute("style"));

        canvas.ReleaseAt((400, 250));

        Assert.Equal(new Bounds(0, 0, 75, 100), first.Bounds);
        Assert.Equal(new Bounds(150, 0, 150, 100), second.Bounds);
    }

    [Fact]
    public void GroupResizeScalesTheScreenDeltaByTheCurrentZoom()
    {
        var board = new Board();
        var first = AddInstance(board, 0, 0, 50, 50);
        var second = AddInstance(board, 100, 0, 100, 50);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        canvas.ZoomIn(); // zooms to scale 1.1
        SelectBoth(canvas);

        canvas.DragHandle(canvas.Find(".group-resize-handle.bottom-right"), (300, 200), (344, 222));

        // Computed with the same arithmetic ZoomPanTracker uses (1.0 + 0.1), rather than the
        // decimal literal 1.1, so this can't disagree with production code over double rounding.
        var scale = 1.0 + 0.1;
        var deltaX = 44 / scale;
        var deltaY = 22 / scale;
        var scaleX = (200 + deltaX) / 200;
        var scaleY = (50 + deltaY) / 50;

        Assert.Equal(50 * scaleX, first.Bounds.Width, precision: 10);
        Assert.Equal(50 * scaleY, first.Bounds.Height, precision: 10);
        Assert.Equal(100 * scaleX, second.Bounds.X, precision: 10);
        Assert.Equal(100 * scaleX, second.Bounds.Width, precision: 10);
        Assert.Equal(50 * scaleY, second.Bounds.Height, precision: 10);
    }

    [Fact]
    public void GroupResizeNeverShrinksAMemberBelowItsOwnMinimumSize()
    {
        var board = new Board();
        var first = AddInstance(board, 0, 0, 50, 50);
        var second = AddInstance(board, 100, 0, 50, 50);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        SelectBoth(canvas);

        // Both members already sit at the 50x50 floor a lone instance's own resize handles
        // enforce. A large inward drag must be fully absorbed by the group resize's own clamp
        // rather than proportionally scaling either member smaller than that.
        canvas.DragHandle(
            canvas.Find(".group-resize-handle.bottom-right"),
            (300, 200),
            (-700, -800)
        );

        Assert.Equal(new Bounds(0, 0, 50, 50), first.Bounds);
        Assert.Equal(new Bounds(100, 0, 50, 50), second.Bounds);
    }

    // The box is a hit target in its own right: a press anywhere inside the multi-selection's
    // bounds lands on it, not on whatever sits beneath, which is what lets it start the move.
    [Fact]
    public void TheSelectionBoxIsASolidHitTargetCarryingItsRole()
    {
        var board = new Board();
        AddInstance(board, 0, 0);
        AddInstance(board, 300, 0);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        SelectBoth(canvas);

        var box = canvas.Find(".selection-bounding-box");
        Assert.Equal("selection-bounds", box.GetAttribute("data-d12-role"));
        var canvasCss = canvas
            .FindAll("style")
            .Select(style => style.InnerHtml)
            .Single(css => css.Contains(".selection-bounding-box {"));
        Assert.Contains(
            "pointer-events: auto",
            ExtractBlock(canvasCss, ".selection-bounding-box {")
        );
    }

    [Fact]
    public async Task AStationaryClickOnAGroupResizeHandleKeepsTheSelectionAndLeavesNoHistoryEntry()
    {
        var board = new Board();
        var first = AddInstance(board, 0, 0);
        var second = AddInstance(board, 300, 0);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        canvas.DragOn(canvas.FindAll(".component-container")[1], (310, 10), (330, 10));
        SelectBoth(canvas);

        canvas.PressHandle(canvas.Find(".group-resize-handle.left"), (0, 25));
        canvas.ReleaseAt((0, 25));

        Assert.All(
            canvas.FindAll(".component-container"),
            container => Assert.Equal("true", container.GetAttribute("aria-selected"))
        );
        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());
        Assert.Equal(new Bounds(0, 0, 50, 50), first.Bounds);
        Assert.Equal(new Bounds(300, 0, 50, 50), second.Bounds);
    }

    [Fact]
    public void IndividualResizeHandlesAreHiddenWhileMultiSelectedAndReappearOnceSelectionShrinksToOne()
    {
        var board = new Board();
        AddInstance(board, 0, 0);
        AddInstance(board, 300, 0);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        SelectBoth(canvas);

        Assert.Empty(canvas.FindAll(".component-container .resize-handle"));
        Assert.Equal(8, canvas.FindAll(".group-resize-handle").Count);

        // A plain click collapses the selection down to just this one - its own handles should
        // reappear, and the group overlay should disappear.
        canvas.ClickOn(canvas.FindAll(".component-container")[0]);

        Assert.Empty(canvas.FindAll(".selection-bounding-box"));
        Assert.Equal(8, canvas.FindAll(".resize-handle").Count);
    }
}
