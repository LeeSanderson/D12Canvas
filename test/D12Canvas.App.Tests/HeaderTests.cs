using Bunit;
using D12Canvas.App.Layout;
using D12Canvas.Model;
using Microsoft.AspNetCore.Components.Web;

namespace D12Canvas.App.Tests;

public sealed class HeaderTests : AppTestContext
{
    [Fact]
    public void TheBrandLinksToTheBoardList()
    {
        var layout = RenderBoardList();

        var brand = layout.Find("a.navbar-brand");
        Assert.Equal("/", brand.GetAttribute("href"));
        Assert.Equal("D12Canvas", brand.TextContent.Trim());
    }

    [Fact]
    public void TheBoardsLinkIsActiveOnTheBoardList()
    {
        var layout = RenderBoardList();

        var boards = BoardsLink(layout);
        Assert.Contains("active", boards.ClassList);
        Assert.Equal("page", boards.GetAttribute("aria-current"));
    }

    [Fact]
    public void TheBoardsLinkIsNotActiveInTheEditor()
    {
        var boardId = BoardStore.Add("Service map", new Board());

        var layout = RenderBoardEditor(boardId);

        var boards = BoardsLink(layout);
        Assert.DoesNotContain("active", boards.ClassList);
        Assert.False(boards.HasAttribute("aria-current"));
    }

    [Fact]
    public void TheBoardListHeaderShowsItsHeadingAndNewBoard()
    {
        var layout = RenderBoardList();

        var header = layout.Find(".page-header");
        Assert.Equal("Boards", header.QuerySelector("h1")!.TextContent);
        Assert.Contains(
            header.QuerySelectorAll("button"),
            button => button.TextContent.Trim() == "New board"
        );
    }

    [Fact]
    public async Task NewBoardInTheHeaderCreatesABoardAndOpensIt()
    {
        var layout = RenderBoardList();

        layout
            .FindAll(".page-header button")
            .Single(button => button.TextContent.Trim() == "New board")
            .Click();

        var created = Assert.Single(await BoardStore.ListAsync());
        Assert.EndsWith($"/board/{created.Id}", Navigation.Uri);
    }

    [Fact]
    public void TheEditorHeaderShowsTheBoardNameAndRename()
    {
        var boardId = BoardStore.Add("Service map", new Board());

        var layout = RenderBoardEditor(boardId);

        layout.WaitForAssertion(
            () => Assert.Equal("Service map", layout.Find(".page-header h1").TextContent)
        );
        Assert.NotNull(layout.Find(".page-header button[aria-label='Rename board']"));
        Assert.DoesNotContain(
            layout.FindAll(".page-header button"),
            button => button.TextContent.Trim() == "New board"
        );
    }

    [Fact]
    public void TheRenameButtonStartsARename()
    {
        var boardId = BoardStore.Add("Service map", new Board());
        var layout = RenderBoardEditor(boardId);
        layout.WaitForAssertion(
            () =>
                Assert.False(
                    layout.Find("button[aria-label='Rename board']").HasAttribute("disabled")
                )
        );

        layout.Find("button[aria-label='Rename board']").Click();

        Assert.Equal(
            "Service map",
            layout.Find("input[aria-label='Board name']").GetAttribute("value")
        );
    }

    [Fact]
    public void TheRenameButtonKeepsTheDraftWhileRenaming()
    {
        var boardId = BoardStore.Add("Service map", new Board());
        var layout = RenderBoardEditor(boardId);
        layout.WaitForAssertion(() => Assert.Equal("Service map", layout.Find("h1").TextContent));
        layout.Find("h1").Click();
        layout.Find("input[aria-label='Board name']").Input("Roadmap");

        layout.Find("button[aria-label='Rename board']").Click();

        Assert.Equal(
            "Roadmap",
            layout.Find("input[aria-label='Board name']").GetAttribute("value")
        );
    }

    [Fact]
    public void EscapeCancelsARename()
    {
        var boardId = BoardStore.Add("Service map", new Board());
        var layout = RenderBoardEditor(boardId);
        layout.WaitForAssertion(() => Assert.Equal("Service map", layout.Find("h1").TextContent));
        layout.Find("h1").Click();

        layout.Find("input[aria-label='Board name']").Input("Roadmap");
        layout
            .Find("input[aria-label='Board name']")
            .KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Equal("Service map", layout.Find("h1").TextContent);
        Assert.Equal("Service map", BoardStore.Get(boardId).Name);
    }

