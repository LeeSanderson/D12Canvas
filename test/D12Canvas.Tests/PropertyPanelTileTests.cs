using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using D12Canvas.BuiltIns;
using D12Canvas.Model;
using D12Canvas.Panel;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

// A Dropdown whose every option has a glyph is drawn as a radio group of labelled tiles. The
// built-in text properties get them, and so does a host property that sets OptionGlyphs through
// the builder's EditableProperties override; a Dropdown without a glyph for every option stays a
// <select>.
public sealed class PropertyPanelTileTests : ComponentTestBase
{
    private const string HostTypeKey = "tile-test-host";

    private static readonly Glyph SquareGlyph = new(new GlyphPart("M3 3h10v10H3z"));
    private static readonly Glyph RoundGlyph = new(
        new GlyphPart("M3 8a5 5 0 1 0 10 0a5 5 0 1 0-10 0z")
    );

    private readonly Board _board = new();

    public PropertyPanelTileTests()
    {
        SetupDiagramCanvasJsModule();

        var services = new ServiceCollection();
        services.AddD12Canvas(options =>
            options.RegisterComponent<TileTestComponent, TileTestProps>(
                HostTypeKey,
                builder =>
                {
                    builder.DisplayName = "Tile test";
                    builder.AccessibleName = "Tile test";
                    builder.DefaultProps = new TileTestProps();
                    builder.EditableProperties =
                    [
                        new EditableProperty(
                            typeof(TileTestProps).GetProperty(nameof(TileTestProps.Shape))!,
                            EditorKind.Dropdown,
                            ["square", "round"],
                            OptionGlyphs: new Dictionary<string, Glyph>
                            {
                                ["square"] = SquareGlyph,
                                ["round"] = RoundGlyph,
                            }
                        ),
                        new EditableProperty(
                            typeof(TileTestProps).GetProperty(nameof(TileTestProps.Size))!,
                            EditorKind.Dropdown,
                            ["small", "large"]
                        ),
                        new EditableProperty(
                            typeof(TileTestProps).GetProperty(nameof(TileTestProps.Edge))!,
                            EditorKind.Dropdown,
                            ["thin", "thick"],
                            OptionGlyphs: new Dictionary<string, Glyph> { ["thin"] = SquareGlyph }
                        ),
                    ];
                }
            )
        );
        Services.AddSingleton(
            services.BuildServiceProvider().GetRequiredService<IComponentRegistry>()
        );
    }

    private ComponentInstance AddText(string weight = "normal", string align = "left", double x = 0)
    {
        var instance = new ComponentInstance(
            "text",
            new TextProps("words", null, 16, weight, align),
            new Bounds(x, 0, 200, 40)
        );
        _board.AddComponent(instance);
        return instance;
    }

    private ComponentInstance AddHost()
    {
        var instance = new ComponentInstance(
            HostTypeKey,
            new TileTestProps(),
            new Bounds(0, 0, 100, 100)
        );
        _board.AddComponent(instance);
        return instance;
    }

    private (
        IRenderedComponent<DiagramCanvas> Canvas,
        IRenderedComponent<PropertyPanel> Panel
    ) RenderAndSelectAll()
    {
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, _board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );
        var containers = canvas.FindAll(".component-container");
        canvas.ClickOn(containers[0]);
        foreach (var container in containers.Skip(1))
        {
            canvas.ClickOn(container, shift: true);
        }

