using AngleSharp.Dom;
using Bunit;
using D12Canvas.BuiltIns;
using D12Canvas.Model;
using D12Canvas.Panel;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

public sealed class PropertyBarChipTests : ComponentTestBase
{
    private const string HostTypeKey = "chip-test-host";

    private static readonly Glyph LeftGlyph = new(new GlyphPart("M3 4h10 M3 8h6 M3 12h10"));
    private static readonly Glyph CenterGlyph = new(new GlyphPart("M3 4h10 M5 8h6 M3 12h10"));

    private readonly Board _board = new();

    public PropertyBarChipTests()
    {
        SetupDiagramCanvasJsModule();

        var services = new ServiceCollection();
        services.AddD12Canvas(options =>
            options.RegisterComponent<ChipTestComponent, ChipTestProps>(
                HostTypeKey,
                builder =>
                {
                    builder.DisplayName = "Chip test";
                    builder.AccessibleName = "Chip test";
                    builder.DefaultProps = new ChipTestProps();
                    builder.EditableProperties =
                    [
                        new EditableProperty(
                            typeof(ChipTestProps).GetProperty(nameof(ChipTestProps.Align))!,
                            EditorKind.Dropdown,
                            ["left", "center", "justify"],
                            Role: PropertyRole.TextAlign,
                            OptionGlyphs: new Dictionary<string, Glyph>
                            {
                                ["left"] = LeftGlyph,
                                ["center"] = CenterGlyph,
                            }
                        ),
                    ];
                }
            )
        );
        Services.AddSingleton(
            services.BuildServiceProvider().GetRequiredService<IComponentRegistry>()
        );
    }

    private ComponentInstance AddStickyNote(double x, string textAlign = "center")
    {
        var instance = new ComponentInstance(
            "sticky-note",
            new StickyNoteProps("", "#FFEB3B", "#000000", 14, textAlign),
            new Bounds(x, 200, 100, 60)
        );
        _board.AddComponent(instance);
        return instance;
    }

    private ComponentInstance AddHost(string align)
    {
        var instance = new ComponentInstance(
            HostTypeKey,
            new ChipTestProps(align),
            new Bounds(200, 200, 100, 60)
        );
        _board.AddComponent(instance);
        return instance;
    }

    private IRenderedComponent<DiagramCanvas> RenderAndSelectAll()
    {
        var canvas = Render<DiagramCanvas>(parameters =>
                parameters.Add(p => p.Board, _board).Add(p => p.SnapToGrid, false)
            )
            .ReturnToOrigin();
        var containers = canvas.FindAll(".component-container");
        canvas.ClickOn(containers[0]);
        foreach (var container in containers.Skip(1))
        {
            canvas.ClickOn(container, shift: true);
        }

        return canvas;
    }

    private static IElement Chip(IRenderedComponent<DiagramCanvas> canvas, string label) =>
        canvas
            .FindAll(".d12-property-bar-chip")
            .Single(chip =>
                chip.GetAttribute("aria-label")!.StartsWith($"{label}: ", StringComparison.Ordinal)
            );

