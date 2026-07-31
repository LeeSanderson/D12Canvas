using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// A multi-selection (2+) moves and resizes as a single bounding-box unit. The bounding box is
// computed over every selected member and rendered (with its own resize handles) whenever 2+ are
// selected; individual members' own resize handles are suppressed for that same reason
// (DiagramCanvasResizeTests/ComponentContainerTests cover their single-select case). A move can
// start either by dragging one of the selected members directly, or by dragging empty space
// inside the combined bounding box - either way every member moves by the same delta, and the
// whole gesture (move or resize) commits to Board exactly once, on release.
public class DiagramCanvasMultiSelectionMoveResizeTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasMultiSelectionMoveResizeTests()
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
        containers[0].Click();
        containers[1].Click(new MouseEventArgs { ShiftKey = true });
    }

    [Fact]
    public void TheBoundingBoxIsNotShownForASingleSelection()
    {
        var board = new Board();
        AddInstance(board, 0, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find(".component-container").Click();

        Assert.Empty(canvas.FindAll(".selection-bounding-box"));
    }

    [Fact]
    public void TheBoundingBoxMatchesTheUnionOfMemberBoundsWhenTwoAreSelected()
    {
        var board = new Board();
        AddInstance(board, 0, 0);
        AddInstance(board, 300, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

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
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        SelectBoth(canvas);

        var containers = canvas.FindAll(".component-container");
        containers[0].MouseDown(new MouseEventArgs { ClientX = 300, ClientY = 200 });
        containers[0].MouseMove(new MouseEventArgs { ClientX = 340, ClientY = 175 });
        containers[0].MouseUp(new MouseEventArgs { ClientX = 340, ClientY = 175 });

        Assert.Equal(new Bounds(140, 75, 50, 50), first.Bounds);
        Assert.Equal(new Bounds(340, 75, 50, 50), second.Bounds);
    }

    [Fact]
    public void DraggingEmptySpaceWithinTheBoundingBoxMovesTheWholeSelectionAndDoesNotClearItAfterwards()
    {
        var board = new Board();
        var first = AddInstance(board, 0, 0);
        var second = AddInstance(board, 300, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        SelectBoth(canvas);

        // (150, 25) sits inside the combined bounding box (0,0)-(350,50) but isn't over either
        // instance - empty space "within the marquee".
        canvas
            .Find(".diagram-canvas")
            .MouseDown(new MouseEventArgs { ClientX = 150, ClientY = 25 });
        canvas
            .Find(".diagram-canvas")
            .MouseMove(new MouseEventArgs { ClientX = 200, ClientY = 75 });
        canvas.Find(".diagram-canvas").MouseUp(new MouseEventArgs { ClientX = 200, ClientY = 75 });

        Assert.Equal(new Bounds(50, 50, 50, 50), first.Bounds);
        Assert.Equal(new Bounds(350, 50, 50, 50), second.Bounds);

        // The trailing native click that follows a real drag's mouseup must not clear the
        // selection it just moved (the same _dragMoved guard the marquee relies on).
        canvas.Find(".diagram-canvas").Click();
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
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        SelectBoth(canvas);

        canvas
            .Find(".diagram-canvas")
            .MouseDown(new MouseEventArgs { ClientX = 150, ClientY = 25 });
        canvas
            .Find(".diagram-canvas")
            .MouseMove(new MouseEventArgs { ClientX = 200, ClientY = 75 });

        // Mid-drag: this isn't a pan, and no marquee was drawn - the whole gesture instead lives
        // in the live preview each member's own style reflects (via EffectiveBounds); Board's own
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

        canvas.Find(".diagram-canvas").MouseUp(new MouseEventArgs { ClientX = 200, ClientY = 75 });

        Assert.Equal(new Bounds(50, 50, 50, 50), first.Bounds);
        Assert.Equal(new Bounds(350, 50, 50, 50), second.Bounds);
    }

    [Fact]
    public void GroupMoveViaEmptySpaceScalesTheScreenDeltaByTheCurrentZoom()
    {
        var board = new Board();
        var first = AddInstance(board, 0, 0);
        var second = AddInstance(board, 300, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find(".diagram-canvas").Wheel(new WheelEventArgs { DeltaY = -100 }); // zooms to scale 1.1
        SelectBoth(canvas);

        canvas
            .Find(".diagram-canvas")
            .MouseDown(new MouseEventArgs { ClientX = 150, ClientY = 25 });
        canvas
            .Find(".diagram-canvas")
            .MouseMove(new MouseEventArgs { ClientX = 194, ClientY = 47 });
        canvas.Find(".diagram-canvas").MouseUp(new MouseEventArgs { ClientX = 194, ClientY = 47 });

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
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        SelectBoth(canvas);

        // Combined bbox starts at (0,0,200,50). Growing it to (0,0,300,100) scales x1.5/x2.
        var handle = canvas.Find(".group-resize-handle.bottom-right");
        handle.MouseDown(new MouseEventArgs { ClientX = 300, ClientY = 200 });
        handle.MouseMove(new MouseEventArgs { ClientX = 400, ClientY = 250 });
        handle.MouseUp(new MouseEventArgs { ClientX = 400, ClientY = 250 });

        Assert.Equal(new Bounds(0, 0, 75, 100), first.Bounds);
        Assert.Equal(new Bounds(150, 0, 150, 100), second.Bounds);
    }

    [Fact]
    public void ResizingViaTheTopLeftHandleKeepsTheOppositeCornerOfTheBoundingBoxAnchored()
    {
        var board = new Board();
        var first = AddInstance(board, 0, 0, 50, 50);
        var second = AddInstance(board, 100, 0, 100, 50);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        SelectBoth(canvas);

        // Combined bbox starts at (0,0,200,50), bottom-right corner (200,50). Dragging the
        // top-left handle outward (up-and-left) grows the bbox to (-20,-10,220,60) - the
        // opposite (bottom-right) corner must stay at exactly (200,50).
        var handle = canvas.Find(".group-resize-handle.top-left");
        handle.MouseDown(new MouseEventArgs { ClientX = 300, ClientY = 200 });
        handle.MouseMove(new MouseEventArgs { ClientX = 280, ClientY = 190 });
        handle.MouseUp(new MouseEventArgs { ClientX = 280, ClientY = 190 });

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
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        SelectBoth(canvas);

        var handle = canvas.Find(".group-resize-handle.bottom-right");
        handle.MouseDown(new MouseEventArgs { ClientX = 300, ClientY = 200 });
        handle.MouseMove(new MouseEventArgs { ClientX = 400, ClientY = 250 });

        Assert.Equal(new Bounds(0, 0, 50, 50), first.Bounds);
        Assert.Equal(new Bounds(100, 0, 100, 50), second.Bounds);
        var containers = canvas.FindAll(".component-container");
        Assert.Contains("width: 75px", containers[0].GetAttribute("style"));
        Assert.Contains("width: 150px", containers[1].GetAttribute("style"));

        canvas
            .Find(".group-resize-handle.bottom-right")
            .MouseUp(new MouseEventArgs { ClientX = 400, ClientY = 250 });

        Assert.Equal(new Bounds(0, 0, 75, 100), first.Bounds);
        Assert.Equal(new Bounds(150, 0, 150, 100), second.Bounds);
    }

    [Fact]
    public void GroupResizeScalesTheScreenDeltaByTheCurrentZoom()
    {
        var board = new Board();
        var first = AddInstance(board, 0, 0, 50, 50);
        var second = AddInstance(board, 100, 0, 100, 50);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find(".diagram-canvas").Wheel(new WheelEventArgs { DeltaY = -100 }); // zooms to scale 1.1
        SelectBoth(canvas);

        var handle = canvas.Find(".group-resize-handle.bottom-right");
        handle.MouseDown(new MouseEventArgs { ClientX = 300, ClientY = 200 });
        handle.MouseMove(new MouseEventArgs { ClientX = 344, ClientY = 222 });
        handle.MouseUp(new MouseEventArgs { ClientX = 344, ClientY = 222 });

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
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        SelectBoth(canvas);

        // Both members already sit at the 50x50 floor a lone instance's own resize handles
        // enforce. A large inward drag must be fully absorbed by the group resize's own clamp
        // rather than proportionally scaling either member smaller than that.
        var handle = canvas.Find(".group-resize-handle.bottom-right");
        handle.MouseDown(new MouseEventArgs { ClientX = 300, ClientY = 200 });
        handle.MouseMove(new MouseEventArgs { ClientX = -700, ClientY = -800 });
        handle.MouseUp(new MouseEventArgs { ClientX = -700, ClientY = -800 });

        Assert.Equal(new Bounds(0, 0, 50, 50), first.Bounds);
        Assert.Equal(new Bounds(100, 0, 50, 50), second.Bounds);
    }

    // A stationary click (mousedown/mouseup, no movement) on a group-resize handle must not bubble
    // a native click up to the canvas's own HandleCanvasClick and wipe the selection the bounding
    // box itself belongs to. bUnit's synthetic event dispatch can't drive this specific scenario
    // (a target with @onclick:stopPropagation but no @onclick handler of its own throws
    // Bunit.MissingEventHandlerException, unlike a real browser) - covered instead by
    // MultiSelectionMoveResizeVisualTests.StationaryClickOnGroupResizeHandle_DoesNotClearSelection,
    // which drives a real click through a real browser.

    [Fact]
    public void IndividualResizeHandlesAreHiddenWhileMultiSelectedAndReappearOnceSelectionShrinksToOne()
    {
        var board = new Board();
        AddInstance(board, 0, 0);
        AddInstance(board, 300, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        SelectBoth(canvas);

        Assert.Empty(canvas.FindAll(".component-container .resize-handle"));
        Assert.Equal(8, canvas.FindAll(".group-resize-handle").Count);

        // A plain click collapses the selection down to just this one - its own handles should
        // reappear, and the group overlay should disappear.
        canvas.FindAll(".component-container")[0].Click();

        Assert.Empty(canvas.FindAll(".selection-bounding-box"));
        Assert.Equal(8, canvas.FindAll(".resize-handle").Count);
    }
}
