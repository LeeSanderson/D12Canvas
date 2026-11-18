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

// A colour role in the property bar is a menu button whose flyout holds the configured preset
// swatches, then "Theme colour" when every target can hold null, then "Custom…" for the native
// picker. One preset list serves every colour role, a host's own included.
public sealed class PropertyBarColourFlyoutTests : ComponentTestBase
{
    private const string HostTypeKey = "colour-flyout-host";

    private static readonly string[] DefaultPresetNames =
    [
        "Black",
        "Grey",
        "White",
        "Red",
        "Orange",
        "Yellow",
        "Green",
        "Blue",
        "Purple",
        "Pink",
    ];

    private readonly Board _board = new();

    public PropertyBarColourFlyoutTests()
    {
        SetupDiagramCanvasJsModule();
    }

    private void UseServices(Action<D12CanvasOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddD12Canvas(options =>
        {
            options.RegisterComponent<HostSwatchComponent, HostSwatchProps>(
                HostTypeKey,
                builder =>
                {
                    builder.DisplayName = "Host swatch";
                    builder.AccessibleName = "Host swatch";
                    builder.DefaultProps = new HostSwatchProps();
                    builder.EditableProperties =
                    [
                        new EditableProperty(
                            typeof(HostSwatchProps).GetProperty(nameof(HostSwatchProps.Tint))!,
                            EditorKind.Color,
                            Role: PropertyRole.Fill
                        ),
                    ];
                }
            );
            configure?.Invoke(options);
        });
        var provider = services.BuildServiceProvider();
        Services.AddSingleton(provider.GetRequiredService<IComponentRegistry>());
        Services.AddSingleton(provider.GetRequiredService<ColourPresetCatalog>());
    }

    private ComponentInstance AddRectangle(double x, string? fill)
    {
        var instance = new ComponentInstance(
            "rectangle",
            new RectangleProps(fill, null, 2),
            new Bounds(x, 200, 100, 60)
        );
        _board.AddComponent(instance);
        return instance;
    }

