using System.Linq;
using AngleSharp.Dom;
using Bunit;
using D12Canvas.Model;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

public class PaletteTests : ComponentTestBase
{
    private readonly ComponentRegistry _registry = new();

    public PaletteTests()
    {
        Services.AddSingleton<IComponentRegistry>(_registry);
    }

    private static IElement Button(IRenderedComponent<Palette> palette, string accessibleName) =>
        palette
            .FindAll(".d12-palette-button")
            .Single(e => e.GetAttribute("aria-label") == accessibleName);

    private static string[] ButtonNames(IRenderedComponent<Palette> palette) =>
        palette
            .FindAll(".d12-palette-button")
            .Select(e => e.GetAttribute("aria-label") ?? "")
            .ToArray();

    private static string[] TabIndexes(IRenderedComponent<Palette> palette) =>
        palette
            .FindAll(".d12-palette-button")
            .Select(e => e.GetAttribute("tabindex") ?? "")
            .ToArray();

    private static string TooltipText(IElement button) =>
        button.ParentElement!.QuerySelector(".d12-tooltip")!.TextContent;

    private static void PressKey(IRenderedComponent<Palette> palette, string key) =>
        palette.Find(".d12-palette").KeyDown(new KeyboardEventArgs { Key = key });

    private void RegisterComponent(
        string key,
        string displayName,
        string accessibleName,
        Glyph? icon = null,
        string? category = null
    )
    {
        _registry.Register(
            new ComponentRegistration(
                Key: key,
                ComponentType: typeof(TestComponentDouble),
                PropsType: typeof(TestProps),
                DisplayName: displayName,
                AccessibleName: accessibleName,
                DefaultProps: new TestProps(),
                Icon: icon,
                Role: "group",
                DefaultSize: null,
                Category: category
            )
        );
    }

    private void RegisterTwoTypes()
    {
        RegisterComponent("rectangle", "Rectangle", "Rectangle");
        RegisterComponent("widget", "Widget", "Widget");
    }

    [Fact]
    public void TheRailIsAVerticalToolbarNamedComponentPalette()
    {
        var palette = Render<Palette>();

        var rail = palette.Find(".d12-palette");
        Assert.Equal("toolbar", rail.GetAttribute("role"));
        Assert.Equal("vertical", rail.GetAttribute("aria-orientation"));
        Assert.Equal("Component palette", rail.GetAttribute("aria-label"));
    }

    [Fact]
    public void EdgeComesFirstWithItsOwnGlyphAndName()
    {
        RegisterComponent("rectangle", "Rectangle", "Rectangle");

        var palette = Render<Palette>();

        var edge = palette.FindAll(".d12-palette-button")[0];
        Assert.Equal("Edge", edge.GetAttribute("aria-label"));
        Assert.Equal("Edge", TooltipText(edge));
        DrawnGlyph.AssertDecorativeIcon(Glyphs.Edge, edge);
        Assert.DoesNotContain("Connector", palette.Markup);
    }

    [Fact]
    public void RegisteredTypesFollowEdgeInRegistrationOrderGroupedByCategory()
    {
        RegisterComponent("rectangle", "Rectangle", "Rectangle", category: "Basic Shapes");
        RegisterComponent("widget", "Widget", "Widget", category: "Widgets");
        RegisterComponent("sticky-note", "Sticky Note", "Sticky note", category: "Basic Shapes");
        RegisterComponent("gadget", "Gadget", "Gadget");

        var palette = Render<Palette>();

        Assert.Equal(
            ["Edge", "Rectangle", "Sticky note", "Widget", "Gadget"],
            ButtonNames(palette)
        );
    }

    [Fact]
    public void ADividerSeparatesEdgeAndEachCategory()
    {
        RegisterComponent("rectangle", "Rectangle", "Rectangle", category: "Basic Shapes");
        RegisterComponent("sticky-note", "Sticky Note", "Sticky note", category: "Basic Shapes");
        RegisterComponent("widget", "Widget", "Widget", category: "Widgets");
        RegisterComponent("gadget", "Gadget", "Gadget");

        var palette = Render<Palette>();

        var sequence = palette
            .Find(".d12-palette")
            .QuerySelectorAll(".d12-palette-divider, .d12-palette-button")
            .Select(e =>
                e.ClassList.Contains("d12-palette-divider")
                    ? "|"
                    : e.GetAttribute("aria-label") ?? ""
            )
            .ToArray();
        Assert.Equal(
            ["Edge", "|", "Rectangle", "Sticky note", "|", "Widget", "|", "Gadget"],
            sequence
        );
        Assert.All(
            palette.FindAll(".d12-palette-divider"),
            divider => Assert.Equal("separator", divider.GetAttribute("role"))
        );
    }

