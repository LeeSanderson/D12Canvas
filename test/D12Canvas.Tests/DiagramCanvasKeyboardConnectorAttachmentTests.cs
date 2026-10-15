using System.Threading.Tasks;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// Keyboard connector attachment: Enter enters/advances a port-focus pick on
// whichever instance currently has real DOM focus, starting on an auto endpoint, arrow keys choose
// among its four standard ports (Top/Right/Bottom/Left mapping directly onto Up/Right/Down/Left),
// Space steps from auto through every port, and a second Enter -
// reached once a source port is already armed - completes the connection exactly like a mouse
// port-to-port drag would. These tests establish "currently focused" via .Focus() (a real
// AngleSharp focus event, routing through ComponentContainer.HandleFocus/DiagramCanvas.FocusEntity)
// rather than .Click(), matching DiagramCanvasAdditiveTraversalTests's own convention -
// native Tab/Shift+Tab itself is never intercepted, so simulating "the user tabbed to the next
// instance" is just a second real .Focus() call.
public class DiagramCanvasKeyboardConnectorAttachmentTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasKeyboardConnectorAttachmentTests()
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

    private static ComponentInstance AddInstance(Board board, string text, double x, double y) =>
        AddInstance(board, text, x, y, 50, 50);

    private static ComponentInstance AddInstance(
        Board board,
        string text,
        double x,
        double y,
        double width,
        double height
    )
    {
        var instance = new ComponentInstance(
            ComponentTypeKey,
            new TestProps(text),
            new Bounds(x, y, width, height)
        );
        board.AddComponent(instance);
        return instance;
    }

    [Fact]
    public async Task PickingSourceAndTargetPortsByKeyboardCreatesTheSameEdgeAPointerDragWould()
    {
        var board = new Board();
        var source = AddInstance(board, "First", 0, 0);
        var target = AddInstance(board, "Second", 100, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.FindAll(".component-container")[0].Focus();
        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed()); // enter port-focus (auto)
        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed("ArrowRight", false));
        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed()); // arm Right as source

        canvas.FindAll(".component-container")[1].Focus(); // the Tab a keyboard user would press
        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed()); // enter port-focus (auto)
        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed("ArrowLeft", false));
        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed()); // confirm target

        var edge = Assert.Single(board.Edges);
        Assert.Equal(new PortEndpoint(source.Id, PortId.Right), edge.Source);
        Assert.Equal(new PortEndpoint(target.Id, PortId.Left), edge.Target);
    }

    [Fact]
    public async Task TheCreatedEdgeUndoesAndRedoesWithTheSameAttachments()
    {
        var board = new Board();
        var source = AddInstance(board, "First", 0, 0);
        var target = AddInstance(board, "Second", 100, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.FindAll(".component-container")[0].Focus();
        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed());
        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed()); // arm auto as source

        canvas.FindAll(".component-container")[1].Focus();
        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed());
        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed()); // confirm auto as target
        Assert.Single(board.Edges);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());
        Assert.Empty(board.Edges);

        await canvas.InvokeAsync(() => canvas.Instance.OnRedoPressed());
        var edge = Assert.Single(board.Edges);
        Assert.Equal(new AutoPortEndpoint(source.Id), edge.Source);
        Assert.Equal(new AutoPortEndpoint(target.Id), edge.Target);
    }

    // Mirrors DiagramCanvasPortDragTests.DroppingBackOnTheSameStartingPortCreatesNoEdge - the
    // keyboard equivalent of "dropping back on the exact port the drag started from".
    [Fact]
    public async Task ConfirmingBackOnTheExactSamePortCreatesNoEdge()
    {
        var board = new Board();
        AddInstance(board, "First", 0, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find(".component-container").Focus();
        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed()); // enter port-focus (auto)
        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed()); // arm auto as source
        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed()); // re-enter (still auto)
        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed()); // confirm auto as target

        Assert.Empty(board.Edges);
    }

    [Fact]
    public async Task ConfirmingAnotherPortOnTheSourceInstanceCreatesNoEdge()
    {
        var board = new Board();
        AddInstance(board, "First", 0, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find(".component-container").Focus();
        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed());
        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed("ArrowRight", false));
        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed()); // arm Right as source
        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed());
        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed("ArrowLeft", false));
        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed()); // confirm Left

        Assert.Empty(board.Edges);
    }

    [Fact]
    public async Task EnterPicksAutoFirstAndHighlightsEveryStandardPortButNoCustomOne()
    {
        var board = new Board();
        var instance = new ComponentInstance(
            ComponentTypeKey,
            new TestProps("First"),
            new Bounds(0, 0, 50, 50),
            customPorts: new[] { new PortDef(0.25, 0) }
        );
        board.AddComponent(instance);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find(".component-container").Focus();
        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed());

        Assert.Equal(
            [
                "port port-top port-focused",
                "port port-right port-focused",
                "port port-bottom port-focused",
                "port port-left port-focused",
            ],
            canvas.FindAll(".port-focused").Select(port => port.ClassName)
        );
    }

    [Fact]
    public async Task EscapeCancelsAHalfBuiltConnectionCleanly()
    {
        var board = new Board();
        AddInstance(board, "First", 0, 0);
        AddInstance(board, "Second", 100, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.FindAll(".component-container")[0].Focus();
        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed());
        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed()); // arm Top as source
        Assert.NotEmpty(canvas.FindAll(".port-focused"));

        await canvas.InvokeAsync(() => canvas.Instance.OnEscapePressed());

        Assert.Empty(canvas.FindAll(".port-focused"));

        // A fresh Enter+Enter on the other instance now only arms a NEW source - it doesn't
        // silently complete the connection Escape just cancelled.
        canvas.FindAll(".component-container")[1].Focus();
        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed());
        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed());

        Assert.Empty(board.Edges);
    }

    [Fact]
    public async Task MovingFocusAwayMidPickClearsTheStalePickOnTheOldInstance()
    {
        var board = new Board();
        AddInstance(board, "First", 0, 0);
        AddInstance(board, "Second", 100, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.FindAll(".component-container")[0].Focus();
        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed());
        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed("ArrowRight", false));
        Assert.NotEmpty(
            canvas.FindAll(".component-container")[0].QuerySelectorAll(".port-focused")
        );

        canvas.FindAll(".component-container")[1].Focus();

        Assert.Empty(canvas.FindAll(".component-container")[0].QuerySelectorAll(".port-focused"));

        // Enter on the new instance starts a fresh pick (defaulting to auto) rather than
        // resuming/completing anything left over from the abandoned pick on the old one.
        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed());

        Assert.Equal(
            4,
            canvas.FindAll(".component-container")[1].QuerySelectorAll(".port-focused").Length
        );
        Assert.Empty(board.Edges);
    }

    [Fact]
    public async Task TheArmedSourcePortStaysHighlightedWhileNavigatingToTheTarget()
    {
        var board = new Board();
        AddInstance(board, "First", 0, 0);
        AddInstance(board, "Second", 100, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.FindAll(".component-container")[0].Focus();
        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed());
        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed("ArrowUp", false));
        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed()); // arms Top as source

        canvas.FindAll(".component-container")[1].Focus();

        var firstContainer = canvas.FindAll(".component-container")[0];
        Assert.NotNull(firstContainer.QuerySelector(".port-top.port-focused"));
        // Focus-follows-selection means the source is no longer the selected/focused instance -
        // the highlight surviving that is exactly what lets a keyboard user still see where the
        // connection started while they navigate to the target.
        Assert.Null(firstContainer.GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task EnterWhileAGroupTabStopIsFocusedIsANoOpGroupsHaveNoPorts()
    {
        var board = new Board();
        AddInstance(board, "First", 0, 0);
        AddInstance(board, "Second", 100, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var containers = canvas.FindAll(".component-container");
        canvas.ClickOn(containers[0]);
        canvas.ClickOn(containers[1], shift: true);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());
        canvas.Find(".group-tab-stop").Focus();

        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed());

        Assert.Empty(canvas.FindAll(".port-focused"));
        Assert.Empty(board.Edges);
    }

    [Fact]
    public async Task AltArrowDoesNotResizeWhileMidPick()
    {
        var board = new Board();
        var instance = AddInstance(board, "First", 0, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find(".component-container").Focus();
        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed()); // enter port-focus

        await canvas.InvokeAsync(() => canvas.Instance.OnAltArrowKeyPressed("ArrowRight", false));

        Assert.Equal(new Bounds(0, 0, 50, 50), instance.Bounds);
    }

    // Arrow keys only ever jump to one of an instance's four standard ports - Space is the only
    // way to reach a custom port, cycling from auto through Board.AllPorts's own order (every
    // standard port, then every custom one).
    [Fact]
    public async Task SpaceCyclesToACustomPortSoAKeyboardConnectionCanUseOne()
    {
        var board = new Board();
        var customPort = new PortDef(0.5, 0);
        var source = new ComponentInstance(
            ComponentTypeKey,
            new TestProps("First"),
            new Bounds(0, 0, 50, 50),
            customPorts: new[] { customPort }
        );
        board.AddComponent(source);
        var target = AddInstance(board, "Second", 100, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.FindAll(".component-container")[0].Focus();
        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed()); // enter port-focus (auto)
        for (var i = 0; i < 5; i++) // auto -> Top -> Right -> Bottom -> Left -> the custom port
        {
            await canvas.InvokeAsync(() => canvas.Instance.OnSpacePressed());
        }
        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed()); // arm the custom port

        canvas.FindAll(".component-container")[1].Focus();
        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed());
        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed()); // confirm auto as target

        var edge = Assert.Single(board.Edges);
        Assert.Equal(new CustomPortEndpoint(source.Id, customPort.Id), edge.Source);
        Assert.Equal(new AutoPortEndpoint(target.Id), edge.Target);
    }

    [Fact]
    public async Task SpaceStepsFromAutoToTopAlone()
    {
        var board = new Board();
        AddInstance(board, "First", 0, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find(".component-container").Focus();
        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed());
        await canvas.InvokeAsync(() => canvas.Instance.OnSpacePressed());

        Assert.Equal(
            "port port-top port-focused",
            Assert.Single(canvas.FindAll(".port-focused")).ClassName
        );
    }

    [Fact]
    public async Task SpaceWrapsFromTheLastPortBackToAuto()
    {
        var board = new Board();
        var customPort = new PortDef(0.5, 0);
        var instance = new ComponentInstance(
            ComponentTypeKey,
            new TestProps("First"),
            new Bounds(0, 0, 50, 50),
            customPorts: new[] { customPort }
        );
        board.AddComponent(instance);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find(".component-container").Focus();
        await canvas.InvokeAsync(() => canvas.Instance.OnEnterPressed()); // auto
        for (var i = 0; i < 6; i++) // auto -> Top -> Right -> Bottom -> Left -> custom -> auto
        {
            await canvas.InvokeAsync(() => canvas.Instance.OnSpacePressed());
        }

        Assert.Equal(4, canvas.FindAll(".port-focused").Count);
        Assert.Empty(canvas.FindAll(".custom-port.port-focused"));
    }
}
