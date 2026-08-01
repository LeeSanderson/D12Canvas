using System.Linq;
using System.Threading.Tasks;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// Keyboard placement: a palette button's Enter/Space activation reaches the exact same
// ClickToAdd a mouse click does (native button semantics synthesize the same click event either
// way), so there's no separate keyboard entry point to exercise here. What these tests cover is
// what ClickToAdd now does beyond placement itself - selecting the newly placed instance and
// moving real DOM focus to it - which a mouse click-to-add never needed, since a mouse click
// leaves the pointer sitting right on top of whatever it just placed.
public class DiagramCanvasKeyboardPlacementTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    public DiagramCanvasKeyboardPlacementTests()
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
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd(ComponentTypeKey));

        Assert.Equal("true", canvas.Find(".component-container").GetAttribute("aria-selected"));
    }

    [Fact]
    public async Task ClickToAddMovesRealDomFocusToTheNewlyPlacedInstance()
    {
        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd(ComponentTypeKey));

        var invocation = Assert.Single(JSInterop.Invocations["focusTabStopAt"]);
        Assert.Equal(0, invocation.Arguments[1]);
    }

    [Fact]
    public async Task ConsecutiveClickToAddsSelectAndFocusOnlyTheMostRecentlyPlacedInstance()
    {
        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd(ComponentTypeKey));
        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd(ComponentTypeKey));

        // The cascade offset moves each successive instance down and to the right, so reading
        // order (top-to-bottom) leaves the first-placed instance at index 0 and the
        // second-placed (now selected) one at index 1.
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
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

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
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        canvas.Find(".component-container").Click();

        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd(ComponentTypeKey));

        var containers = canvas
            .FindAll(".component-container")
            .Select(c => c.GetAttribute("aria-selected"))
            .ToList();
        Assert.Single(containers, "true");
    }
}