    [Fact]
    public void WithNothingRegisteredTheRailHoldsOnlyEdge()
    {
        var palette = Render<Palette>();

        Assert.Equal(["Edge"], ButtonNames(palette));
        Assert.Empty(palette.FindAll(".d12-palette-divider"));
    }

    [Fact]
    public void TheRailShowsTheFirstEightRegisteredTypesAndEdgeDoesNotCount()
    {
        for (var number = 1; number <= 10; number++)
        {
            RegisterComponent($"type-{number}", $"Type {number}", $"Type {number}");
        }

        var palette = Render<Palette>();

        Assert.Equal(
            ["Edge", .. Enumerable.Range(1, 8).Select(number => $"Type {number}")],
            ButtonNames(palette)
        );
    }

    [Fact]
    public void TheEightTypeLimitCountsInCategoryOrder()
    {
        for (var number = 1; number <= 7; number++)
        {
            RegisterComponent(
                $"shape-{number}",
                $"Shape {number}",
                $"Shape {number}",
                category: "Shapes"
            );
        }
        RegisterComponent("widget", "Widget", "Widget", category: "Widgets");
        RegisterComponent("shape-8", "Shape 8", "Shape 8", category: "Shapes");

        var palette = Render<Palette>();

        Assert.Equal(
            ["Edge", .. Enumerable.Range(1, 8).Select(number => $"Shape {number}")],
            ButtonNames(palette)
        );
    }

    [Fact]
    public void EachButtonIsNamedByItsAccessibleNameAndDrawsItsGlyphDecoratively()
    {
        var widgetGlyph = new Glyph(new GlyphPart("M3 3h10v10h-10z"));
        RegisterComponent("rectangle", "Rectangle", "Rectangle", icon: Glyphs.Rectangle);
        RegisterComponent("widget", "Widget", "A host widget", icon: widgetGlyph);

        var palette = Render<Palette>();

        DrawnGlyph.AssertDecorativeIcon(Glyphs.Rectangle, Button(palette, "Rectangle"));
        DrawnGlyph.AssertDecorativeIcon(widgetGlyph, Button(palette, "A host widget"));
    }

    [Fact]
    public void ATypeWithNoIconDrawsTheComponentGlyph()
    {
        RegisterComponent("widget", "Widget", "Widget");

        var palette = Render<Palette>();

        DrawnGlyph.AssertDecorativeIcon(Glyphs.Component, Button(palette, "Widget"));
    }

    [Fact]
    public void EachButtonShowsItsDisplayNameInALibraryTooltipNotTheNativeTitle()
    {
        RegisterComponent("sticky-note", "Sticky Note", "A yellow sticky note");

        var palette = Render<Palette>();

        Assert.Equal("Sticky Note", TooltipText(Button(palette, "A yellow sticky note")));
        Assert.Empty(palette.FindAll("[title]"));
    }

    [Fact]
    public void TheTooltipSitsToTheRightOfTheRail()
    {
        RegisterComponent("rectangle", "Rectangle", "Rectangle");

        var palette = Render<Palette>();

        Assert.All(
            palette.FindAll(".d12-tooltip"),
            tooltip => Assert.True(tooltip.ClassList.Contains("d12-tooltip-right"))
        );
    }

    [Fact]
    public void ThePaletteCarriesTheIconRulesOnceInItsOwnStyleBlock()
    {
        RegisterTwoTypes();

        var palette = Render<Palette>();

        var style = Assert.Single(palette.FindAll("style"));
        Assert.Contains(".d12-icon {", style.InnerHtml);
    }

    [Fact]
    public void OnlyTheFirstButtonIsATabStop()
    {
        RegisterTwoTypes();

        var palette = Render<Palette>();

        Assert.Equal(["0", "-1", "-1"], TabIndexes(palette));
    }

    [Theory]
    [InlineData("ArrowDown", new[] { "-1", "0", "-1" })]
    [InlineData("ArrowUp", new[] { "-1", "-1", "0" })]
    [InlineData("End", new[] { "-1", "-1", "0" })]
    public void ArrowAndEndKeysMoveTheTabStopAndFocus(string key, string[] expectedTabIndexes)
    {
        RegisterTwoTypes();
        var palette = Render<Palette>();

        PressKey(palette, key);

        Assert.Equal(expectedTabIndexes, TabIndexes(palette));
        JSInterop.VerifyFocusAsyncInvoke();
    }

    [Fact]
    public void ArrowDownFromTheLastButtonWrapsToEdge()
    {
        RegisterTwoTypes();
        var palette = Render<Palette>();

        PressKey(palette, "End");
        PressKey(palette, "ArrowDown");

        Assert.Equal(["0", "-1", "-1"], TabIndexes(palette));
    }

