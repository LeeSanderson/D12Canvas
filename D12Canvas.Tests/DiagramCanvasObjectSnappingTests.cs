using System.Threading.Tasks;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Pointer;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

public class DiagramCanvasObjectSnappingTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasObjectSnappingTests()
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
                DefaultSize: new ComponentSize(120, 80),
                Category: null
            )
        );
        Services.AddSingleton<IComponentRegistry>(registry);
    }

    private static ComponentInstance AddInstance(Board board, Bounds bounds)
    {
        var instance = new ComponentInstance(ComponentTypeKey, new TestProps(), bounds);
        board.AddComponent(instance);
        return instance;
    }

    private IRenderedComponent<DiagramCanvas> RenderBoard(Board board, bool objectSnapping = true)
    {
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters
                .Add(p => p.Board, board)
                .Add(p => p.SnapToGrid, false)
                .Add(p => p.ObjectSnapping, objectSnapping)
        );
        canvas
            .InvokeAsync(() => canvas.Instance.OnContainerResized(800, 600))
            .GetAwaiter()
            .GetResult();
        return canvas;
    }

    private static void Press(
        IRenderedComponent<DiagramCanvas> canvas,
        Guid id,
        double x,
        double y
    ) =>
        canvas
            .InvokeAsync(
                () =>
                    canvas.Instance.OnPointerPressed(
                        PointerEvents.Press(HitRole.Instance, PointerPress.PrimaryButton, x, y, id)
                    )
            )
            .GetAwaiter()
            .GetResult();

    private static void Move(
        IRenderedComponent<DiagramCanvas> canvas,
        double x,
        double y,
        bool ctrl = false
    ) =>
        canvas
            .InvokeAsync(() => canvas.Instance.OnPointerMoved(PointerEvents.Move(x, y, ctrl: ctrl)))
            .GetAwaiter()
            .GetResult();

    private static void Release(IRenderedComponent<DiagramCanvas> canvas, double x, double y) =>
        canvas
            .InvokeAsync(
                () =>
                    canvas.Instance.OnPointerReleased(
                        PointerEvents.Release(PointerPress.PrimaryButton, x, y)
                    )
            )
            .GetAwaiter()
            .GetResult();

    [Fact]
    public void ObjectSnappingDefaultsToOff()
    {
        var canvas = Render<DiagramCanvas>();

        Assert.False(canvas.Instance.ObjectSnapping);
    }

    [Fact]
    public void ADragNearAShapeDrawsAFullBleedGuideInTheGuideLayerAndTheReleaseClearsIt()
    {
        var board = new Board();
        var moving = AddInstance(board, new Bounds(0, 0, 50, 50));
        AddInstance(board, new Bounds(203, 300, 80, 50));
        var canvas = RenderBoard(board);

        Press(canvas, moving.Id, 10, 10);
        Move(canvas, 210, 43);

        var guide = canvas.Find(".guide-layer .guide-strokes line.alignment-guide");
        Assert.Equal("203", guide.GetAttribute("x1"));
        Assert.Equal("203", guide.GetAttribute("x2"));
        Assert.Equal("0", guide.GetAttribute("y1"));
        Assert.Equal("600", guide.GetAttribute("y2"));
        Assert.Equal("non-scaling-stroke", guide.GetAttribute("vector-effect"));

        Release(canvas, 210, 43);

        Assert.Empty(canvas.FindAll(".guide-strokes"));
        Assert.Equal(new Bounds(203, 33, 50, 50), board.GetComponent(moving.Id)!.Bounds);
    }

    [Fact]
    public void AnEqualSpacingMatchDrawsTheRepeatedGapsWithCaps()
    {
        var board = new Board();
        AddInstance(board, new Bounds(0, 200, 50, 50));
        AddInstance(board, new Bounds(90, 200, 50, 50));
        var third = AddInstance(board, new Bounds(300, 0, 50, 50));
        var canvas = RenderBoard(board);

        Press(canvas, third.Id, 310, 10);
        Move(canvas, 193, 213);

        Assert.Equal(6, canvas.FindAll(".guide-strokes line.spacing-guide").Count);
    }

    [Fact]
    public void PressingCtrlWithThePointerStillLetsGoOfTheGuide()
    {
        var board = new Board();
        var moving = AddInstance(board, new Bounds(0, 0, 50, 50));
        AddInstance(board, new Bounds(203, 300, 80, 50));
        var canvas = RenderBoard(board);

        Press(canvas, moving.Id, 10, 10);
        Move(canvas, 210, 43);
        Move(canvas, 210, 43, ctrl: true);

        Assert.Empty(canvas.FindAll(".guide-strokes"));
    }

    [Fact]
    public void WithObjectSnappingOffNoGuideIsDrawn()
    {
        var board = new Board();
        var moving = AddInstance(board, new Bounds(0, 0, 50, 50));
        AddInstance(board, new Bounds(203, 300, 80, 50));
        var canvas = RenderBoard(board, objectSnapping: false);

        Press(canvas, moving.Id, 10, 10);
        Move(canvas, 210, 43);

        Assert.Empty(canvas.FindAll(".guide-strokes"));
    }

    [Fact]
    public void AShapeOffScreenIsNoCandidate()
    {
        var board = new Board();
        var moving = AddInstance(board, new Bounds(0, 0, 50, 50));
        AddInstance(board, new Bounds(203, 900, 80, 50));
        var canvas = RenderBoard(board);

        Press(canvas, moving.Id, 10, 10);
        Move(canvas, 210, 43);
        Release(canvas, 210, 43);

        Assert.Equal(200, board.GetComponent(moving.Id)!.Bounds.X);
    }

    [Fact]
    public async Task TogglingObjectSnappingNotifiesTheBindingAndLeavesSnapToGridAlone()
    {
        bool? notified = null;
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(
                p => p.ObjectSnappingChanged,
                EventCallback.Factory.Create<bool>(this, value => notified = value)
            )
        );

        await canvas.InvokeAsync(() => canvas.Instance.OnToggleObjectSnappingPressed());
        Assert.True(canvas.Instance.ObjectSnapping);
        Assert.True(notified);
        Assert.True(canvas.Instance.SnapToGrid);

        await canvas.InvokeAsync(() => canvas.Instance.OnToggleObjectSnappingPressed());
        Assert.False(canvas.Instance.ObjectSnapping);
        Assert.False(notified);
        Assert.True(canvas.Instance.SnapToGrid);
    }
}
