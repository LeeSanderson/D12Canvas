using System.Threading.Tasks;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// Click select, escape, and aria-selected. Selection is transient view state - these tests
// exercise it entirely through DiagramCanvas/ComponentContainer, never through Board, since it
// has no selection concept of its own.
public class DiagramCanvasSelectionTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasSelectionTests()
    {
        SetupDiagramCanvasJsModule();
        SetupComponentContainerJsModule();

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

        canvas.ClickOn(canvas.Find(".component-container"));

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
        canvas.ClickOn(containers[0]);
        canvas.ClickOn(containers[1]);

        containers = canvas.FindAll(".component-container");
        Assert.Null(containers[0].GetAttribute("aria-selected"));
        Assert.Equal("true", containers[1].GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task ClickingEmptyCanvasClearsTheSelection()
    {
        var board = new Board();
        AddInstance(board, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.ClickOn(canvas.Find(".component-container"));
        Assert.Equal("true", canvas.Find(".component-container").GetAttribute("aria-selected"));

        await canvas.ClickCanvas(200, 200);

        Assert.Null(canvas.Find(".component-container").GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task EscapeClearsTheSelection()
    {
        var board = new Board();
        AddInstance(board, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.ClickOn(canvas.Find(".component-container"));
        Assert.Equal("true", canvas.Find(".component-container").GetAttribute("aria-selected"));

        await canvas.InvokeAsync(() => canvas.Instance.OnEscapePressed());

        Assert.Null(canvas.Find(".component-container").GetAttribute("aria-selected"));
    }

    // A pan is a drag with the middle or secondary button; only a release below the drag
    // threshold on the primary button is the click that clears the selection.
    [Fact]
    public async Task PanningTheCanvasDoesNotClearAnExistingSelection()
    {
        var board = new Board();
        AddInstance(board, 0);
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        canvas.ClickOn(canvas.Find(".component-container"));
        Assert.Equal("true", canvas.Find(".component-container").GetAttribute("aria-selected"));

        await canvas.Pan(from: (100, 100), to: (50, 40));

        Assert.Equal("true", canvas.Find(".component-container").GetAttribute("aria-selected"));
    }
}
