using Bunit;
using D12Canvas.Model;

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

        ChooseFromCardMenu(layout, "Draft", "Rename…");

        _dialogModule.VerifyInvoke("showModal");
        Assert.Equal("Draft", layout.Find("dialog form input").GetAttribute("value"));
    }

    [Fact]
    public void SubmittingTheRenameDialogSavesTheNewNameAndClosesIt()
    {
        var boardId = BoardStore.Add("Draft", new Board());
        var layout = RenderBoardList();
        ChooseFromCardMenu(layout, "Draft", "Rename…");

        layout.Find("dialog form input").Input("Roadmap");
        layout.Find("dialog form").Submit();

        Assert.Equal("Roadmap", BoardStore.Get(boardId).Name);
        layout.WaitForAssertion(() => Assert.Empty(layout.FindAll("dialog form input")));
        Assert.Contains("Roadmap", layout.Markup);
    }

    [Fact]
    public void ClosingTheRenameDialogWithoutSubmittingKeepsTheOldName()
    {
        var boardId = BoardStore.Add("Draft", new Board());
        var layout = RenderBoardList();
        ChooseFromCardMenu(layout, "Draft", "Rename…");

        layout.Find("dialog form input").Input("Roadmap");
        layout.Find("dialog").TriggerEvent("onclose", EventArgs.Empty);

        Assert.Equal("Draft", BoardStore.Get(boardId).Name);
        Assert.Empty(layout.FindAll("dialog form input"));
    }

    [Fact]
    public void RenamingFromTheEditorHeadingSavesTheNewName()
    {
        var boardId = BoardStore.Add("Draft", new Board());
        var layout = RenderBoardEditor(boardId);
        layout.WaitForAssertion(() => Assert.Equal("Draft", layout.Find("h1").TextContent));

        layout.Find("h1").Click();
        layout.Find("input[aria-label='Board name']").Input("Roadmap");
        layout.Find(".page-header form").Submit();

        Assert.Equal("Roadmap", BoardStore.Get(boardId).Name);
        Assert.Equal("Roadmap", layout.Find("h1").TextContent);
    }
}
