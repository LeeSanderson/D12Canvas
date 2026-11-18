using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace D12Canvas.Tests;

// The menu reads nothing but the context it is handed, so every assertion here is about which rows
// that context composes, how they read and where the menu lands; which command a row runs on the
// board is DiagramCanvasContextMenuTests' concern.
public class ContextMenuTests : ComponentTestBase
{
    private static readonly ContextMenuContext SingleInstance = new(
        ContextMenuSet.Object,
        CanArrange: true
    );

    private static readonly ContextMenuContext EmptyBoardCanvas = new(ContextMenuSet.Canvas);

    private readonly BunitJSModuleInterop _menuModule;

    public ContextMenuTests()
    {
        _menuModule = JSInterop.SetupModule("./_content/D12Canvas/ContextMenu.razor.js");
        _menuModule
            .SetupModule("registerMenu", _ => true)
            .SetupVoid("dispose", _ => true)
            .SetVoidResult();
    }

    private IRenderedComponent<ContextMenu> RenderMenu(
        ContextMenuContext context,
        double x = 10,
        double y = 10,
        double containerWidth = 800,
        double containerHeight = 600,
        Action<ContextMenuCommand>? onInvoke = null,
        Action? onRequestClose = null
    ) =>
        RenderInIconScope<ContextMenu>(parameters =>
            parameters
                .Add(p => p.Context, context)
                .Add(p => p.X, x)
                .Add(p => p.Y, y)
                .Add(p => p.ContainerWidth, containerWidth)
                .Add(p => p.ContainerHeight, containerHeight)
                .Add(p => p.OnInvoke, command => onInvoke?.Invoke(command))
                .Add(p => p.OnRequestClose, () => onRequestClose?.Invoke())
        );

    private static string PathData(Glyph glyph) =>
        string.Join(" ", glyph.Parts.Select(part => part.Data));

    private static string IconPathData(IElement icon) =>
        string.Join(" ", icon.QuerySelectorAll("path").Select(path => path.GetAttribute("d")));

    // The rows top to bottom, with "|" standing for each separator.
    private static string[] Layout(IRenderedComponent<ContextMenu> menu) =>
        menu.Find(".d12-context-menu")
            .Children.Select(child =>
                child.ClassList.Contains("d12-context-menu-separator")
                    ? "|"
                    : child.QuerySelector(".d12-context-menu-label")!.TextContent
            )
            .ToArray();

    private static IElement RowNamed(IRenderedComponent<ContextMenu> menu, string label) =>
        menu.FindAll(".d12-context-menu-item")
            .Single(item => item.QuerySelector(".d12-context-menu-label")!.TextContent == label);

    private static string? HintOf(IRenderedComponent<ContextMenu> menu, string label) =>
        RowNamed(menu, label).QuerySelector(".d12-context-menu-hint")?.TextContent;

