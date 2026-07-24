using Bunit;
using Xunit;

namespace D12Canvas.Tests;

public class DiagramCanvasTests : ComponentTestBase
{
    [Fact(Skip = "Not implemented yet")]
    public void DiagramCanvas_ShouldRender()
    {
        // Arrange
        var canvas = RenderComponent<DiagramCanvas>();

        // Assert
        canvas.MarkupMatches("<div class=\"diagram-canvas\">Canvas</div>");
    }

    [Fact]
    public void DiagramCanvas_ImportsColocatedJsModule()
    {
        var module = JSInterop.SetupModule("./_content/D12Canvas/DiagramCanvas.razor.js");
        module
            .Setup<Dictionary<string, double>>("getContainerDimensions", _ => true)
            .SetResult(new Dictionary<string, double> { ["width"] = 800, ["height"] = 600 });
        module.Setup<Action>("addResizeListener", _ => true).SetResult(() => { });
        module.Setup<Action>("addKeyboardListener", _ => true).SetResult(() => { });

        var canvas = RenderComponent<DiagramCanvas>();

        Assert.NotNull(canvas.Find(".diagram-canvas"));
    }
}
