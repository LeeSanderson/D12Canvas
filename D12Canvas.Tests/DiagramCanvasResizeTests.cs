using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// Ticket 31: resize a selected instance via its handles. A resize is one press-to-release
// gesture (ADR 0007) - Board only ever sees the final Bounds, on release, never an intermediate
// mousemove tick.
public class DiagramCanvasResizeTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasResizeTests()
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
        double width,
        double height
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

    [Fact]
    public void ResizingASelectedInstanceViaTheBottomRightHandleGrowsItByTheScreenDelta()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100, 50, 50);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find(".component-container").Click();

        var handle = canvas.Find(".resize-handle.bottom-right");
        handle.MouseDown(new MouseEventArgs { ClientX = 300, ClientY = 200 });
        handle.MouseMove(new MouseEventArgs { ClientX = 340, ClientY = 225 });
        handle.MouseUp(new MouseEventArgs { ClientX = 340, ClientY = 225 });

        Assert.Equal(new Bounds(100, 100, 90, 75), instance.Bounds);
    }

    [Fact]
    public void ResizingScalesTheScreenDeltaByTheCurrentZoom()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100, 50, 50);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find(".diagram-canvas").Wheel(new WheelEventArgs { DeltaY = -100 }); // zooms to scale 1.1
        canvas.Find(".component-container").Click();

        var handle = canvas.Find(".resize-handle.bottom-right");
        handle.MouseDown(new MouseEventArgs { ClientX = 300, ClientY = 200 });
        handle.MouseMove(new MouseEventArgs { ClientX = 344, ClientY = 222 });
        handle.MouseUp(new MouseEventArgs { ClientX = 344, ClientY = 222 });

        // Computed with the same arithmetic ZoomPanTracker uses (1.0 + 0.1), rather than the
        // decimal literal 1.1, so this can't disagree with production code over double rounding.
        var scale = 1.0 + 0.1;
        Assert.Equal(50 + 44 / scale, instance.Bounds.Width, precision: 10);
        Assert.Equal(50 + 22 / scale, instance.Bounds.Height, precision: 10);
        Assert.Equal(100, instance.Bounds.X, precision: 10);
        Assert.Equal(100, instance.Bounds.Y, precision: 10);
    }

    [Fact]
    public void TheBoardIsUnchangedMidResizeAndOnlyUpdatesOnRelease()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100, 50, 50);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find(".component-container").Click();

        var handle = canvas.Find(".resize-handle.bottom-right");
        handle.MouseDown(new MouseEventArgs { ClientX = 300, ClientY = 200 });
        handle.MouseMove(new MouseEventArgs { ClientX = 350, ClientY = 260 });

        // Mid-resize: the instance visually follows the cursor (rendered via ComponentContainer's
        // own local Width/Height), but Board itself hasn't been told about it yet.
        Assert.Contains("width: 100px", canvas.Find(".component-container").GetAttribute("style"));
        Assert.Contains("height: 110px", canvas.Find(".component-container").GetAttribute("style"));
        Assert.Equal(new Bounds(100, 100, 50, 50), instance.Bounds);

        canvas
            .Find(".resize-handle.bottom-right")
            .MouseUp(new MouseEventArgs { ClientX = 350, ClientY = 260 });

        Assert.Equal(new Bounds(100, 100, 100, 110), instance.Bounds);
    }

    [Fact]
    public void ResizingViaTheTopLeftHandleKeepsTheOppositeCornerAnchoredOnTheBoard()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find(".component-container").Click();

        var handle = canvas.Find(".resize-handle.top-left");
        handle.MouseDown(new MouseEventArgs { ClientX = 300, ClientY = 200 });
        handle.MouseMove(new MouseEventArgs { ClientX = 310, ClientY = 220 });
        handle.MouseUp(new MouseEventArgs { ClientX = 310, ClientY = 220 });

        Assert.Equal(new Bounds(110, 120, 90, 80), instance.Bounds);
        Assert.Equal(200, instance.Bounds.Right, precision: 10);
        Assert.Equal(200, instance.Bounds.Bottom, precision: 10);
    }
}