    private ComponentInstance AddStickyNote(double x)
    {
        var instance = new ComponentInstance(
            "sticky-note",
            new StickyNoteProps("", "#FFEB3B", "#000000", 14, "center"),
            new Bounds(x, 200, 100, 60)
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

    private static IReadOnlyList<IElement> Swatches(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas.FindAll(".d12-property-bar .d12-flyout .d12-property-bar-swatch").ToList();

    private static IElement Swatch(IRenderedComponent<DiagramCanvas> canvas, string name) =>
        Swatches(canvas).Single(swatch => swatch.GetAttribute("aria-label") == name);

    private static string[] CheckedSwatches(IRenderedComponent<DiagramCanvas> canvas) =>
        Swatches(canvas)
            .Where(swatch => swatch.GetAttribute("aria-checked") == "true")
            .Select(swatch => swatch.GetAttribute("aria-label")!)
            .ToArray();

    private static string[] RowNames(IRenderedComponent<DiagramCanvas> canvas) =>
        canvas
            .FindAll(".d12-property-bar .d12-flyout .d12-property-bar-flyout-item")
            .Select(row => row.TextContent.Trim())
            .ToArray();

    private static IElement Row(IRenderedComponent<DiagramCanvas> canvas, string name) =>
        canvas
            .FindAll(".d12-property-bar .d12-flyout .d12-property-bar-flyout-item")
            .Single(row => row.TextContent.Trim() == name);

    [Fact]
    public void AColourChipIsAMenuButtonNamedForItsRoleAndHex()
    {
        UseServices();
        AddRectangle(200, "#2f80ed");
        var canvas = RenderAndSelectAll();

        var chip = Chip(canvas, "Fill");

        Assert.Equal("BUTTON", chip.TagName);
        Assert.Equal("Fill: #2F80ED", chip.GetAttribute("aria-label"));
        Assert.Equal("menu", chip.GetAttribute("aria-haspopup"));
        Assert.Equal("false", chip.GetAttribute("aria-expanded"));
        Assert.Equal("#2F80ED", chip.QuerySelector(".d12-property-bar-value")!.TextContent);

        chip.Click();

        Assert.Equal("true", Chip(canvas, "Fill").GetAttribute("aria-expanded"));
        Assert.Equal("Fill", OpenFlyout(canvas)!.GetAttribute("aria-label"));
        Assert.Equal(
            "#2F80ED",
            Chip(canvas, "Fill").QuerySelector(".d12-property-bar-value")!.TextContent
        );
    }

    [Fact]
    public void TheDefaultPresetsShowInOrderAsNamedSwatchesWithANameAndHexTooltip()
    {
        UseServices();
        AddRectangle(200, "#ff0000");
        var canvas = RenderAndSelectAll();

        Chip(canvas, "Fill").Click();

        var swatches = Swatches(canvas);
        Assert.Equal(
            DefaultPresetNames,
            swatches.Select(swatch => swatch.GetAttribute("aria-label"))
        );
        Assert.All(swatches, swatch => Assert.Equal("menuitemradio", swatch.GetAttribute("role")));
        Assert.All(swatches, swatch => Assert.Null(swatch.QuerySelector("svg")));
        var blue = Swatch(canvas, "Blue");
        Assert.Equal("--d12-swatch: #2F80ED", blue.GetAttribute("style"));
        Assert.Equal(
            "Blue #2F80ED",
            blue.ParentElement!.QuerySelector(".d12-tooltip")!.TextContent
        );
        Assert.Equal(
            "5",
            canvas.Find(".d12-property-bar-swatches").GetAttribute("data-d12-flyout-columns")
        );
        Assert.Empty(canvas.FindAll(".d12-property-bar [title]"));
    }

    [Fact]
    public void ASwatchIsCheckedWhenTheValueMatchesItsHexIgnoringCase()
    {
        UseServices();
        AddRectangle(200, "#2f80ed");
        var canvas = RenderAndSelectAll();

        Chip(canvas, "Fill").Click();

        Assert.Equal(["Blue"], CheckedSwatches(canvas));
    }

    [Fact]
    public void AMixedOrThemedColourChecksNoSwatch()
    {
        UseServices();
        AddRectangle(100, "#2f80ed");
        AddRectangle(300, "#000000");
        var canvas = RenderAndSelectAll();

        Chip(canvas, "Fill").Click();

        Assert.Empty(CheckedSwatches(canvas));
        Assert.Equal("false", Row(canvas, "Theme colour").GetAttribute("aria-checked"));
    }

    [Fact]
    public void AThemedColourChecksThemeColourAndNoSwatch()
    {
        UseServices();
        AddRectangle(200, null);
        var canvas = RenderAndSelectAll();

        Chip(canvas, "Fill").Click();

        Assert.Empty(CheckedSwatches(canvas));
        Assert.Equal("true", Row(canvas, "Theme colour").GetAttribute("aria-checked"));
    }

    [Fact]
    public void ThemeColourAndCustomFollowTheGridWithoutGlyphsWhenEveryTargetCanHoldNull()
    {
        UseServices();
        AddRectangle(200, "#ff0000");
        var canvas = RenderAndSelectAll();

        Chip(canvas, "Fill").Click();

        Assert.Equal(["Theme colour", "Custom…"], RowNames(canvas));
        Assert.Equal("menuitemradio", Row(canvas, "Theme colour").GetAttribute("role"));
        Assert.Equal("menuitem", Row(canvas, "Custom…").GetAttribute("role"));
        Assert.Null(Row(canvas, "Theme colour").QuerySelector("svg"));
        Assert.Null(Row(canvas, "Custom…").QuerySelector("svg"));
        var flyoutChildren = OpenFlyout(canvas)!.Children.Select(child => child.ClassName).ToList();
        Assert.True(
            flyoutChildren.FindIndex(name => name!.Contains("d12-property-bar-swatches"))
                < flyoutChildren.FindIndex(name => name!.Contains("d12-property-bar-custom"))
        );
    }

    [Fact]
    public void ThemeColourIsMissingWhenATargetCannotHoldNull()
    {
        UseServices();
        AddRectangle(100, "#ff0000");
        AddStickyNote(300);
        var canvas = RenderAndSelectAll();

        Chip(canvas, "Fill").Click();

        Assert.Equal(["Custom…"], RowNames(canvas));
        Assert.Equal(10, Swatches(canvas).Count);
    }

    [Fact]
    public async Task PickingASwatchCommitsOneEditAndClosesTheFlyout()
    {
        UseServices();
        var rectangle = AddRectangle(200, "#ff0000");
        var canvas = RenderAndSelectAll();

        Chip(canvas, "Fill").Click();
        Swatch(canvas, "Green").Click();

        Assert.Equal("#2F9E44", ((RectangleProps)rectangle.Props).FillColor);
        Assert.Null(OpenFlyout(canvas));
        Assert.Equal("Fill: #2F9E44", Chip(canvas, "Fill").GetAttribute("aria-label"));

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal("#ff0000", ((RectangleProps)rectangle.Props).FillColor);
    }

    [Fact]
    public async Task PickingThemeColourCommitsNullInOneEditAndClosesTheFlyout()
    {
        UseServices();
        var rectangle = AddRectangle(200, "#ff0000");
        var canvas = RenderAndSelectAll();

        Chip(canvas, "Fill").Click();
        Row(canvas, "Theme colour").Click();

        Assert.Null(((RectangleProps)rectangle.Props).FillColor);
        Assert.Null(OpenFlyout(canvas));

        await canvas.InvokeAsync(() => canvas.Instance.OnUndoPressed());

        Assert.Equal("#ff0000", ((RectangleProps)rectangle.Props).FillColor);
    }

    [Fact]
    public void CustomClosesTheFlyoutAndLeavesTheValueForTheNativePicker()
    {
        UseServices();
        var rectangle = AddRectangle(200, "#ff0000");
        var canvas = RenderAndSelectAll();

        Chip(canvas, "Fill").Click();
        Row(canvas, "Custom…").Click();

        Assert.Null(OpenFlyout(canvas));
        Assert.Equal("#ff0000", ((RectangleProps)rectangle.Props).FillColor);
        var picker = canvas.Find(".d12-property-bar-picker");
        Assert.Equal("color", picker.GetAttribute("type"));
        Assert.Equal("Fill: Custom colour", picker.GetAttribute("aria-label"));
        Assert.Same(Chip(canvas, "Fill").ParentElement, picker.ParentElement);
    }

    [Fact]
    public void AHostsPresetsReplaceTheDefaults()
    {
        UseServices(options =>
            options.ColourPresets = [
                new ColourPreset("Brand ink", "#123456"),
                new ColourPreset("Brand paper", "#FAFAF0"),
            ]
        );
        AddRectangle(200, "#123456");
        var canvas = RenderAndSelectAll();

        Chip(canvas, "Fill").Click();

        Assert.Equal(
            ["Brand ink", "Brand paper"],
            Swatches(canvas).Select(swatch => swatch.GetAttribute("aria-label"))
        );
        Assert.Equal(["Brand ink"], CheckedSwatches(canvas));
    }

    [Fact]
    public void ASwatchTooltipShowsALowerCasePresetHexInUpperCaseAsTheChipDoes()
    {
        UseServices(options => options.ColourPresets = [new ColourPreset("Brand ink", "#abcdef")]);
        AddRectangle(200, "#ff0000");
        var canvas = RenderAndSelectAll();

        Chip(canvas, "Fill").Click();

        Assert.Equal(
            "Brand ink #ABCDEF",
            Swatch(canvas, "Brand ink").ParentElement!.QuerySelector(".d12-tooltip")!.TextContent
        );
    }

    [Fact]
    public void AnEmptyPresetListRemovesTheGridAndKeepsThemeColourAndCustom()
    {
        UseServices(options => options.ColourPresets = []);
        AddRectangle(200, "#ff0000");
        var canvas = RenderAndSelectAll();

        Chip(canvas, "Fill").Click();

        Assert.Empty(canvas.FindAll(".d12-property-bar-swatches"));
        Assert.Empty(Swatches(canvas));
        Assert.Equal(["Theme colour", "Custom…"], RowNames(canvas));
    }

    [Fact]
    public void EveryColourRoleOffersTheSameList()
    {
        UseServices(options => options.ColourPresets = [new ColourPreset("Brand", "#123456")]);
        AddStickyNote(100);
        _board.AddComponent(
            new ComponentInstance(
                HostTypeKey,
                new HostSwatchProps("#123456"),
                new Bounds(300, 200, 100, 60)
            )
        );
        AddRectangle(500, "#123456");
        _board.AddEdge(
            new Edge(
                new FloatingEndpoint(100, 400),
                new FloatingEndpoint(400, 400),
                color: "#123456"
            )
        );
        var canvas = Render<DiagramCanvas>(parameters =>
                parameters.Add(p => p.Board, _board).Add(p => p.SnapToGrid, false)
            )
            .ReturnToOrigin();

        foreach (
            var (label, select) in new (string, Action)[]
            {
                ("Fill", () => canvas.ClickOn(canvas.FindAll(".component-container")[0])),
                ("Text colour", () => canvas.ClickOn(canvas.FindAll(".component-container")[0])),
                ("Fill", () => canvas.ClickOn(canvas.FindAll(".component-container")[1])),
                ("Stroke", () => canvas.ClickOn(canvas.FindAll(".component-container")[2])),
                ("Colour", () => canvas.ClickElement(canvas.Find(".edge-hit"))),
            }
        )
        {
            select();
            Chip(canvas, label).Click();

            Assert.Equal(
                ["Brand"],
                Swatches(canvas).Select(swatch => swatch.GetAttribute("aria-label"))
            );

            Chip(canvas, label).Click();
        }
    }

    [Fact]
    public void AChipInTheBarsSecondHalfOpensItsFlyoutFromItsRightEdge()
    {
        UseServices();
        AddRectangle(200, "#ff0000");
        _board.AddEdge(new Edge(new FloatingEndpoint(100, 400), new FloatingEndpoint(400, 400)));
        var canvas = RenderAndSelectAll();

        Chip(canvas, "Fill").Click();
        Assert.DoesNotContain("d12-property-bar-flyout-end", OpenFlyout(canvas)!.ClassList);
        Chip(canvas, "Fill").Click();

        canvas.ClickElement(canvas.Find(".edge-hit"));
        Chip(canvas, "Colour").Click();
        Assert.Contains("d12-property-bar-flyout-end", OpenFlyout(canvas)!.ClassList);
    }

    [Fact]
    public void ThePropertyPanelKeepsItsNativePicker()
    {
        UseServices();
        AddRectangle(200, "#ff0000");
        var canvas = RenderAndSelectAll();
        var panel = Render<PropertyPanel>(parameters =>
            parameters.Add(p => p.Canvas, canvas.Instance)
        );

        Assert.NotEmpty(panel.FindAll("input[type=color]"));
        Assert.Empty(panel.FindAll(".d12-property-bar-swatch"));
    }
}

internal sealed record HostSwatchProps(string? Tint = null);

internal sealed class HostSwatchComponent : ComponentBase
{
    [Parameter]
    public HostSwatchProps Props { get; set; } = new();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "span");
        builder.AddContent(1, Props.Tint);
        builder.CloseElement();
    }
}
