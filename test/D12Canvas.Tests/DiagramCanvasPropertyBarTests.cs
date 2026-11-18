using AngleSharp.Dom;
using Bunit;
using D12Canvas.BuiltIns;
using D12Canvas.Model;
using D12Canvas.Pointer;
using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

public class DiagramCanvasPropertyBarTests : ComponentTestBase
{
    private const string SuppressionCondition =
        ".diagram-container:has(.d12-property-bar:hover, .d12-property-bar:focus-within)";

    public DiagramCanvasPropertyBarTests()
    {
        SetupDiagramCanvasJsModule();
        var options = new D12CanvasOptions();
        BuiltInComponents.RegisterAll(options);
        Services.AddSingleton(options.Registry);
    }

    private static ComponentInstance AddRectangle(
        Board board,
        double x = 200,
        double y = 200,
        string? fill = "#ff0000",
        bool locked = false
    )
    {
        var instance = new ComponentInstance(
            "rectangle",
            new RectangleProps(fill, null, 2),
            new Bounds(x, y, 100, 60)
        )
        {
            Locked = locked,
        };
        board.AddComponent(instance);
        return instance;
    }

    private static ComponentInstance AddStickyNote(Board board, double x, double y)
    {
        var instance = new ComponentInstance(
            "sticky-note",
            new StickyNoteProps("", "#FFEB3B", "#000000", 14),
            new Bounds(x, y, 100, 60)
        );
        board.AddComponent(instance);
        return instance;
    }

    private static Edge AddEdge(Board board, string? color = null)
    {
        var edge = new Edge(
            new FloatingEndpoint(100, 400),
            new FloatingEndpoint(400, 400),
            color: color
        );
        board.AddEdge(edge);
        return edge;
    }

    private IRenderedComponent<DiagramCanvas> RenderCanvas(Board board) =>
        Render<DiagramCanvas>(parameters =>
                parameters.Add(p => p.Board, board).Add(p => p.SnapToGrid, false)
            )
            .ReturnToOrigin();

    private static void Select(
        IRenderedComponent<DiagramCanvas> canvas,
        Guid id,
        bool shift = false
    ) => canvas.ClickOn(canvas.ContainerOf(id), shift);

