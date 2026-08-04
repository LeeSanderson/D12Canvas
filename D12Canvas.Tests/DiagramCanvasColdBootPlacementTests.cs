using System.Collections.Generic;
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

    [Fact]
    public async Task ClickToAddIsANoOpBeforeContainerDimensionsAreKnown()
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

        var module = JSInterop.SetupModule("./_content/D12Canvas/DiagramCanvas.razor.js");
        // Deliberately left unresolved (no SetResult) - simulates ClickToAdd firing before the
        // container-size JS round trip has come back, the same as a cold Blazor WASM boot.
        module.Setup<Dictionary<string, double>>("getContainerDimensions", _ => true);

        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        await canvas.InvokeAsync(() => canvas.Instance.ClickToAdd(ComponentTypeKey));

        Assert.Empty(board.Components);
    }
}
