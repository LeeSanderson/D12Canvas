using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// Ticket 32: marquee + shift-click multi-select (ADR 0006). A plain drag on empty canvas still
// pans (pre-existing behaviour); Shift+drag draws an intersection-based marquee instead, pairing
// with Shift-click's existing "multi-select gesture" meaning. A drag starting inside the current
// selection's own combined bounding box does neither - reserved for ticket 33's group-move-as-a-
// unit, so it's deliberately inert for now.
public class DiagramCanvasMarqueeSelectTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasMarqueeSelectTests()
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
    public void ShiftDraggingOnEmptyCanvasRendersAVisibleMarqueeThatTracksTheDragAndDisappearsOnRelease()
    {
        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        Assert.Empty(canvas.FindAll(".marquee-select"));

        canvas
            .Find(".diagram-canvas")
            .MouseDown(
                new MouseEventArgs
                {
                    ClientX = 20,
                    ClientY = 30,
                    ShiftKey = true,
                }
            );
        canvas
            .Find(".diagram-canvas")
            .MouseMove(new MouseEventArgs { ClientX = 120, ClientY = 90 });

        var marquee = canvas.Find(".marquee-select");
        var style = marquee.GetAttribute("style");
        Assert.Contains("left: 20px", style);
        Assert.Contains("top: 30px", style);
        Assert.Contains("width: 100px", style);
        Assert.Contains("height: 60px", style);

        canvas.Find(".diagram-canvas").MouseUp(new MouseEventArgs { ClientX = 120, ClientY = 90 });

        Assert.Empty(canvas.FindAll(".marquee-select"));
    }

    [Fact]
    public void APlainDragWithoutShiftPansInsteadOfDrawingAMarquee()
    {
        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas
            .Find(".diagram-canvas")
            .MouseDown(new MouseEventArgs { ClientX = 100, ClientY = 100 });
        canvas.Find(".diagram-canvas").MouseMove(new MouseEventArgs { ClientX = 50, ClientY = 40 });

        Assert.Empty(canvas.FindAll(".marquee-select"));
        Assert.Contains(
            "translate(-50px, -60px)",
            canvas.Find(".canvas-content").GetAttribute("style")
        );
    }

    [Fact]
    public void MarqueeSelectsEveryInstanceItIntersectsIncludingOnesOnlyPartiallyOverlapped()
    {
        var board = new Board();
        AddInstance(board, 60, 60); // fully inside the drag rectangle (0,0)-(200,200)
        AddInstance(board, 190, 190); // only its top-left corner overlaps the rectangle
        AddInstance(board, 400, 400); // untouched
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas
            .Find(".diagram-canvas")
            .MouseDown(
                new MouseEventArgs
                {
                    ClientX = 0,
                    ClientY = 0,
                    ShiftKey = true,
                }
            );
        canvas
            .Find(".diagram-canvas")
            .MouseMove(new MouseEventArgs { ClientX = 200, ClientY = 200 });
        canvas.Find(".diagram-canvas").MouseUp(new MouseEventArgs { ClientX = 200, ClientY = 200 });

        var containers = canvas.FindAll(".component-container");
        Assert.Equal("true", containers[0].GetAttribute("aria-selected"));
        Assert.Equal("true", containers[1].GetAttribute("aria-selected"));
        Assert.Null(containers[2].GetAttribute("aria-selected"));
    }

    [Fact]
    public void MarqueeWorksWhenDraggedInAnyDirection()
    {
        var board = new Board();
        AddInstance(board, 60, 60);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        // Dragged from bottom-right up to top-left (negative deltas), not the usual direction.
        canvas
            .Find(".diagram-canvas")
            .MouseDown(
                new MouseEventArgs
                {
                    ClientX = 200,
                    ClientY = 200,
                    ShiftKey = true,
                }
            );
        canvas.Find(".diagram-canvas").MouseMove(new MouseEventArgs { ClientX = 0, ClientY = 0 });
        canvas.Find(".diagram-canvas").MouseUp(new MouseEventArgs { ClientX = 0, ClientY = 0 });

        Assert.Equal("true", canvas.Find(".component-container").GetAttribute("aria-selected"));
    }

    [Fact]
    public void MarqueeReplacesAnExistingSelectionRatherThanAddingToIt()
    {
        var board = new Board();
        AddInstance(board, 0, 0);
        AddInstance(board, 300, 300);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.FindAll(".component-container")[0].Click();
        Assert.Equal(
            "true",
            canvas.FindAll(".component-container")[0].GetAttribute("aria-selected")
        );

        canvas
            .Find(".diagram-canvas")
            .MouseDown(
                new MouseEventArgs
                {
                    ClientX = 280,
                    ClientY = 280,
                    ShiftKey = true,
                }
            );
        canvas
            .Find(".diagram-canvas")
            .MouseMove(new MouseEventArgs { ClientX = 400, ClientY = 400 });
        canvas.Find(".diagram-canvas").MouseUp(new MouseEventArgs { ClientX = 400, ClientY = 400 });

        var containers = canvas.FindAll(".component-container");
        Assert.Null(containers[0].GetAttribute("aria-selected"));
        Assert.Equal("true", containers[1].GetAttribute("aria-selected"));
    }

    [Fact]
    public void MarqueeAccountsForZoomWhenConvertingScreenCoordinatesToBoardSpace()
    {
        var board = new Board();
        AddInstance(board, 210, 210); // spans board (210,210)-(260,260)
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find(".diagram-canvas").Wheel(new WheelEventArgs { DeltaY = -100 }); // zooms to scale 1.1

        // At scale 1.1, this 220px screen-space drag reaches only ~200 board units - short of the
        // instance's (210,210) origin. If the conversion ignored zoom (treating screen pixels as
        // board units 1:1), the same drag would reach 220 and wrongly intersect it.
        canvas
            .Find(".diagram-canvas")
            .MouseDown(
                new MouseEventArgs
                {
                    ClientX = 0,
                    ClientY = 0,
                    ShiftKey = true,
                }
            );
        canvas
            .Find(".diagram-canvas")
            .MouseMove(new MouseEventArgs { ClientX = 220, ClientY = 220 });
        canvas.Find(".diagram-canvas").MouseUp(new MouseEventArgs { ClientX = 220, ClientY = 220 });

        Assert.Null(canvas.Find(".component-container").GetAttribute("aria-selected"));
    }

    [Fact]
    public void ShiftClickAddsAnUnselectedInstanceToTheSelection()
    {
        var board = new Board();
        AddInstance(board, 0);
        AddInstance(board, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var containers = canvas.FindAll(".component-container");
        containers[0].Click();
        containers[1].Click(new MouseEventArgs { ShiftKey = true });

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
        containers[0].Click();
        containers[1].Click(new MouseEventArgs { ShiftKey = true });
        containers = canvas.FindAll(".component-container");
        containers[1].Click(new MouseEventArgs { ShiftKey = true });

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
        containers[0].Click();
        containers[1].Click(new MouseEventArgs { ShiftKey = true });

        containers = canvas.FindAll(".component-container");
        containers[1].Click();

        containers = canvas.FindAll(".component-container");
        Assert.Null(containers[0].GetAttribute("aria-selected"));
        Assert.Equal("true", containers[1].GetAttribute("aria-selected"));
    }

    // A drag starting inside the selection's own combined bounding box - but on empty space, not
    // on either instance itself - used to be deliberately inert (reserved for ticket 33), and this
    // test asserted exactly that: no pan, no marquee, selection undisturbed. Ticket 33 gave that
    // drag a real job (moving the whole selection as one unit) - see
    // DiagramCanvasMultiSelectionMoveResizeTests.DraggingEmptySpaceWithinTheBoundingBoxMoves...
    // and ...TheBoardIsUnchangedMidGroupMoveAndOnlyUpdatesOnRelease, which cover the same gesture
    // (still no pan, still no marquee, still doesn't clear the selection) plus its new effect.

    // A stationary click (no movement) on empty space still clears the selection per ADR 0006,
    // even when that point happens to fall within the selection's combined bounding box - only a
    // real drag from there does something else now (ticket 33's group move).
    [Fact]
    public void AStationaryClickWithinTheSelectionBoundsStillClearsTheSelection()
    {
        var board = new Board();
        AddInstance(board, 0, 0);
        AddInstance(board, 300, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var containers = canvas.FindAll(".component-container");
        containers[0].Click();
        containers[1].Click(new MouseEventArgs { ShiftKey = true });

        canvas
            .Find(".diagram-canvas")
            .MouseDown(new MouseEventArgs { ClientX = 150, ClientY = 25 });
        canvas.Find(".diagram-canvas").MouseUp(new MouseEventArgs { ClientX = 150, ClientY = 25 });
        canvas.Find(".diagram-canvas").Click();

        containers = canvas.FindAll(".component-container");
        Assert.Null(containers[0].GetAttribute("aria-selected"));
        Assert.Null(containers[1].GetAttribute("aria-selected"));
    }

    private static void AddInstance(Board board, double x) => AddInstance(board, x, 0);
}
