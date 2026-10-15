using Bunit;
using D12Canvas.Model;
using D12Canvas.Pointer;
using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// Shift+F10 and the ContextMenu key reach the canvas as one call once the listener has given the
// request to the canvas. With no press target, the selection decides the set and the anchor: the
// object set at the bottom-left of the selection's on-screen box, or the canvas set at the
// viewport centre. A secondary press the browser kept by its Menu verdict opens nothing.
public class DiagramCanvasKeyboardMenuTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasKeyboardMenuTests()
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

    private static ComponentInstance AddInstance(Board board, double x, double y = 0)
    {
        var instance = new ComponentInstance(
            ComponentTypeKey,
            new TestProps(),
            new Bounds(x, y, 50, 50),
            0
        );
        board.AddComponent(instance);
        return instance;
    }

    private IRenderedComponent<DiagramCanvas> RenderCanvas(Board board) =>
        Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board)).ReturnToOrigin();

    private static Task PressMenuKey(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.InvokeAsync(() => canvas.Instance.OnContextMenuKeyPressed());

    private static string MenuStyle(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.Find(".d12-context-menu").GetAttribute("style")!;

    private static string MenuLabel(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.Find(".d12-context-menu").GetAttribute("aria-label")!;

    [Fact]
    public async Task WithAShapeSelectedTheObjectMenuOpensAtTheBottomLeftOfItsBox()
    {
        var board = new Board();
        AddInstance(board, 40, 30);
        var canvas = RenderCanvas(board);
        canvas.ClickOn(canvas.Find(".component-container"));

        await PressMenuKey(canvas);

        Assert.Equal("Selection actions", MenuLabel(canvas));
        Assert.Contains("left: 40px", MenuStyle(canvas));
        Assert.Contains("top: 80px", MenuStyle(canvas));
    }

    [Fact]
    public async Task WithTwoShapesSelectedTheMenuOpensUnderTheirCombinedBox()
    {
        var board = new Board();
        AddInstance(board, 40, 30);
        AddInstance(board, 200, 100);
        var canvas = RenderCanvas(board);
        var containers = canvas.FindAll(".component-container");
        canvas.ClickOn(containers[0]);
        canvas.ClickOn(containers[1], shift: true);

        await PressMenuKey(canvas);

        Assert.Contains("left: 40px", MenuStyle(canvas));
        Assert.Contains("top: 150px", MenuStyle(canvas));
    }

    [Fact]
    public async Task WithOnlyAnEdgeSelectedTheObjectMenuOpensAtTheEdgesBox()
    {
        var board = new Board();
        var source = AddInstance(board, 0);
        var target = AddInstance(board, 200);
        var edge = new Edge(
            new PortEndpoint(source.Id, PortId.Right),
            new PortEndpoint(target.Id, PortId.Left)
        );
        board.AddEdge(edge);
        var canvas = RenderCanvas(board);
        await canvas.Press(
            100,
            25,
            PointerPress.PrimaryButton,
            role: HitRole.Edge,
            entityId: edge.Id
        );
        await canvas.Release(100, 25);

        await PressMenuKey(canvas);

        Assert.Equal("Selection actions", MenuLabel(canvas));
        Assert.Contains("left: 50px", MenuStyle(canvas));
        Assert.Contains("top: 25px", MenuStyle(canvas));
    }

    [Fact]
    public async Task WithNothingSelectedTheCanvasMenuOpensAtTheViewportCentre()
    {
        var board = new Board();
        AddInstance(board, 40, 30);
        var canvas = RenderCanvas(board);

        await PressMenuKey(canvas);

        Assert.Equal("Canvas actions", MenuLabel(canvas));
        Assert.Contains("left: 400px", MenuStyle(canvas));
        Assert.Contains("top: 300px", MenuStyle(canvas));
    }

    [Fact]
    public async Task ASelectionScrolledOutOfViewDrawsTheMenuInsideTheContainer()
    {
        var board = new Board();
        AddInstance(board, 5000, 30);
        var canvas = RenderCanvas(board);
        await canvas.InvokeAsync(() => canvas.Instance.OnSelectAllPressed());

        await PressMenuKey(canvas);

        Assert.Contains($"left: {800 - ContextMenuPlacement.Width}px", MenuStyle(canvas));
    }

    [Fact]
    public async Task AKeyboardMenuTellsTheBrowserItWasOpenedFromTheKeyboard()
    {
        var board = new Board();
        AddInstance(board, 40, 30);
        var canvas = RenderCanvas(board);
        canvas.ClickOn(canvas.Find(".component-container"));

        await PressMenuKey(canvas);

        Assert.True(OpenedFromKeyboardAsRegistered());
    }

    [Fact]
    public async Task APointerMenuDoesNotClaimTheKeyboard()
    {
        var canvas = RenderCanvas(new Board());

        await canvas.ClickCanvas(100, 100, PointerPress.SecondaryButton);

        Assert.False(OpenedFromKeyboardAsRegistered());
    }

    private bool OpenedFromKeyboardAsRegistered()
    {
        var registration = Assert.Single(JSInterop.Invocations["registerMenu"]);
        var options = registration.Arguments[2]!;
        return (bool)options.GetType().GetProperty("openedFromKeyboard")!.GetValue(options)!;
    }

    [Fact]
    public async Task TheMenuKeyDoesNothingWhileAPressOwnsTheBoard()
    {
        var board = new Board();
        var instance = AddInstance(board, 40, 30);
        var canvas = RenderCanvas(board);
        await canvas.Press(
            50,
            40,
            PointerPress.PrimaryButton,
            role: HitRole.Instance,
            entityId: instance.Id
        );

        await PressMenuKey(canvas);

        Assert.Empty(canvas.FindAll(".d12-context-menu"));
    }

    [Fact]
    public async Task ASecondaryPressTheBrowserKeptSelectsItsShapeAndOpensNoMenu()
    {
        var board = new Board();
        var instance = AddInstance(board, 40, 30);
        var canvas = RenderCanvas(board);

        await canvas.Press(
            50,
            40,
            PointerPress.SecondaryButton,
            role: HitRole.AuthorContent,
            entityId: instance.Id,
            menuVerdict: PointerPress.BrowserMenuVerdict
        );

        Assert.Empty(canvas.FindAll(".d12-context-menu"));
        Assert.Equal("true", canvas.Find(".component-container").GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task ASecondaryPressTheCanvasKeptOnAuthorContentOpensTheObjectMenu()
    {
        var board = new Board();
        var instance = AddInstance(board, 40, 30);
        var canvas = RenderCanvas(board);

        await canvas.Press(
            50,
            40,
            PointerPress.SecondaryButton,
            role: HitRole.AuthorContent,
            entityId: instance.Id,
            menuVerdict: PointerPress.CanvasMenuVerdict
        );
        await canvas.Release(50, 40, PointerPress.SecondaryButton);

        Assert.Equal("Selection actions", MenuLabel(canvas));
    }
}
