using AngleSharp.Dom;
using Bunit;
using D12Canvas.App.Layout;
using D12Canvas.BuiltIns;
using D12Canvas.Model;

namespace D12Canvas.App.Tests;

public sealed class EditorControlsTests : AppTestContext
{
    [Fact]
    public void TheEditorHeaderShowsUndoRedoViewAndShortcuts()
    {
        var layout = RenderEditor();

        Assert.NotNull(layout.Find(".page-header button[aria-label='Undo']"));
        Assert.NotNull(layout.Find(".page-header button[aria-label='Redo']"));
        Assert.NotNull(ViewToggle(layout));
        Assert.NotNull(ShortcutsButton(layout));
    }

    [Fact]
    public void TheBoardListHeaderHasNoEditorControls()
    {
        var layout = RenderBoardList();

        Assert.Empty(layout.FindAll(".page-header button[aria-label='Undo']"));
        Assert.Empty(layout.FindAll(".page-header button[aria-label='Redo']"));
        Assert.Empty(layout.FindAll(".page-header button[aria-label='View']"));
        Assert.Empty(layout.FindAll(".page-header button[aria-label='Shortcuts']"));
        Assert.Empty(layout.FindAll("footer"));
    }

    [Fact]
    public void IconButtonsCarryTheirShortcutInTheTitle()
    {
        var layout = RenderEditor();

        Assert.Equal(
            "Undo (Ctrl+Z)",
            layout.Find(".page-header button[aria-label='Undo']").GetAttribute("title")
        );
        Assert.Equal(
            "Redo (Ctrl+Shift+Z)",
            layout.Find(".page-header button[aria-label='Redo']").GetAttribute("title")
        );
        Assert.Equal("Zoom in (PageUp)", ZoomInButton(layout).GetAttribute("title"));
        Assert.Equal("Zoom out (PageDown)", ZoomOutButton(layout).GetAttribute("title"));
    }

    [Fact]
    public void UndoAndRedoInTheHeaderUndoAndRedoAnEdit()
    {
        var (boardId, layout) = RenderEditorWithId();
        AddStickyNote(layout);
        Assert.Single(BoardStore.Get(boardId).Board.Components);

        layout.Find(".page-header button[aria-label='Undo']").Click();
        Assert.Empty(BoardStore.Get(boardId).Board.Components);

        layout.Find(".page-header button[aria-label='Redo']").Click();
        Assert.Single(BoardStore.Get(boardId).Board.Components);
    }

    [Fact]
    public void TheStatusBarCountsTheSelection()
    {
        var layout = RenderEditor();
        Assert.Equal("Nothing selected", SelectionText(layout));

        AddStickyNote(layout);

        layout.WaitForAssertion(() => Assert.Equal("1 selected", SelectionText(layout)));
    }

    [Fact]
    public void TheStatusBarZoomButtonsChangeTheZoom()
    {
        var layout = RenderEditor();
        Assert.Equal("100%", ZoomLevel(layout));

        ZoomInButton(layout).Click();
        layout.WaitForAssertion(() => Assert.Equal("110%", ZoomLevel(layout)));

        ZoomOutButton(layout).Click();
        ZoomOutButton(layout).Click();
        layout.WaitForAssertion(() => Assert.Equal("90%", ZoomLevel(layout)));
    }

    [Theory]
    [InlineData("Zoom to fit", "45%")]
    [InlineData("Zoom to selection", "55%")]
    [InlineData("Actual size", "100%")]
    public void TheZoomDropUpCommandsChangeTheZoom(string command, string expectedZoom)
    {
        var layout = RenderZoomedInPastFit();

        layout.Find("footer .dropup > button").Click();
        MenuItem(ZoomDropUpMenu(layout), command).Click();

        layout.WaitForAssertion(() => Assert.Equal(expectedZoom, ZoomLevel(layout)));
        Assert.DoesNotContain("show", ZoomDropUpMenu(layout).ClassList);
    }