    private static (string Left, string Top) Position(IRenderedComponent<ContextMenu> menu)
    {
        var style = menu.Find(".d12-context-menu").GetAttribute("style")!;
        var parts = style
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.Split(':', StringSplitOptions.TrimEntries))
            .ToDictionary(pair => pair[0], pair => pair[1]);
        return (parts["left"], parts["top"]);
    }

    [Fact]
    public void ASingleInstanceShowsDeleteThenTheFourLayeringRowsThenLockThenZoomToSelectionEachInItsOwnSection()
    {
        var menu = RenderMenu(SingleInstance);

        Assert.Equal(
            [
                "Delete",
                "|",
                "Bring to Front",
                "Bring Forward",
                "Send Backward",
                "Send to Back",
                "|",
                "Lock",
                "|",
                "Zoom to Selection",
            ],
            Layout(menu)
        );
    }

    [Fact]
    public void GroupingRowsSitBetweenDeleteAndLayeringInOneSection()
    {
        var menu = RenderMenu(SingleInstance with { CanGroup = true, CanUngroup = true });

        Assert.Equal(
            [
                "Delete",
                "|",
                "Group",
                "Ungroup",
                "|",
                "Bring to Front",
                "Bring Forward",
                "Send Backward",
                "Send to Back",
                "|",
                "Lock",
                "|",
                "Zoom to Selection",
            ],
            Layout(menu)
        );
    }

    [Fact]
    public void UngroupIsAbsentWhenNothingSelectedIsAGroupAndItsSeparatorGoesWithIt()
    {
        var menu = RenderMenu(SingleInstance);

        Assert.DoesNotContain("Ungroup", Layout(menu));
        Assert.Equal(3, menu.FindAll(".d12-context-menu-separator").Count);
    }

    [Fact]
    public void AnEdgeOnlySelectionShowsDeleteThenLockThenZoomToSelection()
    {
        var menu = RenderMenu(new ContextMenuContext(ContextMenuSet.Object));

        Assert.Equal(["Delete", "|", "Lock", "|", "Zoom to Selection"], Layout(menu));
    }

    [Fact]
    public void TheCanvasSetShowsSelectAllThenTheViewRowsThenTheTwoSnapToggles()
    {
        var menu = RenderMenu(EmptyBoardCanvas with { CanSelectAll = true });

        Assert.Equal(
            [
                "Select All",
                "|",
                "Zoom to Fit",
                "Zoom to 100%",
                "|",
                "Snap to Grid",
                "Object Snapping",
            ],
            Layout(menu)
        );
    }

    [Fact]
    public void TheCanvasSetOnAnEmptyBoardLeavesSelectAllOut()
    {
        var menu = RenderMenu(EmptyBoardCanvas);

        Assert.Equal(
            ["Zoom to Fit", "Zoom to 100%", "|", "Snap to Grid", "Object Snapping"],
            Layout(menu)
        );
    }

    [Fact]
    public void TheCanvasSetNeverShowsAnObjectRowEvenWhenTheSelectionCouldUseIt()
    {
        var menu = RenderMenu(
            new ContextMenuContext(
                ContextMenuSet.Canvas,
                CanGroup: true,
                CanUngroup: true,
                CanArrange: true
            )
        );

        Assert.Equal(
            ["Zoom to Fit", "Zoom to 100%", "|", "Snap to Grid", "Object Snapping"],
            Layout(menu)
        );
    }

    [Fact]
    public void TheObjectSetNeverShowsACanvasRow()
    {
        var menu = RenderMenu(SingleInstance with { CanSelectAll = true, SnapToGrid = true });

        var layout = Layout(menu);
        Assert.DoesNotContain("Select All", layout);
        Assert.DoesNotContain("Snap to Grid", layout);
        Assert.DoesNotContain("Object Snapping", layout);
    }

    [Fact]
    public void NoRowIsEverDisabled()
    {
        var menu = RenderMenu(SingleInstance with { CanGroup = true });

        Assert.All(
            menu.FindAll(".d12-context-menu-item"),
            item =>
            {
                Assert.False(item.HasAttribute("disabled"));
                Assert.False(item.HasAttribute("aria-disabled"));
            }
        );
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void TheSnapTogglesAreCheckboxRowsCarryingTheirState(
        bool snapToGrid,
        bool objectSnapping
    )
    {
        var menu = RenderMenu(
            EmptyBoardCanvas with
            {
                SnapToGrid = snapToGrid,
                ObjectSnapping = objectSnapping,
            }
        );

        var snapRow = RowNamed(menu, "Snap to Grid");
        var objectRow = RowNamed(menu, "Object Snapping");
        Assert.Equal("menuitemcheckbox", snapRow.GetAttribute("role"));
        Assert.Equal(snapToGrid ? "true" : "false", snapRow.GetAttribute("aria-checked"));
        Assert.Equal(objectSnapping ? "true" : "false", objectRow.GetAttribute("aria-checked"));
        Assert.Equal(
            snapToGrid ? [PathData(Glyphs.Check)] : [],
            snapRow
                .QuerySelectorAll(".d12-context-menu-check svg.d12-icon")
                .Select(IconPathData)
                .ToArray()
        );
        Assert.Equal(
            objectSnapping ? 1 : 0,
            objectRow.QuerySelectorAll(".d12-context-menu-check svg.d12-icon").Length
        );
    }

    [Fact]
    public void ACheckedToggleRowDrawsTheCheckInItsOwnColumnBeforeItsOwnGlyph()
    {
        var menu = RenderMenu(EmptyBoardCanvas with { SnapToGrid = true });

        var snapRow = RowNamed(menu, "Snap to Grid");
        var columns = snapRow.Children.Take(2).ToArray();
        Assert.Contains("d12-context-menu-check", columns[0].ClassList);
        Assert.Equal(PathData(Glyphs.Check), IconPathData(columns[0].QuerySelector("svg")!));
        Assert.Equal("svg", columns[1].TagName.ToLowerInvariant());
        Assert.Equal(PathData(Glyphs.SnapToGrid), IconPathData(columns[1]));
    }

    [Theory]
    [MemberData(nameof(EveryRowWithItsGlyph))]
    public void EveryRowLeadsWithItsDecorativeGlyphAndIsNamedByItsLabel(string label, Glyph glyph)
    {
        var menu = RenderMenu(EveryRowContext(label));

        var row = RowNamed(menu, label);
        var icon = row.Children.Single(child => child.ClassList.Contains("d12-icon"));
        Assert.Equal(PathData(glyph), IconPathData(icon));
        Assert.Equal("true", icon.GetAttribute("aria-hidden"));
        Assert.Same(icon.NextElementSibling, row.QuerySelector(".d12-context-menu-label"));
        Assert.False(row.HasAttribute("aria-label"));
    }

    public static TheoryData<string, Glyph> EveryRowWithItsGlyph() =>
        new()
        {
            { "Cut", Glyphs.Cut },
            { "Copy", Glyphs.Copy },
            { "Paste", Glyphs.Paste },
            { "Duplicate", Glyphs.Duplicate },
            { "Delete", Glyphs.Delete },
            { "Group", Glyphs.Group },
            { "Ungroup", Glyphs.Ungroup },
            { "Bring to Front", Glyphs.BringToFront },
            { "Bring Forward", Glyphs.BringForward },
            { "Send Backward", Glyphs.SendBackward },
            { "Send to Back", Glyphs.SendToBack },
            { "Lock", Glyphs.Lock },
            { "Unlock", Glyphs.Unlock },
            { "Zoom to Selection", Glyphs.ZoomToSelection },
            { "Add port here", Glyphs.AddPort },
            { "Add port…", Glyphs.AddPort },
            { "Remove port", Glyphs.RemovePort },
            { "Choose image…", Glyphs.ChooseImage },
            { "Remove image", Glyphs.RemoveImage },
            { "Select All", Glyphs.SelectAll },
            { "Zoom to Fit", Glyphs.ZoomToFit },
            { "Zoom to 100%", Glyphs.ZoomTo100Percent },
            { "Snap to Grid", Glyphs.SnapToGrid },
            { "Object Snapping", Glyphs.ObjectSnapping },
            { "Unlock All", Glyphs.Unlock },
        };

    private static ContextMenuContext EveryRowContext(string label) =>
        label switch
        {
            "Select All"
            or "Zoom to Fit"
            or "Zoom to 100%"
            or "Snap to Grid"
            or "Object Snapping"
            or "Unlock All" => EmptyBoardCanvas with
            {
                CanSelectAll = true,
                CanUnlockAll = true,
            },
            "Unlock" => SingleInstance with { SelectionLocked = true },
            "Add port…" => SingleInstance with { CanPlacePort = true },
            _ => SingleInstance with
            {
                AsyncClipboard = true,
                CanCut = true,
                CanCopy = true,
                CanDelete = true,
                CanGroup = true,
                CanUngroup = true,
                CanAddPortHere = true,
                CanRemovePort = true,
                CanChangePicture = true,
            },
        };

    [Fact]
    public void TheStripButtonsDrawTheAlignAndDistributeGlyphsAndKeepTheirNames()
    {
        var menu = RenderMenu(SingleInstance with { CanAlign = true, CanDistribute = true });

        var buttons = menu.FindAll(".d12-context-menu-strip button");
        Assert.Equal(
            [
                ("Align left", PathData(Glyphs.AlignLeft)),
                ("Align centre", PathData(Glyphs.AlignCentre)),
                ("Align right", PathData(Glyphs.AlignRight)),
                ("Align top", PathData(Glyphs.AlignTop)),
                ("Align middle", PathData(Glyphs.AlignMiddle)),
                ("Align bottom", PathData(Glyphs.AlignBottom)),
                ("Distribute horizontally", PathData(Glyphs.DistributeHorizontally)),
                ("Distribute vertically", PathData(Glyphs.DistributeVertically)),
            ],
            buttons.Select(button =>
                (
                    button.GetAttribute("aria-label")!,
                    IconPathData(button.QuerySelector("svg.d12-icon")!)
                )
            )
        );
        Assert.All(
            buttons,
            button => Assert.Equal("true", button.QuerySelector("svg")!.GetAttribute("aria-hidden"))
        );
    }

    [Fact]
    public void TheMenusIconsBringNoStyleBlockOfTheirOwn()
    {
        var menu = RenderMenu(
            SingleInstance with
            {
                CanAlign = true,
                CanDistribute = true,
                CanGroup = true,
            }
        );

        Assert.Single(menu.FindAll("style"));
    }

    [Fact]
    public void ACommandRowIsAPlainMenuItemWithNoCheckedState()
    {
        var menu = RenderMenu(SingleInstance);

        var delete = RowNamed(menu, "Delete");
        Assert.Equal("menuitem", delete.GetAttribute("role"));
        Assert.False(delete.HasAttribute("aria-checked"));
        Assert.Empty(menu.FindAll(".d12-context-menu-check"));
    }

    [Fact]
    public void EveryRowIsAFocusableButton()
    {
        var menu = RenderMenu(SingleInstance with { CanGroup = true, CanUngroup = true });

        foreach (var item in menu.FindAll(".d12-context-menu-item"))
        {
            Assert.Equal("button", item.TagName.ToLowerInvariant());
            Assert.Equal("button", item.GetAttribute("type"));
        }
    }

    [Fact]
    public void ClickingARowReportsItsCommand()
    {
        var invoked = new List<ContextMenuCommand>();
        var menu = RenderMenu(
            SingleInstance with
            {
                CanGroup = true,
                CanUngroup = true,
            },
            onInvoke: invoked.Add
        );

        foreach (
            var label in new[]
            {
                "Delete",
                "Group",
                "Ungroup",
                "Bring to Front",
                "Bring Forward",
                "Send Backward",
                "Send to Back",
            }
        )
        {
            RowNamed(menu, label).Click();
        }

        Assert.Equal(
            [
                ContextMenuCommand.Delete,
                ContextMenuCommand.Group,
                ContextMenuCommand.Ungroup,
                ContextMenuCommand.BringToFront,
                ContextMenuCommand.BringForward,
                ContextMenuCommand.SendBackward,
                ContextMenuCommand.SendToBack,
            ],
            invoked
        );
    }

    [Fact]
    public void ClickingACanvasRowReportsItsCommand()
    {
        var invoked = new List<ContextMenuCommand>();
        var menu = RenderMenu(EmptyBoardCanvas with { CanSelectAll = true }, onInvoke: invoked.Add);

        RowNamed(menu, "Select All").Click();
        RowNamed(menu, "Snap to Grid").Click();
        RowNamed(menu, "Object Snapping").Click();

        Assert.Equal(
            [
                ContextMenuCommand.SelectAll,
                ContextMenuCommand.ToggleSnapToGrid,
                ContextMenuCommand.ToggleObjectSnapping,
            ],
            invoked
        );
    }

    [Fact]
    public void EveryLiveChordIsHintedInWordsOffApplePlatforms()
    {
        var menu = RenderMenu(SingleInstance with { CanGroup = true, CanUngroup = true });

        Assert.Equal("Delete", HintOf(menu, "Delete"));
        Assert.Equal("Ctrl+G", HintOf(menu, "Group"));
        Assert.Equal("Ctrl+Shift+G", HintOf(menu, "Ungroup"));
        Assert.Equal("Ctrl+Shift+]", HintOf(menu, "Bring to Front"));
        Assert.Equal("Ctrl+]", HintOf(menu, "Bring Forward"));
        Assert.Equal("Ctrl+[", HintOf(menu, "Send Backward"));
        Assert.Equal("Ctrl+Shift+[", HintOf(menu, "Send to Back"));
        Assert.Equal("Shift+2", HintOf(menu, "Zoom to Selection"));
    }

    [Fact]
    public void EveryLiveChordIsHintedInSymbolsOnApplePlatforms()
    {
        var menu = RenderMenu(
            SingleInstance with
            {
                CanGroup = true,
                CanUngroup = true,
                ApplePlatform = true,
            }
        );

        Assert.Equal("⌫", HintOf(menu, "Delete"));
        Assert.Equal("⌘G", HintOf(menu, "Group"));
        Assert.Equal("⇧⌘G", HintOf(menu, "Ungroup"));
        Assert.Equal("⇧⌘]", HintOf(menu, "Bring to Front"));
        Assert.Equal("⌘[", HintOf(menu, "Send Backward"));
        Assert.Equal("⇧2", HintOf(menu, "Zoom to Selection"));
    }

    [Fact]
    public void TheCanvasRowsHintSelectAllAndSnapToGridButNotObjectSnapping()
    {
        var menu = RenderMenu(EmptyBoardCanvas with { CanSelectAll = true });

        Assert.Equal("Ctrl+A", HintOf(menu, "Select All"));
        Assert.Equal("Ctrl+'", HintOf(menu, "Snap to Grid"));
        Assert.Null(HintOf(menu, "Object Snapping"));
    }

    [Fact]
    public void TheViewRowsHintTheirShiftDigitChords()
    {
        var words = RenderMenu(EmptyBoardCanvas);
        Assert.Equal("Shift+1", HintOf(words, "Zoom to Fit"));
        Assert.Equal("Shift+0", HintOf(words, "Zoom to 100%"));

        var symbols = RenderMenu(EmptyBoardCanvas with { ApplePlatform = true });
        Assert.Equal("⇧1", HintOf(symbols, "Zoom to Fit"));
        Assert.Equal("⇧0", HintOf(symbols, "Zoom to 100%"));
    }

    [Fact]
    public void TheSnapToGridHintGoesWhenTheHostDisablesTheChordAndTheRowStays()
    {
        var menu = RenderMenu(EmptyBoardCanvas with { SnapToGridChordLive = false });

        Assert.Null(HintOf(menu, "Snap to Grid"));
    }

    [Fact]
    public void AHintIsHiddenFromAssistiveTechSoTheRowIsNamedByItsLabel()
    {
        var menu = RenderMenu(SingleInstance);

        Assert.All(
            menu.FindAll(".d12-context-menu-hint"),
            hint => Assert.Equal("true", hint.GetAttribute("aria-hidden"))
        );
    }

    [Fact]
    public void OpensRightAndDownFromTheAnchorWhenThereIsRoom()
    {
        var menu = RenderMenu(SingleInstance, x: 120, y: 80);

        Assert.Equal(("120px", "80px"), Position(menu));
    }

    [Fact]
    public void NearTheBottomRightCornerItOpensUpAndLeftOfTheAnchor()
    {
        var menu = RenderMenu(SingleInstance, x: 780, y: 590);

        var height = ContextMenuPlacement.HeightOf(ContextMenuComposition.Compose(SingleInstance));
        Assert.Equal(
            (
                $"{780 - ContextMenuPlacement.Width}px",
                $"{(590 - height).ToString(System.Globalization.CultureInfo.InvariantCulture)}px"
            ),
            Position(menu)
        );
    }

    // Placement computes the menu's height from these numbers rather than measuring it, so the
    // stylesheet and ContextMenuPlacement must describe the same box.
    [Fact]
    public void TheStylesheetDrawsTheBoxPlacementAssumes()
    {
        var css = StyleBlockText(RenderMenu(SingleInstance));

        var root = ExtractEveryBlock(css, ".d12-context-menu {");
        var item = ExtractBlock(css, ".d12-context-menu-item {");
        var separator = ExtractBlock(css, ".d12-context-menu-separator {");
        Assert.Contains($"width: {ContextMenuPlacement.Width}px;", root);
        Assert.Contains("padding: 4px;", root);
        Assert.Contains("border: 1px solid", root);
        Assert.Equal(ContextMenuPlacement.Frame, 2 * 4 + 2 * 1);
        Assert.Contains($"height: {ContextMenuPlacement.RowHeight}px;", item);
        Assert.Contains("box-sizing: border-box;", item);
        Assert.Contains("height: 1px;", separator);
        Assert.Contains("margin: 4px 6px;", separator);
        Assert.Equal(ContextMenuPlacement.SeparatorHeight, 1 + 2 * 4);
    }

    [Fact]
    public void ItsAnchorReachesTheJavaScriptThatDismissesItAndRovesFocus()
    {
        RenderMenu(SingleInstance);

        var registration = Assert.Single(_menuModule.Invocations["registerMenu"]);
        Assert.IsType<Microsoft.AspNetCore.Components.ElementReference>(registration.Arguments[0]);
    }

    [Fact]
    public async Task ARequestToCloseFromTheBrowserReachesTheCanvas()
    {
        var closed = false;
        var menu = RenderMenu(SingleInstance, onRequestClose: () => closed = true);

        await menu.InvokeAsync(() => menu.Instance.RequestClose());

        Assert.True(closed);
    }

    [Fact]
    public void ALockedSelectionReadsUnlockWithTheLockChordAndOffersNoDelete()
    {
        var menu = RenderMenu(SingleInstance with { SelectionLocked = true, CanDelete = false });

        Assert.Contains("Unlock", Layout(menu));
        Assert.DoesNotContain("Lock", Layout(menu));
        Assert.DoesNotContain("Delete", Layout(menu));
        Assert.Equal("Ctrl+Shift+L", HintOf(menu, "Unlock"));
    }

    [Fact]
    public void UnlockAllIsTheCanvasMenusLastRowWithNoHintAndOnlyWhileSomethingIsLocked()
    {
        var withLocks = RenderMenu(EmptyBoardCanvas with { CanUnlockAll = true });
        var withoutLocks = RenderMenu(EmptyBoardCanvas);

        Assert.Equal(["|", "Unlock All"], Layout(withLocks).TakeLast(2));
        Assert.Null(HintOf(withLocks, "Unlock All"));
        Assert.DoesNotContain("Unlock All", Layout(withoutLocks));
    }

    [Fact]
    public void UnlockAllIsNeverOnTheObjectMenu()
    {
        Assert.DoesNotContain(
            "Unlock All",
            Layout(RenderMenu(SingleInstance with { CanUnlockAll = true }))
        );
    }

    [Fact]
    public void TheMenuIsNamedForTheSetItShows()
    {
        Assert.Equal(
            "Selection actions",
            RenderMenu(SingleInstance).Find(".d12-context-menu").GetAttribute("aria-label")
        );
        Assert.Equal(
            "Canvas actions",
            RenderMenu(EmptyBoardCanvas).Find(".d12-context-menu").GetAttribute("aria-label")
        );
    }
}
