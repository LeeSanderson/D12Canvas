using System.Threading.Tasks;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

public class DiagramCanvasGroupZIndexTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasGroupZIndexTests()
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

    private static ComponentInstance AddInstance(Board board, double x, int zIndex)
    {
        var instance = new ComponentInstance(
            ComponentTypeKey,
            new TestProps(),
            new Bounds(x, 0, 50, 50),
            zIndex
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
    public async Task BringToFrontPressedAppliesToEveryMemberOfAPersistedGroupPreservingTheirRelativeOrder()
    {
        var board = new Board();
        var first = AddInstance(board, 0, zIndex: 2);
        var second = AddInstance(board, 100, zIndex: 3);
        AddInstance(board, 200, zIndex: 9);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        SelectBoth(canvas);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        await canvas.InvokeAsync(() => canvas.Instance.OnBringToFrontPressed());

        Assert.Equal(10, first.ZIndex);
        Assert.Equal(11, second.ZIndex);
    }

    [Fact]
    public async Task SendToBackPressedAppliesToEveryMemberOfAPersistedGroupPreservingTheirRelativeOrder()
    {
        var board = new Board();
        AddInstance(board, 0, zIndex: 0);
        var first = AddInstance(board, 100, zIndex: 5);
        var second = AddInstance(board, 200, zIndex: 8);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var containers = canvas.FindAll(".component-container");
        canvas.ClickOn(containers[1]);
        canvas.ClickOn(containers[2], shift: true);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        await canvas.InvokeAsync(() => canvas.Instance.OnSendToBackPressed());

        Assert.Equal(-2, first.ZIndex);
        Assert.Equal(-1, second.ZIndex);
    }

    [Fact]
    public async Task BringForwardPressedRewritesEveryMemberOfAPersistedGroupInOneGesturePreservingTheirRelativeOrder()
    {
        var board = new Board();
        var first = AddInstance(board, 0, zIndex: 2);
        var second = AddInstance(board, 100, zIndex: 4);
        AddInstance(board, 200, zIndex: 6);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        SelectBoth(canvas);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        await canvas.InvokeAsync(() => canvas.Instance.OnBringForwardPressed());

        Assert.Equal(4, first.ZIndex);
        Assert.Equal(6, second.ZIndex);
    }

    [Fact]
    public async Task SendBackwardPressedRewritesEveryMemberOfAPersistedGroupInOneGesturePreservingTheirRelativeOrder()
    {
        var board = new Board();
        AddInstance(board, 0, zIndex: 4);
        var first = AddInstance(board, 100, zIndex: 6);
        var second = AddInstance(board, 200, zIndex: 8);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var containers = canvas.FindAll(".component-container");
        canvas.ClickOn(containers[1]);
        canvas.ClickOn(containers[2], shift: true);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        await canvas.InvokeAsync(() => canvas.Instance.OnSendBackwardPressed());

        Assert.Equal(4, first.ZIndex);
        Assert.Equal(6, second.ZIndex);
    }

    [Fact]
    public async Task LayeringANestedGroupRewritesEveryLeafMembersZIndexPreservingRelativeOrder()
    {
        var board = new Board();
        var a = AddInstance(board, 0, zIndex: 1);
        var b = AddInstance(board, 100, zIndex: 2);
        var c = AddInstance(board, 200, zIndex: 3);
        var untouched = AddInstance(board, 300, zIndex: 9);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        SelectBoth(canvas);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());
        canvas.ClickOn(canvas.FindAll(".component-container")[2], shift: true);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());
        Assert.Equal(2, board.Groups.Count);

        await canvas.InvokeAsync(() => canvas.Instance.OnBringToFrontPressed());

        Assert.Equal(10, a.ZIndex);
        Assert.Equal(11, b.ZIndex);
        Assert.Equal(12, c.ZIndex);
        Assert.Equal(9, untouched.ZIndex);
    }
}