    [Fact]
    public void HomeReturnsTheTabStopToEdge()
    {
        RegisterTwoTypes();
        var palette = Render<Palette>();

        PressKey(palette, "ArrowDown");
        PressKey(palette, "Home");

        Assert.Equal(["0", "-1", "-1"], TabIndexes(palette));
    }

    [Fact]
    public void FocusingAButtonMakesItTheTabStop()
    {
        RegisterTwoTypes();
        var palette = Render<Palette>();

        Button(palette, "Widget").Focus();

        Assert.Equal(["-1", "-1", "0"], TabIndexes(palette));
    }

    [Fact]
    public void OtherKeysLeaveTheTabStopAlone()
    {
        RegisterTwoTypes();
        var palette = Render<Palette>();

        PressKey(palette, "ArrowRight");

        Assert.Equal(["0", "-1", "-1"], TabIndexes(palette));
    }

    [Fact]
    public void RendersStandaloneWithoutRequiringAParentDiagramCanvas()
    {
        RegisterComponent("widget", "Widget", "Widget");

        var palette = Render<Palette>();

        Assert.NotNull(palette.Find(".d12-palette"));
    }

    [Fact]
    public void EveryButtonIsDraggable()
    {
        RegisterComponent("rectangle", "Rectangle", "Rectangle");

        var palette = Render<Palette>();

        Assert.All(
            palette.FindAll(".d12-palette-button"),
            button => Assert.Equal("true", button.GetAttribute("draggable"))
        );
    }

    [Fact]
    public void DragStartOnAnEntryBeginsAPaletteDragOnTheWiredCanvas()
    {
        RegisterComponent("rectangle", "Rectangle", "Rectangle");
        SetupDiagramCanvasJsModule();

        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var palette = Render<Palette>(parameters => parameters.Add(p => p.Canvas, canvas.Instance));

        Button(palette, "Rectangle").DragStart(new DragEventArgs());
        canvas.Find(".diagram-canvas").Drop(new DragEventArgs { ClientX = 300, ClientY = 250 });

        var instance = Assert.Single(board.Components);
        Assert.Equal("rectangle", instance.ComponentTypeKey);
    }

    [Fact]
    public void DragStartIsANoOpWhenNoCanvasIsWired()
    {
        RegisterComponent("rectangle", "Rectangle", "Rectangle");

        var palette = Render<Palette>();

        var exception = Record.Exception(
            () => Button(palette, "Rectangle").DragStart(new DragEventArgs())
        );
        Assert.Null(exception);
    }

    [Fact]
    public void ClickingAnEntryPlacesANewInstanceOnTheWiredCanvas()
    {
        RegisterComponent("rectangle", "Rectangle", "Rectangle");
        SetupDiagramCanvasJsModule();

        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var palette = Render<Palette>(parameters => parameters.Add(p => p.Canvas, canvas.Instance));

        Button(palette, "Rectangle").Click();

        var instance = Assert.Single(board.Components);
        Assert.Equal("rectangle", instance.ComponentTypeKey);
    }

    [Fact]
    public void ClickingAnEntrySelectsTheNewlyPlacedInstance()
    {
        RegisterComponent("rectangle", "Rectangle", "Rectangle");
        SetupDiagramCanvasJsModule();

        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var palette = Render<Palette>(parameters => parameters.Add(p => p.Canvas, canvas.Instance));

        Button(palette, "Rectangle").Click();

        Assert.Equal("true", canvas.Find(".component-container").GetAttribute("aria-selected"));
    }

    [Fact]
    public void ClickIsANoOpWhenNoCanvasIsWired()
    {
        RegisterComponent("rectangle", "Rectangle", "Rectangle");

        var palette = Render<Palette>();

        var exception = Record.Exception(() => Button(palette, "Rectangle").Click());
        Assert.Null(exception);
    }

    [Fact]
    public void DragStartOnTheEdgeButtonBeginsAnEdgeDragOnTheWiredCanvas()
    {
        SetupDiagramCanvasJsModule();

        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var palette = Render<Palette>(parameters => parameters.Add(p => p.Canvas, canvas.Instance));

        Button(palette, "Edge").DragStart(new DragEventArgs());
        canvas.Find(".diagram-canvas").Drop(new DragEventArgs { ClientX = 300, ClientY = 250 });

        Assert.Empty(board.Components);
        Assert.Single(board.Edges);
    }

    [Fact]
    public void ClickingTheEdgeButtonPlacesAFloatingEdgeOnTheWiredCanvas()
    {
        SetupDiagramCanvasJsModule();

        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var palette = Render<Palette>(parameters => parameters.Add(p => p.Canvas, canvas.Instance));

        Button(palette, "Edge").Click();

        Assert.Empty(board.Components);
        Assert.Single(board.Edges);
    }
}
