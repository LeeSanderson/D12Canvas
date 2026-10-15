using Bunit;
using Xunit;

namespace D12Canvas.Tests;

public class DiagramCanvasTests : ComponentTestBase
{
    [Fact]
    public void DiagramCanvas_ImportsColocatedJsModule()
    {
        SetupDiagramCanvasJsModule();

        var canvas = Render<DiagramCanvas>();

        Assert.NotNull(canvas.Find(".diagram-canvas"));
    }
}
