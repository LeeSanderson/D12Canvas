using System.Threading.Tasks;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

public class DiagramCanvasUndoRedoTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasUndoRedoTests()
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

    private static ComponentInstance AddInstance(
        Board board,
        double x,
        double y,
        double width = 50,
        double height = 50,
        int zIndex = 0
    )
    {
        var instance = new ComponentInstance(
            ComponentTypeKey,
            new TestProps(),
            new Bounds(x, y, width, height),
            zIndex
        );
        board.AddComponent(instance);
        return instance;
    }

    [Fact]
    public async Task ChangedFiresOnAMoveAndOnUndo()
    {
        var board = new Board();
        AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        var changedCount = 0;
        canvas.Instance.Changed += (_, _) => changedCount++;

        var container = canvas.Find(".component-container");
        canvas.ClickOn(container);
        container = canvas.Find(".component-container");
        canvas.DragOn(container, (300, 200), (340, 175));
        Assert.Equal(1, changedCount);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal(2, changedCount);
    }

    [Fact]
    public async Task UndoAfterAMoveRestoresThePriorBounds()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        canvas.ClickOn(canvas.Find(".component-container"));
        var container = canvas.Find(".component-container");
        canvas.DragOn(container, (300, 200), (340, 175));
        Assert.Equal(new Bounds(140, 75, 50, 50), instance.Bounds);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal(new Bounds(100, 100, 50, 50), instance.Bounds);
    }

    [Fact]
    public async Task RedoAfterUndoReappliesTheMove()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        canvas.ClickOn(canvas.Find(".component-container"));
        var container = canvas.Find(".component-container");
        canvas.DragOn(container, (300, 200), (340, 175));
        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        await canvas.InvokeAsync(() => canvas.Instance.OnRedoPressed());

        Assert.Equal(new Bounds(140, 75, 50, 50), instance.Bounds);
    }

    [Fact]
    public async Task UndoAfterAResizeRestoresThePriorBounds()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        canvas.ClickOn(canvas.Find(".component-container"));
        canvas.DragHandle(canvas.Find(".resize-handle.bottom-right"), (300, 200), (340, 225));
        Assert.Equal(new Bounds(100, 100, 90, 75), instance.Bounds);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal(new Bounds(100, 100, 50, 50), instance.Bounds);
    }

    [Fact]
    public async Task RedoAfterUndoReappliesTheResize()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        canvas.ClickOn(canvas.Find(".component-container"));
        canvas.DragHandle(canvas.Find(".resize-handle.bottom-right"), (300, 200), (340, 225));
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
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        var containers = canvas.FindAll(".component-container");
        canvas.ClickOn(containers[0]);
        canvas.ClickOn(containers[1], shift: true);
        containers = canvas.FindAll(".component-container");
        canvas.DragOn(containers[0], (300, 200), (340, 175));
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
        var first = AddInstance(board, 0, 0, 50, 50);
        var second = AddInstance(board, 100, 0, 100, 50);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        var containers = canvas.FindAll(".component-container");
        canvas.ClickOn(containers[0]);
        canvas.ClickOn(containers[1], shift: true);
        canvas.DragHandle(canvas.Find(".group-resize-handle.bottom-right"), (300, 200), (400, 250));
        Assert.Equal(new Bounds(0, 0, 75, 100), first.Bounds);
        Assert.Equal(new Bounds(150, 0, 150, 100), second.Bounds);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal(new Bounds(0, 0, 50, 50), first.Bounds);
        Assert.Equal(new Bounds(100, 0, 100, 50), second.Bounds);
    }

    [Fact]
    public async Task UndoAfterAPersistedGroupMoveRevertsEveryMemberInOneStep()
    {
        var board = new Board();
        var first = AddInstance(board, 100, 100);
        var second = AddInstance(board, 300, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        var containers = canvas.FindAll(".component-container");
        canvas.ClickOn(containers[0]);
        canvas.ClickOn(containers[1], shift: true);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        containers = canvas.FindAll(".component-container");
        canvas.DragOn(containers[0], (300, 200), (340, 175));
        Assert.Equal(new Bounds(140, 75, 50, 50), first.Bounds);
        Assert.Equal(new Bounds(340, 75, 50, 50), second.Bounds);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal(new Bounds(100, 100, 50, 50), first.Bounds);
        Assert.Equal(new Bounds(300, 100, 50, 50), second.Bounds);
    }

    [Fact]
    public async Task UndoAfterAPersistedGroupResizeRevertsEveryMemberInOneStep()
    {
        var board = new Board();
        var first = AddInstance(board, 0, 0, 50, 50);
        var second = AddInstance(board, 100, 0, 100, 50);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        var containers = canvas.FindAll(".component-container");
        canvas.ClickOn(containers[0]);
        canvas.ClickOn(containers[1], shift: true);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());

        canvas.DragHandle(canvas.Find(".group-resize-handle.bottom-right"), (300, 200), (400, 250));
        Assert.Equal(new Bounds(0, 0, 75, 100), first.Bounds);
        Assert.Equal(new Bounds(150, 0, 150, 100), second.Bounds);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal(new Bounds(0, 0, 50, 50), first.Bounds);
        Assert.Equal(new Bounds(100, 0, 100, 50), second.Bounds);
    }

    [Fact]
    public async Task UndoAfterAPersistedGroupLayeringCommandRevertsEveryMemberInOneStep()
    {
        var board = new Board();
        var first = AddInstance(board, 0, 0, zIndex: 2);
        var second = AddInstance(board, 300, 0, zIndex: 3);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        var containers = canvas.FindAll(".component-container");
        canvas.ClickOn(containers[0]);
        canvas.ClickOn(containers[1], shift: true);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());
        await canvas.InvokeAsync(() => canvas.Instance.OnBringToFrontPressed());
        Assert.Equal(4, first.ZIndex);
        Assert.Equal(5, second.ZIndex);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal(2, first.ZIndex);
        Assert.Equal(3, second.ZIndex);
    }

    [Fact]
    public async Task UndoAfterAPersistedGroupBringForwardLayeringCommandRevertsEveryMemberInOneStep()
    {
        var board = new Board();
        var first = AddInstance(board, 0, 0, zIndex: 1);
        var second = AddInstance(board, 300, 0, zIndex: 2);
        AddInstance(board, 600, 0, zIndex: 3);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        var containers = canvas.FindAll(".component-container");
        canvas.ClickOn(containers[0]);
        canvas.ClickOn(containers[1], shift: true);
        await canvas.InvokeAsync(() => canvas.Instance.OnGroupPressed());
        await canvas.InvokeAsync(() => canvas.Instance.OnBringForwardPressed());
        Assert.Equal(2, first.ZIndex);
        Assert.Equal(3, second.ZIndex);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal(1, first.ZIndex);
        Assert.Equal(2, second.ZIndex);
    }

    [Fact]
    public async Task ANewGestureAfterUndoClearsRedo()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        canvas.ClickOn(canvas.Find(".component-container"));
        var container = canvas.Find(".component-container");
        canvas.DragOn(container, (300, 200), (340, 175));
        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        container = canvas.Find(".component-container");
        canvas.DragOn(container, (300, 200), (260, 190));
        Assert.Equal(new Bounds(60, 90, 50, 50), instance.Bounds);

        await canvas.InvokeAsync(() => canvas.Instance.OnRedoPressed());

        Assert.Equal(new Bounds(60, 90, 50, 50), instance.Bounds);
    }

    [Fact]
    public async Task UndoWithNoHistoryIsANoOp()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal(new Bounds(100, 100, 50, 50), instance.Bounds);
    }

    [Fact]
    public async Task RedoWithNothingUndoneIsANoOp()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        await canvas.InvokeAsync(() => canvas.Instance.OnRedoPressed());

        Assert.Equal(new Bounds(100, 100, 50, 50), instance.Bounds);
    }

    [Fact]
    public async Task UndoAfterAClickToAddRemovesThePlacedInstance()
    {
        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd(ComponentTypeKey));
        Assert.Single(board.Components);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Empty(board.Components);
    }

    [Fact]
    public async Task RedoAfterUndoingAClickToAddRestoresTheInstanceUnderTheSameId()
    {
        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd(ComponentTypeKey));
        var placedId = Assert.Single(board.Components).Id;
        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        await canvas.InvokeAsync(() => canvas.Instance.OnRedoPressed());

        Assert.Equal(placedId, Assert.Single(board.Components).Id);
    }

    [Fact]
    public async Task UndoAfterADragDropPlacementRemovesThePlacedInstance()
    {
        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        canvas.Instance.BeginPaletteDrag(ComponentTypeKey);
        canvas.Find(".diagram-canvas").Drop(new DragEventArgs { ClientX = 300, ClientY = 250 });
        Assert.Single(board.Components);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Empty(board.Components);
    }

    [Fact]
    public async Task RedoAfterUndoingADragDropPlacementRestoresTheInstanceUnderTheSameId()
    {
        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        canvas.Instance.BeginPaletteDrag(ComponentTypeKey);
        canvas.Find(".diagram-canvas").Drop(new DragEventArgs { ClientX = 300, ClientY = 250 });
        var placedId = Assert.Single(board.Components).Id;
        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        await canvas.InvokeAsync(() => canvas.Instance.OnRedoPressed());

        Assert.Equal(placedId, Assert.Single(board.Components).Id);
    }

    [Fact]
    public async Task UndoAfterASingleSelectionDeleteRestoresTheInstance()
    {
        var board = new Board();
        var instance = AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        canvas.ClickOn(canvas.Find(".component-container"));

        await canvas.InvokeAsync(() => canvas.Instance.OnDeletePressed());
        Assert.Empty(board.Components);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        var restored = Assert.Single(board.Components);
        Assert.Equal(instance.Id, restored.Id);
        Assert.Equal(new Bounds(100, 100, 50, 50), restored.Bounds);
    }

    [Fact]
    public async Task RedoAfterUndoingASingleSelectionDeleteRemovesItAgain()
    {
        var board = new Board();
        AddInstance(board, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        canvas.ClickOn(canvas.Find(".component-container"));
        await canvas.InvokeAsync(() => canvas.Instance.OnDeletePressed());
        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        await canvas.InvokeAsync(() => canvas.Instance.OnRedoPressed());

        Assert.Empty(board.Components);
    }

    [Fact]
    public async Task UndoAfterAMultiSelectionDeleteRestoresEveryInstanceInOneStep()
    {
        var board = new Board();
        var first = AddInstance(board, 0, 0);
        var second = AddInstance(board, 100, 0);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        var containers = canvas.FindAll(".component-container");
        canvas.ClickOn(containers[0]);
        canvas.ClickOn(containers[1], shift: true);

        await canvas.InvokeAsync(() => canvas.Instance.OnDeletePressed());
        Assert.Empty(board.Components);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal(2, board.Components.Count);
        Assert.Equal(new Bounds(0, 0, 50, 50), board.GetComponent(first.Id)?.Bounds);
        Assert.Equal(new Bounds(100, 0, 50, 50), board.GetComponent(second.Id)?.Bounds);
    }

    [Fact]
    public async Task RedoAfterUndoingAMultiSelectionDeleteRemovesEveryInstanceAgain()
    {
        var board = new Board();
        AddInstance(board, 0, 0);
        AddInstance(board, 100, 0);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        var containers = canvas.FindAll(".component-container");
        canvas.ClickOn(containers[0]);
        canvas.ClickOn(containers[1], shift: true);
        await canvas.InvokeAsync(() => canvas.Instance.OnDeletePressed());
        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        await canvas.InvokeAsync(() => canvas.Instance.OnRedoPressed());

        Assert.Empty(board.Components);
    }

    [Fact]
    public async Task UndoAfterCreatingAnEdgeRemovesIt()
    {
        var board = new Board();
        AddInstance(board, 100, 100, 100, 100);
        AddInstance(board, 300, 100, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        var containers = canvas.FindAll(".component-container");
        canvas.DragConnectorToPort(
            canvas.SelectedPortSpan(EntityIdOf(containers[0]), "Right"),
            (200, 150),
            (300, 150),
            EntityIdOf(containers[1]),
            "Left"
        );
        Assert.Single(board.Edges);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Empty(board.Edges);
    }

    [Fact]
    public async Task RedoAfterUndoingAnEdgeCreationRestoresItWithTheSameAttachments()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100, 100, 100);
        var target = AddInstance(board, 300, 100, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        var containers = canvas.FindAll(".component-container");
        canvas.DragConnectorToPort(
            canvas.SelectedPortSpan(EntityIdOf(containers[0]), "Right"),
            (200, 150),
            (300, 150),
            EntityIdOf(containers[1]),
            "Left"
        );
        var createdId = Assert.Single(board.Edges).Id;
        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        await canvas.InvokeAsync(() => canvas.Instance.OnRedoPressed());

        var restored = Assert.Single(board.Edges);
        Assert.Equal(createdId, restored.Id);
        Assert.Equal(new PortEndpoint(source.Id, PortId.Right), restored.Source);
        Assert.Equal(new PortEndpoint(target.Id, PortId.Left), restored.Target);
    }

    [Fact]
    public async Task UndoAfterDeletingASelectedEdgeRestoresItWithBothAttachedEndpointsIntact()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100, 100, 100);
        var target = AddInstance(board, 300, 100, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        var containers = canvas.FindAll(".component-container");
        canvas.DragConnectorToPort(
            canvas.SelectedPortSpan(EntityIdOf(containers[0]), "Right"),
            (200, 150),
            (300, 150),
            EntityIdOf(containers[1]),
            "Left"
        );
        var edgeId = Assert.Single(board.Edges).Id;

        canvas.ClickElement(canvas.Find(".edge-hit"));
        await canvas.InvokeAsync(() => canvas.Instance.OnDeletePressed());
        Assert.Empty(board.Edges);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        var restored = Assert.Single(board.Edges);
        Assert.Equal(edgeId, restored.Id);
        Assert.Equal(new PortEndpoint(source.Id, PortId.Right), restored.Source);
        Assert.Equal(new PortEndpoint(target.Id, PortId.Left), restored.Target);
    }

    [Fact]
    public async Task UndoAfterDeletingASelectedEdgeRestoresAFloatingEndpointIntact()
    {
        var board = new Board();
        var source = AddInstance(board, 100, 100, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        canvas.ReturnToOrigin();
        canvas.DragConnector(canvas.SelectedPortSpan(source.Id, "Right"), (200, 150), (190, 400));
        var edgeId = Assert.Single(board.Edges).Id;

        canvas.ClickElement(canvas.Find(".edge-hit"));
        await canvas.InvokeAsync(() => canvas.Instance.OnDeletePressed());
        Assert.Empty(board.Edges);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        var restored = Assert.Single(board.Edges);
        Assert.Equal(edgeId, restored.Id);
        Assert.Equal(new PortEndpoint(source.Id, PortId.Right), restored.Source);
        Assert.Equal(new FloatingEndpoint(190, 400), restored.Target);
    }

    [Fact]
    public async Task RedoAfterUndoingAnEdgeDeleteRemovesItAgain()
    {
        var board = new Board();
        AddInstance(board, 100, 100, 100, 100);
        AddInstance(board, 300, 100, 100, 100);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        var containers = canvas.FindAll(".component-container");
        canvas.DragConnectorToPort(
            canvas.SelectedPortSpan(EntityIdOf(containers[0]), "Right"),
            (200, 150),
            (300, 150),
            EntityIdOf(containers[1]),
            "Left"
        );

        canvas.ClickElement(canvas.Find(".edge-hit"));
        await canvas.InvokeAsync(() => canvas.Instance.OnDeletePressed());
        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        await canvas.InvokeAsync(() => canvas.Instance.OnRedoPressed());

        Assert.Empty(board.Edges);
    }

    private static Guid EntityIdOf(AngleSharp.Dom.IElement container) =>
        Guid.Parse(container.GetAttribute("data-d12-entity")!);
}
