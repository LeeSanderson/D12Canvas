using System.Threading.Tasks;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// Ticket 29: click select, escape, and aria-selected. Selection is transient view state (ADR
// 0006) - these tests exercise it entirely through DiagramCanvas/ComponentContainer, never
// through Board, since it has no selection concept of its own.
public class DiagramCanvasSelectionTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasSelectionTests()
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

    private static void AddInstance(Board board, double x) =>
        board.AddComponent(
            new ComponentInstance(ComponentTypeKey, new TestProps(), new Bounds(x, 0, 50, 50))
        );

    [Fact]
    public void AnUnselectedInstanceHasNoAriaSelectedAttributeOrSelectedClass()
    {
        var board = new Board();
        AddInstance(board, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var container = canvas.Find(".component-container");
        Assert.Null(container.GetAttribute("aria-selected"));
        Assert.DoesNotContain("selected", container.ClassList);
    }

    [Fact]
    public void ClickingAnInstanceSelectsItWithAVisibleAffordanceAndAriaSelected()
    {
        var board = new Board();
        AddInstance(board, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find(".component-container").Click();

        var container = canvas.Find(".component-container");
        Assert.Equal("true", container.GetAttribute("aria-selected"));
        Assert.Contains("selected", container.ClassList);
    }

    [Fact]
    public void ClickingASecondInstanceMovesSelectionOffTheFirst()
    {
        var board = new Board();
        AddInstance(board, 0);
        AddInstance(board, 100);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var containers = canvas.FindAll(".component-container");
        containers[0].Click();
        containers[1].Click();

        containers = canvas.FindAll(".component-container");
        Assert.Null(containers[0].GetAttribute("aria-selected"));
        Assert.Equal("true", containers[1].GetAttribute("aria-selected"));
    }

    [Fact]
    public void ClickingEmptyCanvasClearsTheSelection()
    {
        var board = new Board();
        AddInstance(board, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find(".component-container").Click();
        Assert.Equal("true", canvas.Find(".component-container").GetAttribute("aria-selected"));

        canvas.Find(".diagram-canvas").Click();

        Assert.Null(canvas.Find(".component-container").GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task EscapeClearsTheSelection()
    {
        var board = new Board();
        AddInstance(board, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find(".component-container").Click();
        Assert.Equal("true", canvas.Find(".component-container").GetAttribute("aria-selected"));

        await canvas.InvokeAsync(() => canvas.Instance.OnEscapePressed());

        Assert.Null(canvas.Find(".component-container").GetAttribute("aria-selected"));
    }

    [Fact]
    public void PanningTheCanvasDoesNotClearAnExistingSelection()
    {
        var board = new Board();
        AddInstance(board, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.Find(".component-container").Click();
        Assert.Equal("true", canvas.Find(".component-container").GetAttribute("aria-selected"));

        // A real pan drag ends with mousedown/mouseup on the same element (the canvas
        // background), so the browser fires a native click right after - simulated here since
        // bUnit doesn't chain that automatically from Move/Up.
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
        canvas.Find(".diagram-canvas").MouseMove(new MouseEventArgs { ClientX = 50, ClientY = 40 });
        canvas.Find(".diagram-canvas").Click();

        Assert.Equal("true", canvas.Find(".component-container").GetAttribute("aria-selected"));
    }
}
