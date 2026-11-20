using System.Linq;
using System.Threading.Tasks;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

public class DiagramCanvasKeyboardPlacementTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasKeyboardPlacementTests()
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
                DefaultSize: new ComponentSize(120, 80),
                Category: null
            )
        );
        Services.AddSingleton<IComponentRegistry>(registry);
    }

    [Fact]
    public async Task ClickToAddSelectsTheNewlyPlacedInstance()
    {
        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd(ComponentTypeKey));

        Assert.Equal("true", canvas.Find(".component-container").GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task ClickToAddMovesRealDomFocusToTheNewlyPlacedInstance()
    {
        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd(ComponentTypeKey));

        var invocation = Assert.Single(JSInterop.Invocations["focusTabStopAt"]);
        Assert.Equal(0, invocation.Arguments[1]);
    }

    [Fact]
    public async Task ConsecutiveClickToAddsSelectAndFocusOnlyTheMostRecentlyPlacedInstance()
    {
        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd(ComponentTypeKey));
        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd(ComponentTypeKey));

        var containers = canvas.FindAll(".component-container");
        Assert.Null(containers[0].GetAttribute("aria-selected"));
        Assert.Equal("true", containers[1].GetAttribute("aria-selected"));

        var invocations = JSInterop.Invocations["focusTabStopAt"];
        Assert.Equal(2, invocations.Count);
        Assert.Equal(0, invocations[0].Arguments[1]);
        Assert.Equal(1, invocations[1].Arguments[1]);
    }

    [Fact]
    public async Task ArrowKeyNudgeMovesTheInstanceJustPlacedByClickToAdd()
    {
        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd(ComponentTypeKey));
        var placed = Assert.Single(board.Components);
        var xBeforeNudge = placed.Bounds.X;

        await canvas.InvokeAsync(() => canvas.Instance.OnArrowKeyPressed("ArrowRight", false));

        Assert.Equal(xBeforeNudge + 1, board.GetComponent(placed.Id)!.Bounds.X);
    }

    [Fact]
    public async Task ClickToAddSelectionReplacesWhateverWasSelectedBefore()
    {
        var board = new Board();
        var existing = new ComponentInstance(
            ComponentTypeKey,
            new TestProps(),
            new Bounds(0, 0, 50, 50)
        );
        board.AddComponent(existing);
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );
        canvas.ClickOn(canvas.Find(".component-container"));

        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd(ComponentTypeKey));

        var containers = canvas
            .FindAll(".component-container")
            .Select(c => c.GetAttribute("aria-selected"))
            .ToList();
        Assert.Single(containers, "true");
    }
}