    private static IElement? OpenFlyout(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.FindAll(".d12-property-bar .d12-flyout").SingleOrDefault();

    private static IReadOnlyList<IElement> Rows(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.FindAll(".d12-property-bar .d12-flyout [role=menuitemradio]").ToList();

    private static string RowName(IElement row) => row.TextContent.Trim();

    private static string[] CheckedRows(IRenderedComponent<DiagramCanvas> canvas) =>
        Rows(canvas)
            .Where(row => row.GetAttribute("aria-checked") == "true")
            .Select(RowName)
            .ToArray();

    [Fact]
    public void AChoiceChipIsAMenuButtonNamedForItsRoleAndValueWithItsOptionsGlyph()
    {
        AddStickyNote(200, textAlign: "right");
        var canvas = RenderAndSelectAll();

        var chip = Chip(canvas, "Text align");

        Assert.Equal("BUTTON", chip.TagName);
        Assert.Equal("Text align: Right", chip.GetAttribute("aria-label"));
        Assert.Equal("menu", chip.GetAttribute("aria-haspopup"));
        Assert.Equal("false", chip.GetAttribute("aria-expanded"));
        Assert.Equal("-1", chip.GetAttribute("tabindex"));
        Assert.Equal("Right", chip.QuerySelector(".d12-property-bar-value")!.TextContent);
        var icons = chip.QuerySelectorAll("svg.d12-icon");
        Assert.Equal(2, icons.Length);
        Assert.Equal(
            Glyphs.TextAlignRight.Parts.Select(part => part.Data),
            icons[0].QuerySelectorAll("path").Select(path => path.GetAttribute("d"))
        );
        Assert.Contains("d12-property-bar-caret", icons[1].ClassList);
        Assert.Null(chip.GetAttribute("title"));
    }

    [Fact]
    public void AMixedChoiceChipShowsAMutedMixedAndNoOptionGlyph()
    {
        AddStickyNote(100, textAlign: "left");
        AddStickyNote(300, textAlign: "right");
        var canvas = RenderAndSelectAll();

        var chip = Chip(canvas, "Text align");

        Assert.Equal("Text align: Mixed", chip.GetAttribute("aria-label"));
        Assert.Contains("d12-property-bar-chip-mixed", chip.ClassList);
        Assert.Equal("Mixed", chip.QuerySelector(".d12-property-bar-value")!.TextContent);
        Assert.Single(chip.QuerySelectorAll("svg.d12-icon"));
    }

    [Fact]
    public void ClickingAChipOpensAFlyoutHeadedByTheRoleWithOneCheckedRowPerOption()
    {
        AddStickyNote(200);
        var canvas = RenderAndSelectAll();

        Chip(canvas, "Vertical align").Click();

        var flyout = OpenFlyout(canvas)!;
        Assert.Equal("menu", flyout.GetAttribute("role"));
        Assert.Equal("Vertical align", flyout.GetAttribute("aria-label"));
        Assert.Contains("d12-property-bar-flyout", flyout.ClassList);
        Assert.Equal("Vertical align", flyout.QuerySelector(".d12-flyout-heading")!.TextContent);
        Assert.Equal("true", Chip(canvas, "Vertical align").GetAttribute("aria-expanded"));
        Assert.Equal(["Top", "Middle", "Bottom"], Rows(canvas).Select(RowName));
        Assert.Equal(["Middle"], CheckedRows(canvas));
        Assert.All(Rows(canvas), row => Assert.NotNull(row.QuerySelector("svg.d12-icon")));
    }

    [Fact]
    public void AMixedValueChecksNoRow()
    {
        AddStickyNote(100, textAlign: "left");
        AddStickyNote(300, textAlign: "right");
        var canvas = RenderAndSelectAll();

        Chip(canvas, "Text align").Click();

        Assert.Equal(3, Rows(canvas).Count);
        Assert.Empty(CheckedRows(canvas));
    }

    [Fact]
    public async Task PickingARowCommitsOneEditAndClosesTheFlyout()
    {
        var note = AddStickyNote(200);
        var canvas = RenderAndSelectAll();

        Chip(canvas, "Text align").Click();
        Rows(canvas).Single(row => RowName(row) == "Left").Click();

        Assert.Equal("left", ((StickyNoteProps)note.Props).TextAlign);
        Assert.Null(OpenFlyout(canvas));
        Assert.Equal("Text align: Left", Chip(canvas, "Text align").GetAttribute("aria-label"));
        Assert.Equal("false", Chip(canvas, "Text align").GetAttribute("aria-expanded"));

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal("center", ((StickyNoteProps)note.Props).TextAlign);
    }

    [Fact]
    public void ClickingTheOpenChipAgainClosesTheFlyout()
    {
        AddStickyNote(200);
        var canvas = RenderAndSelectAll();

        Chip(canvas, "Text align").Click();
        Chip(canvas, "Text align").Click();

        Assert.Null(OpenFlyout(canvas));
    }

    [Fact]
    public async Task TheFlyoutClosesWhenItAsksTo()
    {
        AddStickyNote(200);
        var canvas = RenderAndSelectAll();
        Chip(canvas, "Text align").Click();

        var flyout = canvas.FindComponent<Flyout>();
        await canvas.InvokeAsync(() => flyout.Instance.RequestClose());

        Assert.Null(OpenFlyout(canvas));
    }

    [Fact]
    public void AnEdgesChoiceChipsReadTheirGlyphsFromTheEdgeRoles()
    {
        _board.AddEdge(
            new Edge(
                new FloatingEndpoint(100, 400),
                new FloatingEndpoint(400, 400),
                routingStyle: EdgeRouting.Orthogonal
            )
        );
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, _board));
        canvas.ClickElement(canvas.Find(".edge-hit"));

        var chip = Chip(canvas, "Routing");
        Assert.Equal("Routing: Orthogonal", chip.GetAttribute("aria-label"));
        Assert.Equal(
            Glyphs.RoutingOrthogonal.Parts.Select(part => part.Data),
            chip.QuerySelector("svg.d12-icon")!
                .QuerySelectorAll("path")
                .Select(path => path.GetAttribute("d"))
        );

        chip.Click();

        Assert.Equal(["Straight", "Orthogonal", "Curved"], Rows(canvas).Select(RowName));
        Assert.Equal(["Orthogonal"], CheckedRows(canvas));
    }

    [Fact]
    public void AHostPropertyWithOptionGlyphsGetsAChipAndAFlyout()
    {
        AddHost("center");
        var canvas = RenderAndSelectAll();

        var chip = Chip(canvas, "Text align");
        Assert.Equal("Text align: Center", chip.GetAttribute("aria-label"));
        Assert.Equal(
            CenterGlyph.Parts.Select(part => part.Data),
            chip.QuerySelector("svg.d12-icon")!
                .QuerySelectorAll("path")
                .Select(path => path.GetAttribute("d"))
        );

        chip.Click();

        Assert.Equal(["Left", "Center", "Justify"], Rows(canvas).Select(RowName));
        Assert.Equal(["Center"], CheckedRows(canvas));
    }

    [Fact]
    public void AHostValueWithNoGlyphShowsItsNameAloneAndItsRowKeepsAnEmptyGlyphColumn()
    {
        var host = AddHost("justify");
        var canvas = RenderAndSelectAll();

        var chip = Chip(canvas, "Text align");
        Assert.Equal("Justify", chip.QuerySelector(".d12-property-bar-value")!.TextContent);
        Assert.Single(chip.QuerySelectorAll("svg.d12-icon"));

        chip.Click();

        var justify = Rows(canvas).Single(row => RowName(row) == "Justify");
        Assert.Null(justify.QuerySelector("svg.d12-icon"));
        Assert.NotNull(justify.QuerySelector(".d12-property-bar-flyout-gap"));

        Rows(canvas).Single(row => RowName(row) == "Left").Click();

        Assert.Equal("left", ((ChipTestProps)host.Props).Align);
    }
}

internal sealed record ChipTestProps(string Align = "left");

internal sealed class ChipTestComponent : ComponentBase
{
    [Parameter]
    public ChipTestProps Props { get; set; } = new();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "span");
        builder.AddContent(1, Props.Align);
        builder.CloseElement();
    }
}
