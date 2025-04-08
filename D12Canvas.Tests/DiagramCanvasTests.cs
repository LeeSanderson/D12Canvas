using Bunit;
using Xunit;

namespace D12Canvas.Tests;

public class DiagramCanvasTests : ComponentTestBase
{
    [Fact(Skip = "Not implemented yet")]
    public void DiagramCanvas_ShouldRender()
    {
        // Arrange
        var cut = RenderComponent<DiagramCanvas>();

        // Assert
        cut.MarkupMatches("<div class=\"diagram-canvas\">Canvas</div>");
    }
}