    [Theory]
    [InlineData("Zoom to fit", "45%")]
    [InlineData("Zoom to selection", "55%")]
    [InlineData("Actual size", "100%")]
    public void TheViewMenuZoomCommandsChangeTheZoom(string command, string expectedZoom)
    {
        var layout = RenderZoomedInPastFit();

        ViewToggle(layout).Click();
        MenuItem(ViewMenu(layout), command).Click();

        layout.WaitForAssertion(() => Assert.Equal(expectedZoom, ZoomLevel(layout)));
        Assert.DoesNotContain("show", ViewMenu(layout).ClassList);
    }

    [Fact]
    public void TheViewMenuShowsEachZoomCommandsShortcut()
    {
        var layout = RenderEditor();

        var shortcuts = ViewMenu(layout)
            .QuerySelectorAll("button.dropdown-item .dropdown-shortcut")
            .Select(shortcut => shortcut.TextContent.Trim());

        Assert.Equal(["Shift+1", "Shift+2", "Shift+0"], shortcuts);
    }

    [Fact]
    public void TheGridToggleTurnsSnapToGridOffOnTheCanvasAndInTheViewMenu()
    {
        var layout = RenderEditor();
        Assert.Equal("true", StatusToggle(layout, "Grid").GetAttribute("aria-pressed"));
        Assert.True(ViewSwitch(layout, "snap-to-grid").HasAttribute("checked"));

        StatusToggle(layout, "Grid").Click();

        Assert.Equal("false", StatusToggle(layout, "Grid").GetAttribute("aria-pressed"));
        Assert.False(ViewSwitch(layout, "snap-to-grid").HasAttribute("checked"));
        Assert.False(Canvas(layout).SnapToGrid);
    }

    [Fact]
    public void TheSnappingToggleTurnsObjectSnappingOffOnTheCanvasAndInTheViewMenu()
    {
        var layout = RenderEditor();
        Assert.Equal("true", StatusToggle(layout, "Snapping").GetAttribute("aria-pressed"));
        Assert.True(ViewSwitch(layout, "object-snapping").HasAttribute("checked"));

        StatusToggle(layout, "Snapping").Click();

        Assert.Equal("false", StatusToggle(layout, "Snapping").GetAttribute("aria-pressed"));
        Assert.False(ViewSwitch(layout, "object-snapping").HasAttribute("checked"));
        Assert.False(Canvas(layout).ObjectSnapping);
    }

    [Fact]
    public void TheViewMenuSwitchesUpdateTheCanvasAndTheStatusBar()
    {
        var layout = RenderEditor();
        ViewToggle(layout).Click();

        ViewSwitch(layout, "snap-to-grid").Change(false);
        ViewSwitch(layout, "object-snapping").Change(false);

        Assert.False(Canvas(layout).SnapToGrid);
        Assert.False(Canvas(layout).ObjectSnapping);
        Assert.Equal("false", StatusToggle(layout, "Grid").GetAttribute("aria-pressed"));
        Assert.Equal("false", StatusToggle(layout, "Snapping").GetAttribute("aria-pressed"));
    }

    [Fact]
    public void TheViewMenuChecksTheCurrentWheelProfile()
    {
        var layout = RenderEditor();
        Assert.Equal("Auto", CheckedWheelProfile(layout));

        ViewToggle(layout).Click();
        MenuItem(ViewMenu(layout), "Trackpad").Click();

        Assert.Equal("Trackpad", CheckedWheelProfile(layout));
        Assert.Equal(WheelDeviceProfile.Trackpad, Canvas(layout).WheelDeviceProfile);
    }