    private static IElement? Bar(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.FindAll(".d12-property-bar").SingleOrDefault();

    private static IReadOnlyList<string?> ControlLabels(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas
            .FindAll(".d12-property-bar-control")
            .Select(control => control.GetAttribute("aria-label"))
            .ToList();

    private static IElement Control(IRenderedComponent<DiagramCanvas> canvas, string label) =>
        canvas
            .FindAll(".d12-property-bar-control")
            .Single(control =>
                control.GetAttribute("aria-label") is { } name
                && (name == label || name.StartsWith($"{label}: ", StringComparison.Ordinal))
            );

    private static IElement Picker(IRenderedComponent<DiagramCanvas> canvas, string label) =>
        canvas.Find($".d12-property-bar-picker[aria-label='{label}: Custom colour']");

    private static void PickOption(
        IRenderedComponent<DiagramCanvas> canvas,
        string label,
        string optionName
    )
    {
        Control(canvas, label).Click();
        canvas
            .FindAll(".d12-property-bar-flyout [role=menuitemradio]")
            .Single(row => row.TextContent.Trim() == optionName)
            .Click();
    }

    private static string? CellStyle(IRenderedComponent<DiagramCanvas> canvas, string label) =>
        Control(canvas, label).Closest(".d12-property-bar-cell")!.GetAttribute("style");

    [Fact]
    public void NothingSelectedShowsNoBar()
    {
        var board = new Board();
        AddRectangle(board);
        var canvas = RenderCanvas(board);

        Assert.Null(Bar(canvas));
    }

    [Fact]
    public void ARectangleShowsItsFillStrokeAndStrokeWidthCentredAboveIt()
    {
        var board = new Board();
        var rectangle = AddRectangle(board, x: 200, y: 200);
        var canvas = RenderCanvas(board);

        Select(canvas, rectangle.Id);

        Assert.Equal(["Fill: #FF0000", "Stroke: Theme", "Stroke width"], ControlLabels(canvas));
        Assert.Equal("toolbar", Bar(canvas)!.GetAttribute("role"));
        Assert.Equal("left: 190px; top: 158px;", Bar(canvas)!.GetAttribute("style"));
    }

    [Fact]
    public void TheBarCentresOnTheWidthItMeasures()
    {
        ReportPropertyBarWidth(200);
        var board = new Board();
        var rectangle = AddRectangle(board, x: 200, y: 200);
        var canvas = RenderCanvas(board);

        Select(canvas, rectangle.Id);

        Assert.Equal("left: 150px; top: 158px;", Bar(canvas)!.GetAttribute("style"));
    }

    [Fact]
    public void TheBarFollowsThePanAndZoomOfTheSelection()
    {
        var board = new Board();
        var rectangle = AddRectangle(board, x: 200, y: 200);
        var canvas = RenderCanvas(board);
        Select(canvas, rectangle.Id);

        canvas.InvokeAsync(() =>
        {
            canvas.Instance.ZoomPanTracker.Scale = 2.0;
            canvas.Instance.ZoomPanTracker.SetPanPosition(-100, -150);
        });

        Assert.Equal("left: 340px; top: 208px;", Bar(canvas)!.GetAttribute("style"));
    }

    [Fact]
    public async Task ABarKeepsItsPlaceThroughAMoveSmallerThanTheMinimumAndFollowsALargerOne()
    {
        var board = new Board();
        var rectangle = AddRectangle(board, x: 200, y: 200);
        var canvas = RenderCanvas(board);
        Select(canvas, rectangle.Id);

        await canvas.InvokeAsync(() => canvas.Instance.ZoomPanTracker.SetPanPosition(-10, 0));

        Assert.Equal("left: 190px; top: 158px;", Bar(canvas)!.GetAttribute("style"));

        await canvas.InvokeAsync(() => canvas.Instance.ZoomPanTracker.SetPanPosition(-40, 0));

        Assert.Equal("left: 150px; top: 158px;", Bar(canvas)!.GetAttribute("style"));
    }

    [Fact]
    public async Task DuringAFramingFlightTheBarTravelsWithTheContent()
    {
        var board = new Board();
        var rectangle = AddRectangle(board, x: 200, y: 200);
        AddRectangle(board, x: 1400, y: 1000);
        var canvas = RenderCanvas(board);
        Select(canvas, rectangle.Id);

        await canvas.InvokeAsync(() => canvas.Instance.ZoomToSelection());

        Assert.Contains(
            "transition: left 250ms ease-out, top 250ms ease-out;",
            Bar(canvas)!.GetAttribute("style")
        );

        await canvas.InvokeAsync(() => canvas.Instance.ZoomPanTracker.SetPanPosition(0, 0));

        Assert.DoesNotContain("transition", Bar(canvas)!.GetAttribute("style"));
    }

    [Fact]
    public void ASelectionNearTheTopEdgeSlidesTheBarAlongTheEdgeRatherThanBelow()
    {
        var board = new Board();
        var rectangle = AddRectangle(board, x: 200, y: 10);
        var canvas = RenderCanvas(board);

        Select(canvas, rectangle.Id);

        Assert.Equal("left: 190px; top: 8px;", Bar(canvas)!.GetAttribute("style"));
    }

    [Fact]
    public async Task ChangingTheFillFromTheBarIsOneUndoableEntry()
    {
        var board = new Board();
        var rectangle = AddRectangle(board, fill: "#ff0000");
        var canvas = RenderCanvas(board);
        Select(canvas, rectangle.Id);

        Picker(canvas, "Fill").Change("#00ff00");

        Assert.Equal("#00ff00", ((RectangleProps)rectangle.Props).FillColor);
        Assert.Equal("--d12-icon-accent: #00ff00", CellStyle(canvas, "Fill"));

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal("#ff0000", ((RectangleProps)rectangle.Props).FillColor);
    }

    [Fact]
    public async Task TheBarIsAbsentForTheWholeOfADragAndReturnsAfterIt()
    {
        var board = new Board();
        var rectangle = AddRectangle(board);
        var canvas = RenderCanvas(board);
        Select(canvas, rectangle.Id);

        await canvas.Press(250, 230, role: HitRole.Instance, entityId: rectangle.Id);
        Assert.Null(Bar(canvas));
        await canvas.Move(290, 270);
        Assert.Null(Bar(canvas));
        await canvas.Release(290, 270);

        Assert.Equal("left: 230px; top: 198px;", Bar(canvas)!.GetAttribute("style"));
    }

    [Fact]
    public async Task TheBarIsAbsentWhileAMenuIsOpenAndReturnsWhenItCloses()
    {
        var board = new Board();
        var rectangle = AddRectangle(board);
        var canvas = RenderCanvas(board);
        Select(canvas, rectangle.Id);

        await canvas.InvokeAsync(() => canvas.Instance.OnContextMenuKeyPressed());
        Assert.NotEmpty(canvas.FindAll(".d12-context-menu"));
        Assert.Null(Bar(canvas));

        await canvas.InvokeAsync(() => canvas.Instance.OnEscapePressed());
        Assert.Empty(canvas.FindAll(".d12-context-menu"));
        Assert.NotNull(Bar(canvas));
    }

    [Fact]
    public void AnEdgeShowsRoutingArrowsAndColourAndARecolourShowsAtOnce()
    {
        var board = new Board();
        var edge = AddEdge(board);
        var canvas = RenderCanvas(board);
        canvas.ClickElement(canvas.Find(".edge-hit"));

        Assert.Equal(
            ["Routing: Straight", "Source arrow: None", "Target arrow: Arrow", "Colour: Theme"],
            ControlLabels(canvas)
        );
        Assert.NotEmpty(canvas.FindAll(".d12-property-bar .d12-property-bar-colour-themed"));

        Picker(canvas, "Colour").Change("#e5246b");

        Assert.Equal("#e5246b", edge.Color);
        Assert.Contains("#e5246b", canvas.Find(".edge-line").GetAttribute("style"));
        Assert.NotEmpty(canvas.FindAll(".edge-halo"));
    }

    // A themed swatch opens on the colour it holds, and the browser fires no change when the pick
    // ends on that colour, so the last colour the picker reported lands when the swatch is left.
    [Fact]
    public void PickingTheColourAThemedSwatchHoldsStillRecolours()
    {
        var board = new Board();
        var edge = AddEdge(board);
        var canvas = RenderCanvas(board);
        canvas.ClickElement(canvas.Find(".edge-hit"));
        var swatchValue = Picker(canvas, "Colour").GetAttribute("value")!;

        Picker(canvas, "Colour").Input("#123456");
        Picker(canvas, "Colour").Input(swatchValue);
        Picker(canvas, "Colour").Blur();

        Assert.Equal(swatchValue, edge.Color);
    }

    [Fact]
    public async Task AnEdgeRoutingChangeFromTheBarIsOneUndoableEntryThatKeepsItsColour()
    {
        var board = new Board();
        var edge = AddEdge(board, color: "#e5246b");
        var canvas = RenderCanvas(board);
        canvas.ClickElement(canvas.Find(".edge-hit"));

        PickOption(canvas, "Routing", "Curved");

        Assert.Equal(EdgeRouting.Curved, edge.RoutingStyle);
        Assert.Equal("#e5246b", edge.Color);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal(EdgeRouting.Straight, edge.RoutingStyle);
        Assert.Equal("#e5246b", edge.Color);
    }

    [Fact]
    public void AShapeAndAnEdgeSelectedTogetherShowNoBar()
    {
        var board = new Board();
        var rectangle = AddRectangle(board);
        AddEdge(board);
        var canvas = RenderCanvas(board);

        Select(canvas, rectangle.Id);
        canvas.ClickElement(canvas.Find(".edge-hit"), shift: true);

        Assert.Single(canvas.FindAll(".edge-halo"));
        Assert.Null(Bar(canvas));
    }

    [Fact]
    public async Task ASelectedGroupShowsTheRolesItsMembersShareAndACommitWritesEveryMember()
    {
        var board = new Board();
        var rectangle = AddRectangle(board, x: 100, y: 200);
        var note = AddStickyNote(board, 300, 200);
        board.AddGroup(new Group([rectangle.Id, note.Id]));
        var canvas = RenderCanvas(board);

        Select(canvas, rectangle.Id);

        Assert.Equal(["Fill: Mixed"], ControlLabels(canvas));
        Assert.NotEmpty(canvas.FindAll(".d12-property-bar-colour-mixed"));
        Assert.Equal("left: 190px; top: 158px;", Bar(canvas)!.GetAttribute("style"));

        Picker(canvas, "Fill").Change("#00ff00");

        Assert.Equal("#00ff00", ((RectangleProps)rectangle.Props).FillColor);
        Assert.Equal("#00ff00", ((StickyNoteProps)note.Props).Color);
        Assert.Empty(canvas.FindAll(".d12-property-bar-colour-mixed"));

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal("#ff0000", ((RectangleProps)rectangle.Props).FillColor);
        Assert.Equal("#FFEB3B", ((StickyNoteProps)note.Props).Color);
    }

    [Fact]
    public void AColourChipPaintsItsGlyphAndShowsTheHexOrAMutedThemeOrMixed()
    {
        var board = new Board();
        var red = AddRectangle(board, x: 100, fill: "#ff0000");
        var themed = AddRectangle(board, x: 300, fill: null);
        var canvas = RenderCanvas(board);

        Select(canvas, red.Id);
        var inked = Control(canvas, "Fill").Closest(".d12-property-bar-cell")!;
        Assert.Equal("--d12-icon-accent: #ff0000", inked.GetAttribute("style"));
        Assert.Equal("#FF0000", inked.QuerySelector(".d12-property-bar-value")!.TextContent);
        Assert.NotNull(inked.QuerySelector("svg.d12-icon .d12-icon-accent"));
        Assert.False(inked.ClassList.Contains("d12-property-bar-colour-themed"));
        Assert.False(inked.ClassList.Contains("d12-property-bar-colour-mixed"));

        Select(canvas, themed.Id);
        var themedCell = Control(canvas, "Fill").Closest(".d12-property-bar-cell")!;
        Assert.True(themedCell.ClassList.Contains("d12-property-bar-colour-themed"));
        Assert.Null(themedCell.GetAttribute("style"));
        Assert.Equal("Theme", themedCell.QuerySelector(".d12-property-bar-value")!.TextContent);
        Assert.Equal("Fill: Theme", Control(canvas, "Fill").GetAttribute("aria-label"));

        Select(canvas, red.Id, shift: true);
        var mixedCell = Control(canvas, "Fill").Closest(".d12-property-bar-cell")!;
        Assert.True(mixedCell.ClassList.Contains("d12-property-bar-colour-mixed"));
        Assert.Null(mixedCell.GetAttribute("style"));
        Assert.Equal("Mixed", mixedCell.QuerySelector(".d12-property-bar-value")!.TextContent);
        Assert.Equal("Fill: Mixed", Control(canvas, "Fill").GetAttribute("aria-label"));
    }

    [Fact]
    public void NoBarControlCarriesANativeTitle()
    {
        var board = new Board();
        var note = AddStickyNote(board, 200, 200);
        var canvas = RenderCanvas(board);

        Select(canvas, note.Id);

        Assert.Empty(canvas.FindAll(".d12-property-bar [title]"));
    }

    [Fact]
    public async Task AColourThatCanFollowTheThemeCanBeReturnedToItInOneEntry()
    {
        var board = new Board();
        var rectangle = AddRectangle(board, fill: "#ff0000");
        var canvas = RenderCanvas(board);
        Select(canvas, rectangle.Id);

        var fillCell = canvas.Find(".d12-property-bar-cell");
        fillCell.QuerySelector(".d12-property-bar-use-theme")!.Click();

        Assert.Null(((RectangleProps)rectangle.Props).FillColor);
        Assert.True(
            Control(canvas, "Fill")
                .Closest(".d12-property-bar-cell")!
                .ClassList.Contains("d12-property-bar-colour-themed")
        );
        Assert.Empty(canvas.FindAll(".d12-property-bar-use-theme"));

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal("#ff0000", ((RectangleProps)rectangle.Props).FillColor);
    }

    [Fact]
    public async Task AStickyNotesAlignmentsChangeFromTheBarAndUndo()
    {
        var board = new Board();
        var note = AddStickyNote(board, 200, 200);
        var canvas = RenderCanvas(board);
        Select(canvas, note.Id);

        Assert.Equal(
            [
                "Fill: #FFEB3B",
                "Text colour: #000000",
                "Font size",
                "Text align: Center",
                "Vertical align: Middle",
            ],
            ControlLabels(canvas)
        );

        PickOption(canvas, "Vertical align", "Bottom");
        PickOption(canvas, "Text align", "Right");

        Assert.Equal("bottom", ((StickyNoteProps)note.Props).VerticalAlign);
        Assert.Equal("right", ((StickyNoteProps)note.Props).TextAlign);
        Assert.Contains(
            "justify-content: safe flex-end",
            canvas.Find(".d12-sticky-note").GetAttribute("style")
        );

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());
        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal("middle", ((StickyNoteProps)note.Props).VerticalAlign);
        Assert.Equal("center", ((StickyNoteProps)note.Props).TextAlign);
    }

    [Fact]
    public void ATextBoxAndAStickyNoteShareOneTextAlignRowAndNoVerticalAlign()
    {
        var board = new Board();
        var note = AddStickyNote(board, 200, 200);
        var text = new ComponentInstance(
            "text",
            new TextProps("Label", null, 16, "normal", "left"),
            new Bounds(400, 200, 200, 40)
        );
        board.AddComponent(text);
        var canvas = RenderCanvas(board);

        Select(canvas, note.Id);
        Select(canvas, text.Id, shift: true);

        Assert.Equal(
            ["Text colour: Mixed", "Font size: Mixed", "Text align: Mixed"],
            ControlLabels(canvas)
        );
    }

    [Fact]
    public void AColourThatCannotHoldNullOffersNoWayToTheTheme()
    {
        var board = new Board();
        var note = AddStickyNote(board, 200, 200);
        var canvas = RenderCanvas(board);
        Select(canvas, note.Id);

        Assert.Empty(canvas.FindAll(".d12-property-bar-use-theme"));
    }

    [Fact]
    public void AMixedNumberIsEmptyWithADashPlaceholderAndMixedInItsName()
    {
        var board = new Board();
        var first = AddRectangle(board, x: 100);
        var second = AddRectangle(board, x: 300);
        second.Props = new RectangleProps("#ff0000", null, 6);
        var canvas = RenderCanvas(board);

        Select(canvas, first.Id);
        Select(canvas, second.Id, shift: true);

        var strokeWidth = Control(canvas, "Stroke width");
        Assert.Equal("", strokeWidth.GetAttribute("value"));
        Assert.Equal("\u2013", strokeWidth.GetAttribute("placeholder"));
        Assert.Equal("Stroke width: Mixed", strokeWidth.GetAttribute("aria-label"));
        Assert.Equal(
            "Stroke width: Mixed",
            strokeWidth
                .Closest(".d12-tooltip-anchor")!
                .QuerySelector(".d12-tooltip-above")!
                .TextContent
        );
    }

    [Fact]
    public void ALockedSelectionShowsABarOfOnlyUnlock()
    {
        var board = new Board();
        var rectangle = AddRectangle(board, locked: true);
        var canvas = RenderCanvas(board);

        canvas.ContainerOf(rectangle.Id).Focus();

        Assert.Single(canvas.FindAll(".component-container.selected"));
        Assert.Equal(
            ["Unlock"],
            canvas
                .FindAll(".d12-property-bar-control")
                .Select(control => control.TextContent.Trim())
        );
    }

    [Fact]
    public void EveryControlIsOutOfTheTabSequenceSinceCtrlEnterIsTheWayIn()
    {
        var board = new Board();
        var rectangle = AddRectangle(board);
        var canvas = RenderCanvas(board);

        Select(canvas, rectangle.Id);

        Assert.All(
            canvas.FindAll(".d12-property-bar-control"),
            control => Assert.Equal("-1", control.GetAttribute("tabindex"))
        );
    }

    [Fact]
    public void OneRuleKeyedOnTheBarsHoverAndFocusHidesEverySelectionChrome()
    {
        var board = new Board();
        var rectangle = AddRectangle(board);
        var canvas = RenderCanvas(board);
        Select(canvas, rectangle.Id);

        var css = canvas
            .FindAll("style")
            .Select(style => style.InnerHtml)
            .Single(text => text.Contains(".d12-property-bar {"));

        Assert.Equal(1, Occurrences(css, ".diagram-container:has("));
        var rule = ExtractBlock(css, SuppressionCondition);
        foreach (
            var chrome in new[]
            {
                ".selection-bounding-box",
                ".resize-handle",
                ".resize-span",
                ".port-span",
                ".port",
                ".edge-halo",
            }
        )
        {
            Assert.Contains($"& {chrome}", rule);
        }

        Assert.Contains("visibility: hidden", rule);
        Assert.Contains(
            "outline-color: transparent",
            ExtractBlock(rule, "& .component-container.selected")
        );
        Assert.DoesNotContain("transition", rule);
    }

    private static int Occurrences(string text, string value) =>
        (text.Length - text.Replace(value, "").Length) / value.Length;
}
