using Bunit;
using D12Canvas.Model;
using D12Canvas.Pointer;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

public class DiagramCanvasMarqueeSelectTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasMarqueeSelectTests()
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
    public async Task DraggingOnEmptyCanvasRendersAVisibleMarqueeThatTracksTheDragAndDisappearsOnRelease()
    {
        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        Assert.Empty(canvas.FindAll(".marquee-select"));

        await canvas.Press(20, 30);
        await canvas.Move(120, 90);

        var marquee = canvas.Find(".marquee-select");
        var style = marquee.GetAttribute("style");
        Assert.Contains("left: 20px", style);
        Assert.Contains("top: 30px", style);
        Assert.Contains("width: 100px", style);
        Assert.Contains("height: 60px", style);

        await canvas.Release(120, 90);

        Assert.Empty(canvas.FindAll(".marquee-select"));
    }

    [Fact]
    public async Task AMiddleDragPansInsteadOfDrawingAMarquee()
    {
        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        await canvas.Press(100, 100, PointerPress.MiddleButton);
        await canvas.Move(50, 40);

        Assert.Empty(canvas.FindAll(".marquee-select"));
        Assert.Contains(
            "translate(-50px, -60px)",
            canvas.Find(".canvas-content").GetAttribute("style")
        );
    }

    [Fact]
    public async Task MarqueeSelectsEveryInstanceItIntersectsIncludingOnesOnlyPartiallyOverlapped()
    {
        var board = new Board();
        AddInstance(board, 60, 60);
        AddInstance(board, 190, 190);
        AddInstance(board, 400, 400);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ReturnToOrigin();

        await canvas.Marquee(from: (0, 0), to: (200, 200));

        var containers = canvas.FindAll(".component-container");
        Assert.Equal("true", containers[0].GetAttribute("aria-selected"));
        Assert.Equal("true", containers[1].GetAttribute("aria-selected"));
        Assert.Null(containers[2].GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task MarqueeWorksWhenDraggedInAnyDirection()
    {
        var board = new Board();
        AddInstance(board, 60, 60);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ReturnToOrigin();

        await canvas.Marquee(from: (200, 200), to: (0, 0));

        Assert.Equal("true", canvas.Find(".component-container").GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task MarqueeReplacesAnExistingSelectionRatherThanAddingToIt()
    {
        var board = new Board();
        AddInstance(board, 0, 0);
        AddInstance(board, 300, 300);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ReturnToOrigin();

        canvas.ClickOn(canvas.FindAll(".component-container")[0]);
        Assert.Equal(
            "true",
            canvas.FindAll(".component-container")[0].GetAttribute("aria-selected")
        );

        await canvas.Marquee(from: (280, 280), to: (400, 400));

        var containers = canvas.FindAll(".component-container");
        Assert.Null(containers[0].GetAttribute("aria-selected"));
        Assert.Equal("true", containers[1].GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task ShiftDragAddsTheBandsContentsToTheExistingSelectionAcrossSweeps()
    {
        var board = new Board();
        AddInstance(board, 0, 0);
        AddInstance(board, 300, 300);
        AddInstance(board, 600, 600);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ReturnToOrigin();

        canvas.ClickOn(canvas.FindAll(".component-container")[0]);

        await canvas.Marquee(from: (280, 280), to: (400, 400), shift: true);
        await canvas.Marquee(from: (580, 580), to: (700, 700), shift: true);

        var containers = canvas.FindAll(".component-container");
        Assert.Equal("true", containers[0].GetAttribute("aria-selected"));
        Assert.Equal("true", containers[1].GetAttribute("aria-selected"));
        Assert.Equal("true", containers[2].GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task AShiftMarqueeThatSweepsBackOffAnInstanceLeavesThePressTimeSelectionIntact()
    {
        var board = new Board();
        AddInstance(board, 0, 0);
        AddInstance(board, 300, 300);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.ClickOn(canvas.FindAll(".component-container")[0]);

        await canvas.Press(280, 280, shift: true);
        await canvas.Move(400, 400, shift: true);
        await canvas.Move(290, 290, shift: true);
        await canvas.Release(290, 290);

        var containers = canvas.FindAll(".component-container");
        Assert.Equal("true", containers[0].GetAttribute("aria-selected"));
        Assert.Null(containers[1].GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task MarqueeAccountsForZoomWhenConvertingScreenCoordinatesToBoardSpace()
    {
        var board = new Board();
        AddInstance(board, 210, 210);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.ZoomIn();

        await canvas.Marquee(from: (0, 0), to: (220, 220));

        Assert.Null(canvas.Find(".component-container").GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task MarqueeAccountsForPanWhenConvertingScreenCoordinatesToBoardSpace()
    {
        var board = new Board();
        AddInstance(board, 60, 60);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.ReturnToOrigin();

        await canvas.Pan(from: (0, 0), to: (200, 200));

        await canvas.Marquee(from: (0, 0), to: (100, 100));
        Assert.Null(canvas.Find(".component-container").GetAttribute("aria-selected"));

        await canvas.Marquee(from: (250, 250), to: (350, 350));
        Assert.Equal("true", canvas.Find(".component-container").GetAttribute("aria-selected"));
    }

    [Fact]
    public void ShiftClickAddsAnUnselectedInstanceToTheSelection()
    {
        var board = new Board();
        AddInstance(board, 0);
        AddInstance(board, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var containers = canvas.FindAll(".component-container");
        canvas.ClickOn(containers[0]);
        canvas.ClickOn(containers[1], shift: true);

        containers = canvas.FindAll(".component-container");
        Assert.Equal("true", containers[0].GetAttribute("aria-selected"));
        Assert.Equal("true", containers[1].GetAttribute("aria-selected"));
    }

    [Fact]
    public void ShiftClickRemovesAnAlreadySelectedInstanceFromTheSelection()
    {
        var board = new Board();
        AddInstance(board, 0);
        AddInstance(board, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var containers = canvas.FindAll(".component-container");
        canvas.ClickOn(containers[0]);
        canvas.ClickOn(containers[1], shift: true);
        containers = canvas.FindAll(".component-container");
        canvas.ClickOn(containers[1], shift: true);

        containers = canvas.FindAll(".component-container");
        Assert.Equal("true", containers[0].GetAttribute("aria-selected"));
        Assert.Null(containers[1].GetAttribute("aria-selected"));
    }

    [Fact]
    public void APlainClickAfterAMultiSelectCollapsesTheSelectionToJustTheClickedInstance()
    {
        var board = new Board();
        AddInstance(board, 0);
        AddInstance(board, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var containers = canvas.FindAll(".component-container");
        canvas.ClickOn(containers[0]);
        canvas.ClickOn(containers[1], shift: true);

        containers = canvas.FindAll(".component-container");
        canvas.ClickOn(containers[1]);

        containers = canvas.FindAll(".component-container");
        Assert.Null(containers[0].GetAttribute("aria-selected"));
        Assert.Equal("true", containers[1].GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task AStationaryPressOnTheSelectionBoxLeavesTheSelectionAloneWhereOneOnEmptyCanvasClearsIt()
    {
        var board = new Board();
        AddInstance(board, 0, 0);
        AddInstance(board, 300, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var containers = canvas.FindAll(".component-container");
        canvas.ClickOn(containers[0]);
        canvas.ClickOn(containers[1], shift: true);

        canvas.PressOn(
            canvas.Find(".selection-bounding-box"),
            (150, 25),
            role: HitRole.SelectionBounds
        );
        canvas.ReleaseAt((150, 25));

        containers = canvas.FindAll(".component-container");
        Assert.Equal("true", containers[0].GetAttribute("aria-selected"));
        Assert.Equal("true", containers[1].GetAttribute("aria-selected"));

        await canvas.ClickCanvas(150, 200);

        containers = canvas.FindAll(".component-container");
        Assert.Null(containers[0].GetAttribute("aria-selected"));
        Assert.Null(containers[1].GetAttribute("aria-selected"));
    }

    private static void AddInstance(Board board, double x) => AddInstance(board, x, 0);
}
