using System.Threading.Tasks;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// Snap-to-grid: an off-by-default, ephemeral toggle (never on Board) that snaps placement and
// single-instance drag-move to whichever grid layer is currently visually dominant - see
// DiagramCanvasAdaptiveGridTests for the layer-blend math the snap spacing itself reuses.
public class DiagramCanvasSnapToGridTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasSnapToGridTests()
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
                DefaultSize: new ComponentSize(120, 80),
                Category: null
            )
        );
        Services.AddSingleton<IComponentRegistry>(registry);
    }

    [Fact]
    public void SnapToGridDefaultsToOff()
    {
        var canvas = Render<DiagramCanvas>();

        Assert.False(canvas.Instance.SnapToGrid);
    }

    [Fact]
    public async Task CtrlApostropheTogglesSnapToGridAndNotifiesSnapToGridChanged()
    {
        bool? notified = null;
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(
                p => p.SnapToGridChanged,
                EventCallback.Factory.Create<bool>(this, v => notified = v)
            )
        );

        await canvas.InvokeAsync(() => canvas.Instance.OnToggleSnapToGridPressed());
        Assert.True(canvas.Instance.SnapToGrid);
        Assert.True(notified);

        await canvas.InvokeAsync(() => canvas.Instance.OnToggleSnapToGridPressed());
        Assert.False(canvas.Instance.SnapToGrid);
        Assert.False(notified);
    }

    [Fact]
    public async Task DisablingTheBuiltInShortcutMakesCtrlApostropheANoOp()
    {
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.EnableSnapToGridShortcut, false)
        );

        await canvas.InvokeAsync(() => canvas.Instance.OnToggleSnapToGridPressed());

        Assert.False(canvas.Instance.SnapToGrid);
    }

    [Fact]
    public async Task WithSnapOffClickToAddLandsExactlyOnTheRawViewportCenter()
    {
        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd(ComponentTypeKey));

        var instance = Assert.Single(board.Components);
        Assert.Equal(new Bounds(340, 260, 120, 80), instance.Bounds);
    }

    [Fact]
    public async Task WithSnapOnClickToAddSnapsToTheDominantGridLayerSpacing()
    {
        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, true)
        );

        // Pans by (-7, -13): the raw (unsnapped) top-left corner would land at (347, 273) - both
        // off the default zoom's 20-unit grid.
        canvas
            .Find(".diagram-canvas")
            .MouseDown(
                new MouseEventArgs
                {
                    Button = 0,
                    ClientX = 100,
                    ClientY = 100,
                }
            );
        canvas.Find(".diagram-canvas").MouseMove(new MouseEventArgs { ClientX = 93, ClientY = 87 });

        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd(ComponentTypeKey));

        var instance = Assert.Single(board.Components);
        Assert.Equal(new Bounds(340, 280, 120, 80), instance.Bounds);
    }

    [Fact]
    public void WithSnapOnDroppingAPendingPaletteDragSnapsToTheDominantGridLayerSpacing()
    {
        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, true)
        );
        canvas.Instance.BeginPaletteDrag(ComponentTypeKey);

        // Drop point (307, 213): the raw (unsnapped) top-left corner would land at (247, 173) -
        // both off the default zoom's 20-unit grid.
        canvas.Find(".diagram-canvas").Drop(new DragEventArgs { ClientX = 307, ClientY = 213 });

        var instance = Assert.Single(board.Components);
        Assert.Equal(new Bounds(240, 180, 120, 80), instance.Bounds);
    }

    [Fact]
    public async Task SnapSpacingFollowsTheDominantGridLayerAsZoomChanges()
    {
        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, true)
        );

        for (var i = 0; i < 9; i++) // scale 1.0 -> 0.1, exactly layer 1 dominant (200-unit spacing)
        {
            canvas.Find(".diagram-canvas").Wheel(new WheelEventArgs { DeltaY = 100 });
        }

        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd(ComponentTypeKey));

        var instance = Assert.Single(board.Components);
        Assert.Equal(new Bounds(4000, 3000, 120, 80), instance.Bounds);
    }

    [Fact]
    public void WithSnapOnDragMoveSnapsTheCommittedBoundsToTheDominantGridLayerSpacing()
    {
        var board = new Board();
        var instance = new ComponentInstance(
            ComponentTypeKey,
            new TestProps(),
            new Bounds(100, 100, 50, 50),
            0
        );
        board.AddComponent(instance);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, true)
        );

        canvas.Find(".component-container").Click();
        var container = canvas.Find(".component-container");
        container.MouseDown(new MouseEventArgs { ClientX = 300, ClientY = 200 });
        // Raw delta (33, -4) would land the instance at (133, 96) - both off the 20-unit grid.
        container.MouseMove(new MouseEventArgs { ClientX = 333, ClientY = 196 });
        container.MouseUp(new MouseEventArgs { ClientX = 333, ClientY = 196 });

        Assert.Equal(new Bounds(140, 100, 50, 50), instance.Bounds);
    }

    // A real (non-zero) drag can still snap right back to the instance's own current position -
    // that must not push a no-op entry onto the undo stack. Proven by pushing a second such
    // "reverting" move after a genuine one, then checking a single undo returns all the way to the
    // pre-move bounds rather than stopping at the (unchanged) intermediate position.
    [Fact]
    public async Task ASnapThatRevertsToTheCurrentPositionDoesNotAddAnUndoEntry()
    {
        var board = new Board();
        var instance = new ComponentInstance(
            ComponentTypeKey,
            new TestProps(),
            new Bounds(100, 100, 50, 50),
            0
        );
        board.AddComponent(instance);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, true)
        );

        canvas.Find(".component-container").Click();
        var container = canvas.Find(".component-container");
        container.MouseDown(new MouseEventArgs { ClientX = 300, ClientY = 200 });
        container.MouseMove(new MouseEventArgs { ClientX = 333, ClientY = 196 }); // -> snaps to (140, 100)
        container.MouseUp(new MouseEventArgs { ClientX = 333, ClientY = 196 });
        Assert.Equal(new Bounds(140, 100, 50, 50), instance.Bounds);

        container.MouseDown(new MouseEventArgs { ClientX = 400, ClientY = 400 });
        // Raw delta (5, 3) would land at (145, 103), which snaps right back to (140, 100).
        container.MouseMove(new MouseEventArgs { ClientX = 405, ClientY = 403 });
        container.MouseUp(new MouseEventArgs { ClientX = 405, ClientY = 403 });
        Assert.Equal(new Bounds(140, 100, 50, 50), instance.Bounds);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal(new Bounds(100, 100, 50, 50), instance.Bounds);
    }
}
