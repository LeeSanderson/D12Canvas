using System.Linq;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

public class DiagramCanvasPlacementTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasPlacementTests()
    {
        SetupDiagramCanvasJsModule();
        SetupComponentContainerJsModule();
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
    public void DroppingAPendingPaletteDragPlacesANewInstanceCenteredOnTheDropPoint()
    {
        RegisterTestComponent(new ComponentSize(120, 80));
        var board = new Board();

        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Instance.BeginPaletteDrag(ComponentTypeKey);

        canvas.Find(".diagram-canvas").Drop(new DragEventArgs { ClientX = 300, ClientY = 250 });

        var instance = Assert.Single(board.Components);
        Assert.Equal(ComponentTypeKey, instance.ComponentTypeKey);
        Assert.Equal("default", ((TestProps)instance.Props).Text);
        Assert.Equal(new Bounds(240, 210, 120, 80), instance.Bounds);
    }

    [Fact]
    public void FallsBackToTheComponentContainerDefaultSizeWhenNoDefaultSizeIsRegistered()
    {
        RegisterTestComponent(defaultSize: null);
        var board = new Board();

        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Instance.BeginPaletteDrag(ComponentTypeKey);

        canvas.Find(".diagram-canvas").Drop(new DragEventArgs { ClientX = 300, ClientY = 250 });

        var instance = Assert.Single(board.Components);
        Assert.Equal(new Bounds(200, 175, 200, 150), instance.Bounds);
    }

    [Fact]
    public void ConvertsTheDropPointToBoardCoordinatesAccountingForPan()
    {
        RegisterTestComponent(new ComponentSize(120, 80));
        var board = new Board();

        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
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
        canvas.Find(".diagram-canvas").MouseMove(new MouseEventArgs { ClientX = 50, ClientY = 40 }); // pans by (-50, -60)

        canvas.Instance.BeginPaletteDrag(ComponentTypeKey);
        canvas.Find(".diagram-canvas").Drop(new DragEventArgs { ClientX = 300, ClientY = 250 });

        var instance = Assert.Single(board.Components);
        Assert.Equal(new Bounds(290, 270, 120, 80), instance.Bounds);
    }

    [Fact]
    public void ConvertsTheDropPointToBoardCoordinatesAccountingForZoom()
    {
        RegisterTestComponent(new ComponentSize(120, 80));
        var board = new Board();

        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Find(".diagram-canvas").Wheel(new WheelEventArgs { DeltaY = -100 }); // zooms to scale 1.1

        canvas.Instance.BeginPaletteDrag(ComponentTypeKey);
        canvas.Find(".diagram-canvas").Drop(new DragEventArgs { ClientX = 300, ClientY = 250 });

        // Computed with the same arithmetic ZoomPanTracker uses (1.0 + 0.1), rather than the
        // decimal literal 1.1, so this can't disagree with production code over double rounding.
        var scale = 1.0 + 0.1;
        var instance = Assert.Single(board.Components);
        Assert.Equal(300 / scale - 60, instance.Bounds.X, precision: 10);
        Assert.Equal(250 / scale - 40, instance.Bounds.Y, precision: 10);
    }

    [Fact]
    public void DroppingWithoutAPrecedingPaletteDragLeavesTheBoardUnchanged()
    {
        RegisterTestComponent(new ComponentSize(120, 80));
        var board = new Board();

        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Find(".diagram-canvas").Drop(new DragEventArgs { ClientX = 300, ClientY = 250 });

        Assert.Empty(board.Components);
    }

    [Fact]
    public void ASecondDropFromTheSameRegistrationIsIndependentOfTheFirstsBounds()
    {
        RegisterTestComponent(new ComponentSize(120, 80));
        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Instance.BeginPaletteDrag(ComponentTypeKey);
        canvas.Find(".diagram-canvas").Drop(new DragEventArgs { ClientX = 300, ClientY = 250 });
        canvas.Instance.BeginPaletteDrag(ComponentTypeKey);
        canvas.Find(".diagram-canvas").Drop(new DragEventArgs { ClientX = 500, ClientY = 250 });

        Assert.Equal(2, board.Components.Count);
        Assert.Equal(2, board.Components.Select(i => i.Bounds).Distinct().Count());
    }

    [Fact]
    public void DroppingAPendingPaletteDragPlacesTheNewInstanceAboveEveryExistingInstance()
    {
        RegisterTestComponent(new ComponentSize(120, 80));
        var board = new Board();
        board.AddComponent(
            new ComponentInstance(ComponentTypeKey, new TestProps(), new Bounds(0, 0, 50, 50), 9)
        );

        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Instance.BeginPaletteDrag(ComponentTypeKey);
        canvas.Find(".diagram-canvas").Drop(new DragEventArgs { ClientX = 300, ClientY = 250 });

        var placed = board.Components.Single(i => i.Bounds.Width == 120);
        Assert.Equal(10, placed.ZIndex);
    }

    [Fact]
    public void DraggingOverTheCanvasShowsTheDragOverAffordanceUntilDragLeaveOrDrop()
    {
        RegisterTestComponent(new ComponentSize(120, 80));
        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        Assert.DoesNotContain("drag-over", canvas.Find(".diagram-canvas").GetAttribute("class"));
        Assert.Empty(canvas.FindAll(".drag-over-affordance"));

        canvas.Find(".diagram-canvas").DragEnter(new DragEventArgs());
        Assert.Contains("drag-over", canvas.Find(".diagram-canvas").GetAttribute("class"));
        Assert.Single(canvas.FindAll(".drag-over-affordance"));

        canvas.Find(".diagram-canvas").DragLeave(new DragEventArgs());
        Assert.DoesNotContain("drag-over", canvas.Find(".diagram-canvas").GetAttribute("class"));
        Assert.Empty(canvas.FindAll(".drag-over-affordance"));

        canvas.Instance.BeginPaletteDrag(ComponentTypeKey);
        canvas.Find(".diagram-canvas").DragEnter(new DragEventArgs());
        canvas.Find(".diagram-canvas").Drop(new DragEventArgs { ClientX = 300, ClientY = 250 });
        Assert.DoesNotContain("drag-over", canvas.Find(".diagram-canvas").GetAttribute("class"));
        Assert.Empty(canvas.FindAll(".drag-over-affordance"));
    }
}
