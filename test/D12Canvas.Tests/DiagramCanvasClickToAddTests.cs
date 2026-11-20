using System.Linq;
using System.Threading.Tasks;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

public class DiagramCanvasClickToAddTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasClickToAddTests()
    {
        SetupDiagramCanvasJsModule();
    }

    private void RegisterTestComponent(ComponentSize? defaultSize)
    {
        var registry = new ComponentRegistry();
        registry.Register(
            new ComponentRegistration(
                Key: ComponentTypeKey,
                ComponentType: typeof(TestPropsComponent),
                PropsType: typeof(TestProps),
                DisplayName: "Test Props",
                AccessibleName: "Test props component",
                DefaultProps: new TestProps("default"),
                Icon: null,
                Role: "group",
                DefaultSize: defaultSize,
                Category: null
            )
        );
        Services.AddSingleton<IComponentRegistry>(registry);
    }

    [Fact]
    public async Task ClickToAddPlacesANewInstanceCenteredOnTheViewport()
    {
        RegisterTestComponent(new ComponentSize(120, 80));
        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd(ComponentTypeKey));

        var instance = Assert.Single(board.Components);
        Assert.Equal(ComponentTypeKey, instance.ComponentTypeKey);
        Assert.Equal("default", ((TestProps)instance.Props).Text);
        Assert.Equal(new Bounds(340, 260, 120, 80), instance.Bounds);
    }

    [Fact]
    public async Task ClickToAddFallsBackToTheComponentContainerDefaultSizeWhenNoDefaultSizeIsRegistered()
    {
        RegisterTestComponent(defaultSize: null);
        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd(ComponentTypeKey));

        var instance = Assert.Single(board.Components);
        Assert.Equal(new Bounds(300, 225, 200, 150), instance.Bounds);
    }

    [Fact]
    public async Task ClickToAddUsesTheCurrentViewportCenterAccountingForPanAndZoom()
    {
        RegisterTestComponent(new ComponentSize(120, 80));
        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        canvas.ZoomIn();
        await canvas.Pan(from: (100, 100), to: (50, 40));

        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd(ComponentTypeKey));

        var scale = 1.0 + 0.1;
        var centerX = -(-50) / scale + 800 / scale / 2;
        var centerY = -(-60) / scale + 600 / scale / 2;
        var instance = Assert.Single(board.Components);
        Assert.Equal(centerX - 60, instance.Bounds.X, precision: 10);
        Assert.Equal(centerY - 40, instance.Bounds.Y, precision: 10);
    }

    [Fact]
    public async Task ConsecutiveClickToAddsCascadeWithAnOffsetSoInstancesDontStack()
    {
        RegisterTestComponent(new ComponentSize(120, 80));
        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd(ComponentTypeKey));
        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd(ComponentTypeKey));
        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd(ComponentTypeKey));

        Assert.Equal(3, board.Components.Count);
        var bounds = board.Components.Select(i => i.Bounds).ToList();
        Assert.Equal(bounds[0].X + 20, bounds[1].X);
        Assert.Equal(bounds[0].Y + 20, bounds[1].Y);
        Assert.Equal(bounds[1].X + 20, bounds[2].X);
        Assert.Equal(bounds[1].Y + 20, bounds[2].Y);
    }

    [Fact]
    public async Task ClickToAddPlacesTheNewInstanceAboveEveryExistingInstance()
    {
        RegisterTestComponent(new ComponentSize(120, 80));
        var board = new Board();
        board.AddComponent(
            new ComponentInstance(ComponentTypeKey, new TestProps(), new Bounds(0, 0, 50, 50), 9)
        );
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd(ComponentTypeKey));

        var placed = board.Components.Single(i => i.Bounds.Width == 120);
        Assert.Equal(10, placed.ZIndex);
    }

    [Fact]
    public async Task ClickToAddIsANoOpWhenNoBoardIsWired()
    {
        RegisterTestComponent(new ComponentSize(120, 80));
        var canvas = Render<DiagramCanvas>();

        var exception = await Record.ExceptionAsync(
            () => canvas.InvokeAsync(() => canvas.Instance.ClickToAdd(ComponentTypeKey))
        );

        Assert.Null(exception);
    }
}
