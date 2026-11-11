using AngleSharp.Dom;
using Bunit;
using D12Canvas.App.Pages;
using D12Canvas.Model;
using Microsoft.AspNetCore.Components.Web;

namespace D12Canvas.App.Tests;

public sealed class RenameTests : AppTestContext
{
    private readonly BunitJSModuleInterop _dialogModule;

    public RenameTests()
    {
        _dialogModule = JSInterop.SetupModule("./Components/ModalDialog.razor.js");
        _dialogModule.SetupVoid("showModal", _ => true).SetVoidResult();
    }

    [Fact]
    public void RenameOnTheBoardListOpensTheDialogAsAModalWithTheCurrentName()
    {
        BoardStore.Add("Draft", new Board());
        var layout = RenderBoardList();

        ClickRowRename(layout);

        _dialogModule.VerifyInvoke("showModal");
        Assert.Equal("Draft", layout.Find("dialog input").GetAttribute("value"));
    }

    [Fact]
    public void SubmittingTheRenameDialogSavesTheNewNameAndClosesIt()
    {
        var boardId = BoardStore.Add("Draft", new Board());
        var layout = RenderBoardList();
        ClickRowRename(layout);

        layout.Find("dialog input").Input("Roadmap");
        layout.Find("dialog form").Submit();

        Assert.Equal("Roadmap", BoardStore.Get(boardId).Name);
        layout.WaitForAssertion(() => Assert.Empty(layout.FindAll("dialog input")));
        Assert.Contains("Roadmap", layout.Markup);
    }

    [Fact]
    public void ClosingTheRenameDialogWithoutSubmittingKeepsTheOldName()
    {
        var boardId = BoardStore.Add("Draft", new Board());
        var layout = RenderBoardList();
        ClickRowRename(layout);

        layout.Find("dialog input").Input("Roadmap");
        layout.Find("dialog").TriggerEvent("onclose", EventArgs.Empty);

        Assert.Equal("Draft", BoardStore.Get(boardId).Name);
        Assert.Empty(layout.FindAll("dialog input"));
    }

    [Fact]
    public void RenamingFromTheEditorHeadingSavesTheNewName()
    {
        var boardId = BoardStore.Add("Draft", new Board());
        var layout = RenderBoardEditor(boardId);
        var editor = layout.FindComponent<BoardEditor>();
        editor.WaitForAssertion(() => Assert.Equal("Draft", editor.Find("h1").TextContent));

        editor.Find("h1").Click();
        editor.Find("input[aria-label='Board name']").Input("Roadmap");
        editor
            .Find("input[aria-label='Board name']")
            .KeyDown(new KeyboardEventArgs { Key = "Enter" });

        Assert.Equal("Roadmap", BoardStore.Get(boardId).Name);
        Assert.Equal("Roadmap", editor.Find("h1").TextContent);
    }

    private static void ClickRowRename(IRenderedComponent<Layout.MainLayout> layout)
    {
        IElement? renameButton = null;
        layout.WaitForAssertion(
            () =>
                renameButton = layout
                    .FindAll("tbody button")
                    .Single(button => button.TextContent.Trim() == "Rename")
        );
        renameButton!.Click();
    }
}
