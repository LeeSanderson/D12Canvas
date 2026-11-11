using AngleSharp.Dom;
using Bunit;
using D12Canvas.App.Layout;
using D12Canvas.App.Storage;
using D12Canvas.BuiltIns;
using D12Canvas.Model;

namespace D12Canvas.App.Tests;

public sealed class BoardListTests : AppTestContext
{
    private const string DeleteDialog = "dialog[aria-labelledby='delete-board-title']";

    private readonly BunitJSModuleInterop _dialogModule;

    public BoardListTests()
    {
        _dialogModule = JSInterop.SetupModule("./Components/ModalDialog.razor.js");
        _dialogModule.SetupVoid("showModal", _ => true).SetVoidResult();
        Clock.SetUtcNow(BoardStore.Now.AddMinutes(5));
    }

    [Fact]
    public void WithNoBoardsOnlyTheEmptyStateCardShows()
    {
        var layout = RenderBoardList();

        layout.WaitForAssertion(() => Assert.Contains("No boards yet", layout.Markup));
        Assert.Single(layout.FindAll("main .list-group"));
        Assert.Empty(layout.FindAll("main .card"));
        Assert.Empty(layout.FindAll("main .row"));
    }

    [Fact]
    public void WhileTheListLoadsFourPlaceholderCardsShow()
    {
        BoardStore.ListGate = new TaskCompletionSource();
        BoardStore.Add("Draft", new Board());

        var layout = RenderBoardList();

        Assert.Equal(4, layout.FindAll("main .placeholder-glow .card").Count);
        Assert.DoesNotContain("Draft", layout.Find("main").TextContent);

        BoardStore.ListGate.SetResult();
        layout.WaitForAssertion(() => Assert.Empty(layout.FindAll("main .placeholder-glow")));
    }

    [Fact]
    public void EachCardLinksToItsBoardAndSaysWhenItWasEdited()
    {
        var boardId = BoardStore.Add("Quarterly roadmap", new Board());

        var layout = RenderBoardList();

        var card = WaitForCard(layout, "Quarterly roadmap");
        var link = card.QuerySelector("a.stretched-link")!;
        Assert.Equal($"board/{boardId}", link.GetAttribute("href"));
        Assert.Equal("Quarterly roadmap", link.GetAttribute("title"));
        var edited = card.QuerySelector("small")!;
        Assert.Equal("Edited 5 min ago", edited.TextContent.Trim());
        Assert.Equal("15 January 2026 at 09:30", edited.GetAttribute("title"));
    }

    [Fact]
    public void TheCardsMenuSitsOutsideTheLinkToTheBoard()
    {
        BoardStore.Add("Draft", new Board());
        var layout = RenderBoardList();

        var card = WaitForCard(layout, "Draft");
        var menuToggle = card.QuerySelector("button[aria-label='Actions for Draft']")!;

        Assert.Null(menuToggle.Closest("a"));
        Assert.NotNull(menuToggle.Closest(".board-card-menu"));
    }

    [Fact]
    public void ACardsSketchStaysBlankUntilItsBoardLoads()
    {
        BoardStore.LoadGate = new TaskCompletionSource();
        BoardStore.Add("Draft", new Board());
        var layout = RenderBoardList();

        var card = WaitForCard(layout, "Draft");
        Assert.Null(card.QuerySelector(".d12-board-sketch"));

        BoardStore.LoadGate.SetResult();
        layout.WaitForAssertion(
            () => Assert.NotNull(WaitForCard(layout, "Draft").QuerySelector(".d12-board-sketch"))
        );
    }

    [Fact]
    public void ABoardThatFailsToLoadLeavesItsSketchBlankAndTheListUsable()
    {
        BoardStore.LoadGate = new TaskCompletionSource();
        BoardStore.LoadGate.SetException(new InvalidOperationException("Corrupt board"));
        BoardStore.Add("Broken", new Board());
        BoardStore.Add("Other", new Board());

        var layout = RenderBoardList();

        layout.WaitForAssertion(() =>
        {
            Assert.Null(WaitForCard(layout, "Broken").QuerySelector(".d12-board-sketch"));
            Assert.Null(WaitForCard(layout, "Other").QuerySelector(".d12-board-sketch"));
        });
    }

    [Fact]
    public void AnEmptyBoardsCardSaysEmptyBoard()
    {
        BoardStore.Add("Draft", new Board());
        var layout = RenderBoardList();

        layout.WaitForAssertion(
            () => Assert.Contains("Empty board", WaitForCard(layout, "Draft").TextContent)
        );
    }

    [Fact]
    public void ABoardWithComponentsGetsASketchOfThem()
    {
        var board = new Board();
        board.AddComponent(
            new ComponentInstance(
                "rectangle",
                new RectangleProps(null, null, 2),
                new Bounds(0, 0, 100, 50)
            )
        );
        BoardStore.Add("Draft", board);
        var layout = RenderBoardList();

        layout.WaitForAssertion(
            () =>
                Assert.Single(
                    WaitForCard(layout, "Draft").QuerySelectorAll(".d12-board-sketch rect")
                )
        );
    }

