using AngleSharp.Dom;
using Bunit;
using D12Canvas.App.Layout;
using D12Canvas.Model;

namespace D12Canvas.App.Tests;

public sealed class AutosaveTests : AppTestContext
{
    private const string AutosaveKey = "d12canvas.autosave";

    [Fact]
    public void ABoardOpenedWithNoStoredChoiceHasAutosaveOnAndNoSaveButton()
    {
        var layout = RenderEditor();

        Assert.True(Switch(layout).HasAttribute("checked"));
        Assert.Null(SaveButton(layout));
        Assert.Equal("", StatusText(layout));
    }

    [Fact]
    public void AnEditSavesTheBoardWithoutPressingSave()
    {
        var layout = RenderEditor();

        Edit(layout);
        Clock.Advance(TimeSpan.FromSeconds(1));

        layout.WaitForAssertion(() => Assert.Single(BoardStore.Saves));
    }

    [Fact]
    public void AnEditShowsSavingThenSavedThenClearsAfterTwoSeconds()
    {
        var layout = RenderEditor();

        Edit(layout);
        Assert.Equal("Saving…", StatusText(layout));
        Assert.NotNull(layout.Find(".page-header [role=status] svg"));

        Clock.Advance(TimeSpan.FromSeconds(1));
        layout.WaitForAssertion(() => Assert.Equal("Saved", StatusText(layout)));

        Clock.Advance(TimeSpan.FromSeconds(1.9));
        Assert.Equal("Saved", StatusText(layout));

        Clock.Advance(TimeSpan.FromSeconds(0.1));
        layout.WaitForAssertion(() => Assert.Equal("", StatusText(layout)));
    }

    [Fact]
    public void TurningAutosaveOffStoresFalseAndShowsADisabledSaveButton()
    {
        var layout = RenderEditor();

        Switch(layout).Change(false);

        Assert.Equal("false", Preferences.Values[AutosaveKey]);
        Assert.False(Switch(layout).HasAttribute("checked"));
        Assert.True(SaveButton(layout)!.HasAttribute("disabled"));
        Assert.Equal("Saved", StatusText(layout));
    }

    [Fact]
    public void AStoredFalseOpensTheBoardWithAutosaveOff()
    {
        Preferences.Values[AutosaveKey] = "false";

        var layout = RenderEditor();

        Assert.False(Switch(layout).HasAttribute("checked"));
        Assert.NotNull(SaveButton(layout));
    }

    [Fact]
    public void WithAutosaveOffAnEditEnablesSaveAndSavingReturnsTheStatusToSaved()
    {
        Preferences.Values[AutosaveKey] = "false";
        var layout = RenderEditor();

        Edit(layout);
        Clock.Advance(TimeSpan.FromSeconds(5));

        Assert.Empty(BoardStore.Saves);
        Assert.Equal("Unsaved changes", StatusText(layout));
        Assert.False(SaveButton(layout)!.HasAttribute("disabled"));

        SaveButton(layout)!.Click();

        Assert.Single(BoardStore.Saves);
        Assert.Equal("Saved", StatusText(layout));
        Assert.True(SaveButton(layout)!.HasAttribute("disabled"));
    }

    [Fact]
    public void AnEditMadeWhileASaveIsRunningStaysUnsaved()
    {
        Preferences.Values[AutosaveKey] = "false";
        var layout = RenderEditor();
        Edit(layout);
        BoardStore.SaveGate = new TaskCompletionSource();
        SaveButton(layout)!.Click();

        Edit(layout);
        BoardStore.SaveGate.SetResult();

        layout.WaitForAssertion(() => Assert.Equal("Unsaved changes", StatusText(layout)));
        Assert.False(SaveButton(layout)!.HasAttribute("disabled"));
    }

    [Fact]
    public void TurningAutosaveOnWithUnsavedChangesSavesAtOnce()
    {
        Preferences.Values[AutosaveKey] = "false";
        var layout = RenderEditor();
        Edit(layout);

        Switch(layout).Change(true);

        layout.WaitForAssertion(() => Assert.Single(BoardStore.Saves));
        Assert.Equal("true", Preferences.Values[AutosaveKey]);
        Assert.Null(SaveButton(layout));
        Assert.Equal("Saved", StatusText(layout));
    }

    [Fact]
    public void TurningAutosaveOnWithNothingChangedDoesNotSave()
    {
        Preferences.Values[AutosaveKey] = "false";
        var layout = RenderEditor();

        Switch(layout).Change(true);

        Assert.Empty(BoardStore.Saves);
        Assert.Equal("", StatusText(layout));
    }

    [Fact]
    public void TurningAutosaveOffBeforeAPendingSaveRunsLeavesTheChangesUnsaved()
    {
        var layout = RenderEditor();
        Edit(layout);

        Switch(layout).Change(false);
        Clock.Advance(TimeSpan.FromSeconds(5));

        Assert.Empty(BoardStore.Saves);
        Assert.Equal("Unsaved changes", StatusText(layout));
    }

    private IRenderedComponent<MainLayout> RenderEditor()
    {
        var boardId = BoardStore.Add("Service map", new Board());
        var layout = RenderBoardEditor(boardId);
        layout.WaitForAssertion(() => Assert.NotEmpty(layout.FindAll(".d12-palette-entry-button")));
        return layout;
    }

    private static void Edit(IRenderedComponent<MainLayout> layout) =>
        layout.Find(".d12-palette-entry-button").Click();

    private static IElement Switch(IRenderedComponent<MainLayout> layout) =>
        layout.Find(".page-header #autosave-switch");

    private static IElement? SaveButton(IRenderedComponent<MainLayout> layout) =>
        layout
            .FindAll(".page-header button")
            .SingleOrDefault(button => button.TextContent.Trim() == "Save");

    private static string StatusText(IRenderedComponent<MainLayout> layout) =>
        layout.Find(".page-header [role=status]").TextContent.Trim();
}
