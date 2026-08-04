using System.Threading.Tasks;
using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

public class DiagramCanvasDisposalTests : ComponentTestBase
{
    public DiagramCanvasDisposalTests()
    {
        SetupDiagramCanvasJsModule();
    }

    [Fact]
    public async Task DisposeAsyncDoesNotThrowWhenTheJsSideReturnsRealObjectReferences()
    {
        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));

        var exception = await Record.ExceptionAsync(() => canvas.Instance.DisposeAsync().AsTask());

        Assert.Null(exception);
    }
}
