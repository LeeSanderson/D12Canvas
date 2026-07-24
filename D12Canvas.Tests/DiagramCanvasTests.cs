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
        SetupDiagramCanvasJsModule();

        var canvas = RenderComponent<DiagramCanvas>();

        Assert.NotNull(canvas.Find(".diagram-canvas"));
    }
}
