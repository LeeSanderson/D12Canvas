using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// Drag-move a selected instance. A drag is one press-to-release gesture - Board only ever sees
// the final Bounds, on release, never an intermediate mousemove tick.
public class DiagramCanvasDragMoveTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasDragMoveTests()
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

    private static ComponentInstance AddInstance(Board board, double x, double y)
    {
        var instance = new ComponentInstance(
            ComponentTypeKey,
            new TestProps(),
            new Bounds(x, y, 50, 50)
        );
        board.AddComponent(instance);
        return instance;
    }

    [Fact]
    public void DraggingASelectedInstanceMovesItByTheScreenDelta()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find(".component-container").Click();

        var container = canvas.Find(".component-container");
        container.MouseDown(new MouseEventArgs { ClientX = 300, ClientY = 200 });
        container.MouseMove(new MouseEventArgs { ClientX = 340, ClientY = 175 });
        container.MouseUp(new MouseEventArgs { ClientX = 340, ClientY = 175 });

        Assert.Equal(new Bounds(140, 75, 50, 50), instance.Bounds);
    }

    [Fact]
    public void DraggingScalesTheScreenDeltaByTheCurrentZoom()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find(".diagram-canvas").Wheel(new WheelEventArgs { DeltaY = -100 }); // zooms to scale 1.1
        canvas.Find(".component-container").Click();

        var container = canvas.Find(".component-container");
        container.MouseDown(new MouseEventArgs { ClientX = 300, ClientY = 200 });
        container.MouseMove(new MouseEventArgs { ClientX = 344, ClientY = 222 });
        container.MouseUp(new MouseEventArgs { ClientX = 344, ClientY = 222 });

        // Computed with the same arithmetic ZoomPanTracker uses (1.0 + 0.1), rather than the
        // decimal literal 1.1, so this can't disagree with production code over double rounding.
        var scale = 1.0 + 0.1;
        Assert.Equal(100 + 44 / scale, instance.Bounds.X, precision: 10);
        Assert.Equal(100 + 22 / scale, instance.Bounds.Y, precision: 10);
    }

    [Fact]
    public void TheBoardIsUnchangedMidDragAndOnlyUpdatesOnRelease()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find(".component-container").Click();

        var container = canvas.Find(".component-container");
        container.MouseDown(new MouseEventArgs { ClientX = 300, ClientY = 200 });
        container.MouseMove(new MouseEventArgs { ClientX = 350, ClientY = 260 });

        // Mid-drag: the instance visually follows the cursor (rendered via ComponentContainer's
        // own local X/Y), but Board itself hasn't been told about it yet.
        Assert.Contains("left: 150px", canvas.Find(".component-container").GetAttribute("style"));
        Assert.Contains("top: 160px", canvas.Find(".component-container").GetAttribute("style"));
        Assert.Equal(new Bounds(100, 100, 50, 50), instance.Bounds);

        canvas
            .Find(".component-container")
            .MouseUp(new MouseEventArgs { ClientX = 350, ClientY = 260 });

        Assert.Equal(new Bounds(150, 160, 50, 50), instance.Bounds);
    }

    [Fact]
    public void DraggingAnUnselectedInstanceDoesNotMoveIt()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var container = canvas.Find(".component-container");
        container.MouseDown(new MouseEventArgs { ClientX = 300, ClientY = 200 });
        container.MouseMove(new MouseEventArgs { ClientX = 340, ClientY = 175 });
        container.MouseUp(new MouseEventArgs { ClientX = 340, ClientY = 175 });

        Assert.Equal(new Bounds(100, 100, 50, 50), instance.Bounds);
    }

    [Fact]
    public void APressAndReleaseWithNoMovementDoesNotMutateBounds()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find(".component-container").Click();

        var container = canvas.Find(".component-container");
        container.MouseDown(new MouseEventArgs { ClientX = 300, ClientY = 200 });
        container.MouseUp(new MouseEventArgs { ClientX = 300, ClientY = 200 });

        Assert.Equal(new Bounds(100, 100, 50, 50), instance.Bounds);
    }
}