    [Fact]
    public void TheMenuButtonOpensTheOffcanvasWithABackdrop()
    {
        var layout = RenderBoardList();

        layout.Find("button[aria-controls='app-menu']").Click();

        Assert.Contains("show", layout.Find("#app-menu").ClassList);
        Assert.Equal(
            "true",
            layout.Find("button[aria-controls='app-menu']").GetAttribute("aria-expanded")
        );
        Assert.NotNull(layout.Find(".offcanvas-backdrop"));
    }

    [Fact]
    public void TheCloseButtonClosesTheOffcanvas()
    {
        var layout = RenderWithMenuOpen();

        layout.Find("#app-menu .btn-close").Click();

        AssertMenuClosed(layout);
    }

    [Fact]
    public void TheBackdropClosesTheOffcanvas()
    {
        var layout = RenderWithMenuOpen();

        layout.Find(".offcanvas-backdrop").Click();

        AssertMenuClosed(layout);
    }

    [Fact]
    public void EscapeClosesTheOffcanvas()
    {
        var layout = RenderWithMenuOpen();

        layout.Find("#app-menu").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        AssertMenuClosed(layout);
    }

    [Fact]
    public void TheBoardsLinkClosesTheOffcanvas()
    {
        var layout = RenderWithMenuOpen();

        BoardsLink(layout).Click();

        AssertMenuClosed(layout);
    }

    [Fact]
    public void SuggestClosesTheOffcanvasAndOpensTheSuggestionBox()
    {
        var dialogModule = SetUpSuggestionBox();
        var layout = RenderWithMenuOpen();

        SuggestButton(layout).Click();

        AssertMenuClosed(layout);
        dialogModule.VerifyInvoke("showModal");
    }

    [Fact]
    public void SuggestOpensTheSuggestionBoxFromTheEditor()
    {
        var dialogModule = SetUpSuggestionBox();
        var boardId = BoardStore.Add("Service map", new Board());
        var layout = RenderBoardEditor(boardId);

        SuggestButton(layout).Click();

        dialogModule.VerifyInvoke("showModal");
    }

    [Fact]
    public void TheBoardListTabIsTitledBoards()
    {
        var layout = RenderBoardList();

        Assert.Equal("Boards · D12Canvas", TabTitle(layout));
    }

    [Fact]
    public void TheEditorTabIsTitledWithTheBoardName()
    {
        var boardId = BoardStore.Add("Service map", new Board());

        var layout = RenderBoardEditor(boardId);

        layout.WaitForAssertion(() => Assert.Equal("Service map · D12Canvas", TabTitle(layout)));
    }

    private string TabTitle(IRenderedComponent<MainLayout> layout) =>
        Render(layout.FindComponent<PageTitle>().Instance.ChildContent!).Markup;

    private IRenderedComponent<MainLayout> RenderWithMenuOpen()
    {
        var layout = RenderBoardList();
        layout.Find("button[aria-controls='app-menu']").Click();
        return layout;
    }

    private static void AssertMenuClosed(IRenderedComponent<MainLayout> layout)
    {
        Assert.DoesNotContain("show", layout.Find("#app-menu").ClassList);
        Assert.Empty(layout.FindAll(".offcanvas-backdrop"));
    }

    private static AngleSharp.Dom.IElement BoardsLink(IRenderedComponent<MainLayout> layout) =>
        layout.FindAll("#app-menu a.nav-link").Single(link => link.TextContent.Trim() == "Boards");

    private static AngleSharp.Dom.IElement SuggestButton(IRenderedComponent<MainLayout> layout) =>
        layout.FindAll("#app-menu button").Single(button => button.TextContent.Trim() == "Suggest");

    private BunitJSModuleInterop SetUpSuggestionBox()
    {
        var dialogModule = JSInterop.SetupModule("./Components/ModalDialog.razor.js");
        dialogModule.SetupVoid("showModal", _ => true).SetVoidResult();
        var suggestionModule = JSInterop.SetupModule("./Components/SuggestionBox.razor.js");
        suggestionModule.SetupModule("attach", _ => true);
        return dialogModule;
    }
}
