using System.Threading.Tasks;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// Ticket 37: completing a move or resize gesture records one ChangeBoundsCommand in a
// session-scoped CommandHistory (ADR 0007) - Ctrl+Z/Ctrl+Shift+Z (wired via JS, tested here by
// invoking the JSInvokable handlers directly, matching DiagramCanvasDeleteSelectionTests) undo/
// redo it. A multi-select move/resize commits as one CompositeCommand, so one undo reverts every
// member at once.
public class DiagramCanvasUndoRedoTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasUndoRedoTests()
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

    [Fact]
    public async Task UndoAfterAMoveRestoresThePriorBounds()
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

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal(new Bounds(100, 100, 50, 50), instance.Bounds);
    }

    [Fact]
    public async Task RedoAfterUndoReappliesTheMove()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find(".component-container").Click();
        var container = canvas.Find(".component-container");
        container.MouseDown(new MouseEventArgs { ClientX = 300, ClientY = 200 });
        container.MouseMove(new MouseEventArgs { ClientX = 340, ClientY = 175 });
        container.MouseUp(new MouseEventArgs { ClientX = 340, ClientY = 175 });
        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        await canvas.InvokeAsync(() => canvas.Instance.OnRedoPressed());

        Assert.Equal(new Bounds(140, 75, 50, 50), instance.Bounds);
    }

    [Fact]
    public async Task UndoAfterAResizeRestoresThePriorBounds()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find(".component-container").Click();
        var handle = canvas.Find(".resize-handle.bottom-right");
        handle.MouseDown(new MouseEventArgs { ClientX = 300, ClientY = 200 });
        handle.MouseMove(new MouseEventArgs { ClientX = 340, ClientY = 225 });
        handle.MouseUp(new MouseEventArgs { ClientX = 340, ClientY = 225 });
        Assert.Equal(new Bounds(100, 100, 90, 75), instance.Bounds);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal(new Bounds(100, 100, 50, 50), instance.Bounds);
    }

    [Fact]
    public async Task RedoAfterUndoReappliesTheResize()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find(".component-container").Click();
        var handle = canvas.Find(".resize-handle.bottom-right");
        handle.MouseDown(new MouseEventArgs { ClientX = 300, ClientY = 200 });
        handle.MouseMove(new MouseEventArgs { ClientX = 340, ClientY = 225 });
        handle.MouseUp(new MouseEventArgs { ClientX = 340, ClientY = 225 });
        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        await canvas.InvokeAsync(() => canvas.Instance.OnRedoPressed());

        Assert.Equal(new Bounds(100, 100, 90, 75), instance.Bounds);
    }

    [Fact]
    public async Task UndoAfterAGroupMoveRevertsEveryMemberInOneStep()
    {
        var board = new Board();
        var first = AddInstance(board, 100, 100);
        var second = AddInstance(board, 300, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var containers = canvas.FindAll(".component-container");
        containers[0].Click();
        containers[1].Click(new MouseEventArgs { ShiftKey = true });
        containers = canvas.FindAll(".component-container");
        containers[0].MouseDown(new MouseEventArgs { ClientX = 300, ClientY = 200 });
        containers[0].MouseMove(new MouseEventArgs { ClientX = 340, ClientY = 175 });
        containers[0].MouseUp(new MouseEventArgs { ClientX = 340, ClientY = 175 });
        Assert.Equal(new Bounds(140, 75, 50, 50), first.Bounds);
        Assert.Equal(new Bounds(340, 75, 50, 50), second.Bounds);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal(new Bounds(100, 100, 50, 50), first.Bounds);
        Assert.Equal(new Bounds(300, 100, 50, 50), second.Bounds);
    }

    [Fact]
    public async Task UndoAfterAGroupResizeRevertsEveryMemberInOneStep()
    {
        var board = new Board();
        var first = AddInstance(board, 0, 0, 50, 50); // (0,0)-(50,50)
        var second = AddInstance(board, 100, 0, 100, 50); // (100,0)-(200,50)
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var containers = canvas.FindAll(".component-container");
        containers[0].Click();
        containers[1].Click(new MouseEventArgs { ShiftKey = true });
        var handle = canvas.Find(".group-resize-handle.bottom-right");
        handle.MouseDown(new MouseEventArgs { ClientX = 300, ClientY = 200 });
        handle.MouseMove(new MouseEventArgs { ClientX = 400, ClientY = 250 });
        handle.MouseUp(new MouseEventArgs { ClientX = 400, ClientY = 250 });
        Assert.Equal(new Bounds(0, 0, 75, 100), first.Bounds);
        Assert.Equal(new Bounds(150, 0, 150, 100), second.Bounds);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal(new Bounds(0, 0, 50, 50), first.Bounds);
        Assert.Equal(new Bounds(100, 0, 100, 50), second.Bounds);
    }

    [Fact]
    public async Task ANewGestureAfterUndoClearsRedo()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find(".component-container").Click();
        var container = canvas.Find(".component-container");
        container.MouseDown(new MouseEventArgs { ClientX = 300, ClientY = 200 });
        container.MouseMove(new MouseEventArgs { ClientX = 340, ClientY = 175 });
        container.MouseUp(new MouseEventArgs { ClientX = 340, ClientY = 175 });
        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        // A fresh gesture after the undo - the undone move must never come back via redo.
        container = canvas.Find(".component-container");
        container.MouseDown(new MouseEventArgs { ClientX = 300, ClientY = 200 });
        container.MouseMove(new MouseEventArgs { ClientX = 260, ClientY = 190 });
        container.MouseUp(new MouseEventArgs { ClientX = 260, ClientY = 190 });
        Assert.Equal(new Bounds(60, 90, 50, 50), instance.Bounds);

        await canvas.InvokeAsync(() => canvas.Instance.OnRedoPressed());

        Assert.Equal(new Bounds(60, 90, 50, 50), instance.Bounds);
    }

    [Fact]
    public async Task UndoWithNoHistoryIsANoOp()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal(new Bounds(100, 100, 50, 50), instance.Bounds);
    }

    [Fact]
    public async Task RedoWithNothingUndoneIsANoOp()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        await canvas.InvokeAsync(() => canvas.Instance.OnRedoPressed());

        Assert.Equal(new Bounds(100, 100, 50, 50), instance.Bounds);
    }
}