    [Fact]
    public void TheNewBoardTileCreatesABoardAndOpensIt()
    {
        BoardStore.Add("Draft", new Board());
        var layout = RenderBoardList();
        layout.WaitForAssertion(() => Assert.NotNull(WaitForCard(layout, "Draft")));

        layout.Find("main .board-new-tile").Click();

        AssertOpenedANewBoard();
    }

    [Fact]
    public void TheHeadersNewBoardButtonCreatesABoardAndOpensIt()
    {
        var layout = RenderBoardList();

        layout.Find(".page-header .btn-primary").Click();

        AssertOpenedANewBoard();
    }

    [Fact]
    public void TheEmptyStatesNewBoardButtonCreatesABoardAndOpensIt()
    {
        var layout = RenderBoardList();
        layout.WaitForAssertion(() => Assert.Contains("No boards yet", layout.Markup));

        layout.Find("main .list-group button").Click();

        AssertOpenedANewBoard();
    }

    [Fact]
    public void RenameInTheCardMenuRenamesTheBoard()
    {
        var boardId = BoardStore.Add("Draft", new Board());
        var layout = RenderBoardList();

        ChooseFromCardMenu(layout, "Draft", "Rename…");
        Assert.Equal("Draft", layout.Find("dialog form input").GetAttribute("value"));
        layout.Find("dialog form input").Input("Roadmap");
        layout.Find("dialog form").Submit();

        Assert.Equal("Roadmap", BoardStore.Get(boardId).Name);
        layout.WaitForAssertion(() => Assert.NotNull(WaitForCard(layout, "Roadmap")));
    }

    [Fact]
    public void DeleteInTheCardMenuAsksBeforeDeleting()
    {
        var boardId = BoardStore.Add("Draft", new Board());
        var layout = RenderBoardList();

        ChooseFromCardMenu(layout, "Draft", "Delete…");

        _dialogModule.VerifyInvoke("showModal");
        Assert.Contains(
            "Delete \"Draft\"? This cannot be undone.",
            layout.Find(DeleteDialog).TextContent
        );
        Assert.Equal("Draft", BoardStore.Get(boardId).Name);
    }

    [Fact]
    public void ConfirmingDeleteRemovesTheBoard()
    {
        var boardId = BoardStore.Add("Draft", new Board());
        BoardStore.Add("Keeper", new Board());
        var layout = RenderBoardList();
        ChooseFromCardMenu(layout, "Draft", "Delete…");

        DialogButton(layout, "Delete").Click();

        Assert.Throws<BoardNotFoundException>(() => BoardStore.Get(boardId));
        layout.WaitForAssertion(() =>
        {
            Assert.Empty(layout.FindAll(DeleteDialog));
            Assert.DoesNotContain("Draft", layout.Find("main").TextContent);
            Assert.Contains("Keeper", layout.Find("main").TextContent);
        });
    }

    [Fact]
    public void CancellingDeleteKeepsTheBoard()
    {
        var boardId = BoardStore.Add("Draft", new Board());
        var layout = RenderBoardList();
        ChooseFromCardMenu(layout, "Draft", "Delete…");

        DialogButton(layout, "Cancel").Click();

        Assert.Equal("Draft", BoardStore.Get(boardId).Name);
        Assert.Empty(layout.FindAll(DeleteDialog));
        Assert.NotNull(WaitForCard(layout, "Draft"));
    }

    [Fact]
    public void EscapeOnTheDeleteDialogKeepsTheBoard()
    {
        var boardId = BoardStore.Add("Draft", new Board());
        var layout = RenderBoardList();
        ChooseFromCardMenu(layout, "Draft", "Delete…");

        layout.Find(DeleteDialog).TriggerEvent("onclose", EventArgs.Empty);

        Assert.Equal("Draft", BoardStore.Get(boardId).Name);
        Assert.Empty(layout.FindAll(DeleteDialog));
    }

    private void AssertOpenedANewBoard()
    {
        var created = Assert.Single(
            BoardStore.ListAsync().Result,
            board => board.Name == "Untitled board"
        );
        Assert.EndsWith($"/board/{created.Id}", Navigation.Uri);
    }

    private static IElement WaitForCard(IRenderedComponent<MainLayout> layout, string name)
    {
        IElement? card = null;
        layout.WaitForAssertion(
            () =>
                card = layout
                    .FindAll("main .board-card")
                    .Single(element =>
                        element.QuerySelector("a.stretched-link")?.TextContent.Trim() == name
                    )
        );
        return card!;
    }

    private static IElement DialogButton(IRenderedComponent<MainLayout> layout, string text) =>
        layout
            .FindAll($"{DeleteDialog} button")
            .Single(button => button.TextContent.Trim() == text);
}