        return (canvas, panel);
    }

    private static IReadOnlyList<IElement> Tiles(
        IRenderedComponent<PropertyPanel> panel,
        string fieldName
    ) => panel.FindAll($"#d12-property-panel-field-{fieldName} [role=radio]");

    private static string[] Checked(IRenderedComponent<PropertyPanel> panel, string fieldName) =>
        Tiles(panel, fieldName).Select(tile => tile.GetAttribute("aria-checked")!).ToArray();

    private static string[] TabIndexes(IRenderedComponent<PropertyPanel> panel, string fieldName) =>
        Tiles(panel, fieldName).Select(tile => tile.GetAttribute("tabindex")!).ToArray();

    private static TextProps PropsOf(ComponentInstance instance) => (TextProps)instance.Props;

    [Fact]
    public void ABuiltInGlyphedDropdownIsARadioGroupLabelledByItsFieldLabel()
    {
        AddText(weight: "bold", align: "center");
        var (_, panel) = RenderAndSelectAll();

        var group = panel.Find("#d12-property-panel-field-TextAlign");
        Assert.Equal("radiogroup", group.GetAttribute("role"));
        var labelId = group.GetAttribute("aria-labelledby")!;
        Assert.Equal("TextAlign", panel.Find($"#{labelId}").TextContent);

        Assert.Equal(
            ["Left", "Center", "Right"],
            Tiles(panel, "TextAlign").Select(tile => tile.TextContent.Trim()).ToArray()
        );
        Assert.Equal(["false", "true", "false"], Checked(panel, "TextAlign"));
        Assert.Equal(["false", "true"], Checked(panel, "FontWeight"));
        Assert.All(
            Tiles(panel, "TextAlign"),
            tile =>
                Assert.Equal(
                    "true",
                    tile.QuerySelector("svg.d12-icon")!.GetAttribute("aria-hidden")
                )
        );
        Assert.Empty(panel.FindAll("select"));
    }

    [Fact]
    public void TheStickyNotesAlignmentsAreTiles()
    {
        _board.AddComponent(
            new ComponentInstance(
                "sticky-note",
                new StickyNoteProps("", "#FFEB3B", "#000000", 14, "right", "bottom"),
                new Bounds(0, 0, 200, 200)
            )
        );
        var (_, panel) = RenderAndSelectAll();

        Assert.Equal(["false", "false", "true"], Checked(panel, "TextAlign"));
        Assert.Equal(["false", "false", "true"], Checked(panel, "VerticalAlign"));
    }

    [Fact]
    public void TheIconsInsideThePanelShareItsStyleBlock()
    {
        AddText();
        var (_, panel) = RenderAndSelectAll();

        Assert.NotEmpty(panel.FindAll("svg.d12-icon"));
        Assert.Single(panel.FindAll("style"));
    }

    [Fact]
    public void AMixedValueChecksNoTileAndTagsTheLabel()
    {
        AddText(weight: "normal");
        AddText(weight: "bold", x: 300);
        var (_, panel) = RenderAndSelectAll();

        Assert.Equal(["false", "false"], Checked(panel, "FontWeight"));
        var labelRow = panel.Find("#d12-property-panel-field-FontWeight").PreviousElementSibling!;
        Assert.Equal("Mixed", labelRow.QuerySelector(".d12-property-panel-mixed-tag")!.TextContent);
        Assert.Empty(
            panel
                .Find("#d12-property-panel-field-TextAlign")
                .PreviousElementSibling!.QuerySelectorAll(".d12-property-panel-mixed-tag")
        );
        Assert.Equal(["0", "-1"], TabIndexes(panel, "FontWeight"));
    }

    [Fact]
    public async Task PickingATileOnAMixedSelectionSetsEveryTargetInOneHistoryEntry()
    {
        var normal = AddText(weight: "normal");
        var bold = AddText(weight: "bold", x: 300);
        var (canvas, panel) = RenderAndSelectAll();

        Tiles(panel, "FontWeight")[1].Click();

        Assert.Equal("bold", PropsOf(normal).FontWeight);
        Assert.Equal("bold", PropsOf(bold).FontWeight);
        Assert.Equal(["false", "true"], Checked(panel, "FontWeight"));

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal("normal", PropsOf(normal).FontWeight);
        Assert.Equal("bold", PropsOf(bold).FontWeight);
    }

    [Fact]
    public void AFullyLockedSelectionDisablesEveryTileAndKeepsThePressedOne()
    {
        var text = AddText(align: "right");
        text.Locked = true;
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, _board));
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );
        canvas.ContainerOf(text.Id).Focus();

        Assert.All(Tiles(panel, "TextAlign"), tile => Assert.True(tile.HasAttribute("disabled")));
        Assert.Equal(["false", "false", "true"], Checked(panel, "TextAlign"));
        Assert.Equal(["-1", "-1", "-1"], TabIndexes(panel, "TextAlign"));
    }

    [Fact]
    public void TheCheckedTileIsTheGroupsOnlyTabStop()
    {
        AddText(align: "center");
        var (_, panel) = RenderAndSelectAll();

        Assert.Equal(["-1", "0", "-1"], TabIndexes(panel, "TextAlign"));
    }

    [Theory]
    [InlineData("ArrowRight", new[] { "-1", "-1", "0" })]
    [InlineData("ArrowDown", new[] { "-1", "-1", "0" })]
    [InlineData("ArrowLeft", new[] { "0", "-1", "-1" })]
    [InlineData("ArrowUp", new[] { "0", "-1", "-1" })]
    public async Task ArrowKeysMoveTheTabStopWithoutCommitting(string key, string[] expected)
    {
        var text = AddText(align: "center");
        var (canvas, panel) = RenderAndSelectAll();

        Tiles(panel, "TextAlign")[1].KeyDown(new KeyboardEventArgs { Key = key });

        Assert.Equal(expected, TabIndexes(panel, "TextAlign"));
        Assert.Equal(["false", "true", "false"], Checked(panel, "TextAlign"));
        Assert.Equal("center", PropsOf(text).TextAlign);

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());
        Assert.Equal("center", PropsOf(text).TextAlign);
    }

    [Fact]
    public void ArrowKeysWrapAtBothEnds()
    {
        AddText(align: "right");
        var (_, panel) = RenderAndSelectAll();

        Tiles(panel, "TextAlign")[2].KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        Assert.Equal(["0", "-1", "-1"], TabIndexes(panel, "TextAlign"));

        Tiles(panel, "TextAlign")[0].KeyDown(new KeyboardEventArgs { Key = "ArrowLeft" });
        Assert.Equal(["-1", "-1", "0"], TabIndexes(panel, "TextAlign"));
    }

    [Theory]
    [InlineData("Enter")]
    [InlineData(" ")]
    public async Task SpaceOrEnterCommitsTheFocusedTileAsOneHistoryEntry(string key)
    {
        var text = AddText(align: "left");
        var (canvas, panel) = RenderAndSelectAll();

        Tiles(panel, "TextAlign")[0].KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        Tiles(panel, "TextAlign")[1].KeyDown(new KeyboardEventArgs { Key = "ArrowRight" });
        Tiles(panel, "TextAlign")[2].KeyDown(new KeyboardEventArgs { Key = key });

        Assert.Equal("right", PropsOf(text).TextAlign);
        Assert.Equal(["false", "false", "true"], Checked(panel, "TextAlign"));

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());
        Assert.Equal("left", PropsOf(text).TextAlign);
    }

    [Fact]
    public void AHostPropertyWithAGlyphForEveryOptionGetsTiles()
    {
        AddHost();
        var (_, panel) = RenderAndSelectAll();

        Assert.Equal(
            ["Square", "Round"],
            Tiles(panel, "Shape").Select(tile => tile.TextContent.Trim()).ToArray()
        );
        Assert.Equal(["true", "false"], Checked(panel, "Shape"));
    }

    [Fact]
    public void AHostDropdownWithoutAGlyphForEveryOptionStaysASelect()
    {
        AddHost();
        var (_, panel) = RenderAndSelectAll();

        Assert.Equal("select", panel.Find("#d12-property-panel-field-Size").LocalName);
        Assert.Equal("select", panel.Find("#d12-property-panel-field-Edge").LocalName);
    }

    [Fact]
    public void TheImageFitDropdownStaysASelect()
    {
        _board.AddComponent(
            new ComponentInstance(
                "image",
                new ImageProps("", "", "cover"),
                new Bounds(0, 0, 240, 180)
            )
        );
        var (_, panel) = RenderAndSelectAll();

        Assert.Equal("select", panel.Find("#d12-property-panel-field-Fit").LocalName);
    }
}

internal sealed record TileTestProps(
    string Shape = "square",
    string Size = "small",
    string Edge = "thin"
);

internal sealed class TileTestComponent : ComponentBase
{
    [Parameter]
    public TileTestProps Props { get; set; } = new();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "span");
        builder.AddContent(1, Props.Shape);
        builder.CloseElement();
    }
}