    [Fact]
    public void TheShortcutsButtonShowsAndHidesTheShortcutsPanel()
    {
        var layout = RenderEditor();
        Assert.Equal("false", ShortcutsButton(layout).GetAttribute("aria-pressed"));
        Assert.Empty(layout.FindAll(".board-editor-shortcuts"));

        ShortcutsButton(layout).Click();

        Assert.Equal("true", ShortcutsButton(layout).GetAttribute("aria-pressed"));
        Assert.NotNull(layout.Find(".board-editor-shortcuts"));

        ShortcutsButton(layout).Click();

        Assert.Equal("false", ShortcutsButton(layout).GetAttribute("aria-pressed"));
        Assert.Empty(layout.FindAll(".board-editor-shortcuts"));
    }

    private IRenderedComponent<MainLayout> RenderEditor() => RenderEditorWithId().Layout;

    private (Guid BoardId, IRenderedComponent<MainLayout> Layout) RenderEditorWithId()
    {
        var boardId = BoardStore.Add("Service map", new Board());
        var layout = RenderBoardEditor(boardId);
        layout.WaitForAssertion(() => Assert.NotEmpty(layout.FindAll(".d12-palette-entry-button")));
        return (boardId, layout);
    }

    private IRenderedComponent<MainLayout> RenderZoomedInPastFit()
    {
        var board = new Board();
        board.AddComponent(
            new ComponentInstance(
                "sticky-note",
                new StickyNoteProps("", "#fff59d", "#000000", 14),
                new Bounds(0, 0, 1600, 1200)
            )
        );
        var boardId = BoardStore.Add("Floor plan", board);
        var layout = RenderBoardEditor(boardId);
        layout.WaitForAssertion(() => Assert.Equal("45%", ZoomLevel(layout)));
        ZoomInButton(layout).Click();
        layout.WaitForAssertion(() => Assert.Equal("55%", ZoomLevel(layout)));
        return layout;
    }

    private static void AddStickyNote(IRenderedComponent<MainLayout> layout) =>
        layout.Find(".d12-palette-entry-button").Click();

    private static DiagramCanvas Canvas(IRenderedComponent<MainLayout> layout) =>
        layout.FindComponent<DiagramCanvas>().Instance;

    private static IElement ViewToggle(IRenderedComponent<MainLayout> layout) =>
        layout.Find(".page-header button[aria-label='View']");

    private static IElement ViewMenu(IRenderedComponent<MainLayout> layout) =>
        ViewToggle(layout).ParentElement!.QuerySelector(".dropdown-menu")!;

    private static IElement ViewSwitch(IRenderedComponent<MainLayout> layout, string id) =>
        layout.Find($".page-header #{id}");

    private static IElement ShortcutsButton(IRenderedComponent<MainLayout> layout) =>
        layout.Find(".page-header button[aria-label='Shortcuts']");

    private static IElement MenuItem(IElement menu, string text) =>
        menu.QuerySelectorAll("button.dropdown-item")
            .Single(item => item.TextContent.Trim().StartsWith(text, StringComparison.Ordinal));

    private static string CheckedWheelProfile(IRenderedComponent<MainLayout> layout) =>
        ViewMenu(layout)
            .QuerySelectorAll("button.dropdown-item[aria-pressed='true']")
            .Single()
            .TextContent.Trim();

    private static string SelectionText(IRenderedComponent<MainLayout> layout) =>
        layout.Find("footer [role=status]").TextContent.Trim();

    private static IElement StatusToggle(IRenderedComponent<MainLayout> layout, string text) =>
        layout
            .FindAll("footer button[aria-pressed]")
            .Single(button => button.TextContent.Trim() == text);

    private static IElement ZoomInButton(IRenderedComponent<MainLayout> layout) =>
        layout.Find("footer button[aria-label='Zoom in']");

    private static IElement ZoomOutButton(IRenderedComponent<MainLayout> layout) =>
        layout.Find("footer button[aria-label='Zoom out']");

    private static IElement ZoomDropUpMenu(IRenderedComponent<MainLayout> layout) =>
        layout.Find("footer .dropup .dropdown-menu");

    private static string ZoomLevel(IRenderedComponent<MainLayout> layout) =>
        layout.Find("footer .dropup > button").TextContent.Trim();
}
