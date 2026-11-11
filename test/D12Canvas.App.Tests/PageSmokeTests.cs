using Bunit;
using D12Canvas.App.Pages;
using D12Canvas.Model;

namespace D12Canvas.App.Tests;

public sealed class PageSmokeTests : AppTestContext
{
    [Fact]
    public void TheBoardListShowsAStoredBoardsName()
    {
        BoardStore.Add("Quarterly roadmap", new Board());

        var layout = RenderBoardList();

        layout.WaitForAssertion(() => Assert.Contains("Quarterly roadmap", layout.Markup));
    }

    [Fact]
    public void TheBoardEditorShowsTheStoredBoardsName()
    {
        var boardId = BoardStore.Add("Service map", new Board());

        var layout = RenderBoardEditor(boardId);

        layout.WaitForAssertion(
            () =>
                Assert.Equal(
                    "Service map",
                    layout.FindComponent<BoardEditor>().Find("h1").TextContent
                )
        );
    }
}
