using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

public class DiagramCanvasColdBootPlacementTests : ComponentTestBase
{
    private const string ComponentTypeKey = "test-props";

    private void RegisterTestComponent()
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
                DefaultSize: new ComponentSize(120, 80),
                Category: null
            )
        );
        Services.AddSingleton<IComponentRegistry>(registry);
    }

    [Fact]
    public async Task ClickToAddBeforeContainerDimensionsAreKnownPlacesNothingYet()
    {
        RegisterTestComponent();
        var module = JSInterop.SetupModule("./_content/D12Canvas/DiagramCanvas.razor.js");
        module.Setup<InitialFacts>("initialFacts", _ => true);

        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd(ComponentTypeKey));

        Assert.Empty(board.Components);
    }

    [Fact]
    public async Task ClicksToAddBeforeTheContainerIsMeasuredLandOnceItIs()
    {
        RegisterTestComponent();
        SetupDiagramCanvasJsModule();
        var initialFacts = CanvasModule.Setup<InitialFacts>("initialFacts", _ => true);

        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters =>
            parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
        );

        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd(ComponentTypeKey));
        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd(ComponentTypeKey));
        Assert.Empty(board.Components);

        initialFacts.SetResult(new InitialFacts(800, 600, false, false));

        canvas.WaitForAssertion(() => Assert.Equal(2, board.Components.Count));
        var placed = board.Components.OrderBy(instance => instance.Bounds.X).ToList();
        var viewport = canvas.Instance.ZoomPanTracker.Viewport;
        Assert.Equal(
            viewport.X + viewport.Width / 2,
            placed[0].Bounds.X + placed[0].Bounds.Width / 2,
            3
        );
        Assert.Equal(20, placed[1].Bounds.X - placed[0].Bounds.X, 3);
        Assert.Equal([placed[1].Id], canvas.Instance.SelectedComponents.Select(c => c.Id));
    }
}
