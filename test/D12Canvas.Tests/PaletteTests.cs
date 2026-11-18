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
            [
                "Edge",
                .. Enumerable.Range(1, 8).Select(number => $"Type {number}"),
                "More components",
            ],
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
            [
                "Edge",
                .. Enumerable.Range(1, 8).Select(number => $"Shape {number}"),
                "More components",
            ],
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

    private void RegisterNumberedTypes(int count, string? category = null)
    {
        for (var number = 1; number <= count; number++)
        {
            RegisterComponent(
                $"type-{number}",
                $"Type {number}",
                $"Type {number}",
                category: category
            );
        }
    }

    private static IElement MoreButton(IRenderedComponent<Palette> palette) =>
        palette.Find(".d12-palette-more-button");

    private static IElement OpenFlyout(IRenderedComponent<Palette> palette)
    {
        MoreButton(palette).Click();
        return palette.Find("[role=menu]");
    }

    private static string[] FlyoutRowNames(IRenderedComponent<Palette> palette) =>
        palette
            .FindAll("[role=menu] [role=menuitem]")
            .Select(e => e.GetAttribute("aria-label") ?? "")
            .ToArray();

    [Fact]
    public void WithEightTypesTheRailHasNoMoreButton()
    {
        RegisterNumberedTypes(8);

        var palette = Render<Palette>();

        Assert.Empty(palette.FindAll(".d12-palette-more-button"));
        Assert.DoesNotContain("More components", palette.Markup);
    }

    [Fact]
    public void PastEightTypesADividerAndTheMoreButtonEndTheRail()
    {
        RegisterNumberedTypes(9);

        var palette = Render<Palette>();

        var lastTwo = palette
            .Find(".d12-palette")
            .QuerySelectorAll(".d12-palette-divider, .d12-palette-button")
            .TakeLast(2)
            .ToArray();
        Assert.True(lastTwo[0].ClassList.Contains("d12-palette-divider"));
        Assert.Equal("More components", lastTwo[1].GetAttribute("aria-label"));
    }

    [Fact]
    public void TheMoreButtonIsNamedMoreComponentsAndItsTooltipGivesTheCount()
    {
        RegisterNumberedTypes(22);

        var palette = Render<Palette>();

        var more = MoreButton(palette);
        Assert.Equal("More components", more.GetAttribute("aria-label"));
        Assert.Equal("menu", more.GetAttribute("aria-haspopup"));
        Assert.Equal("false", more.GetAttribute("aria-expanded"));
        Assert.Equal("More components (14)", TooltipText(more));
        DrawnGlyph.AssertDecorativeIcon(Palette.MoreGlyph, more);
    }

    [Fact]
    public void ClickingTheMoreButtonOpensAMenuBesideItAndMarksTheButtonExpanded()
    {
        RegisterNumberedTypes(9);
        var palette = Render<Palette>();

        var menu = OpenFlyout(palette);

        Assert.Equal("More components", menu.GetAttribute("aria-label"));
        Assert.Equal("true", MoreButton(palette).GetAttribute("aria-expanded"));
        Assert.True(menu.ClassList.Contains("d12-palette-flyout"));
        Assert.Same(MoreButton(palette).ParentElement!.ParentElement, menu.ParentElement);
    }

    [Fact]
    public void OpeningTheFlyoutRegistersItWithItsOpener()
    {
        RegisterNumberedTypes(9);
        var palette = Render<Palette>();

        OpenFlyout(palette);

        var registration = Assert.Single(FlyoutModule.Invocations["registerFlyout"]);
        Assert.Equal(3, registration.Arguments.Count);
    }

    [Fact]
    public void ClickingTheMoreButtonAgainClosesTheFlyout()
    {
        RegisterNumberedTypes(9);
        var palette = Render<Palette>();

        OpenFlyout(palette);
        MoreButton(palette).Click();

        Assert.Empty(palette.FindAll("[role=menu]"));
        Assert.Equal("false", MoreButton(palette).GetAttribute("aria-expanded"));
    }

    [Fact]
    public void TheFlyoutHoldsOnlyTheTypesNotOnTheRailGroupedByCategory()
    {
        RegisterComponent("rectangle", "Rectangle", "Rectangle", category: "Basic Shapes");
        for (var number = 1; number <= 7; number++)
        {
            RegisterComponent(
                $"flow-{number}",
                $"Flow {number}",
                $"Flow {number}",
                category: "Flowchart"
            );
        }
        RegisterComponent("callout", "Callout", "Callout", category: "Annotations");
        RegisterComponent("gadget", "Gadget", "Gadget");
        RegisterComponent("flow-8", "Flow 8", "Flow 8", category: "Flowchart");
        var palette = Render<Palette>();

        OpenFlyout(palette);

        Assert.Equal(["Flow 8", "Callout", "Gadget"], FlyoutRowNames(palette));
        var columns = palette.FindAll("[role=menu] > [role=group]");
        Assert.Equal(
            ["Flowchart", "Annotations", "Uncategorized"],
            columns.Select(column => column.GetAttribute("aria-label") ?? "").ToArray()
        );
        Assert.Equal(
            ["Flowchart", "Annotations", "Uncategorized"],
            columns
                .Select(column => column.QuerySelector(".d12-flyout-heading")!.TextContent)
                .ToArray()
        );
        Assert.Equal(
            ["Flow 8"],
            columns[0]
                .QuerySelectorAll("[role=menuitem]")
                .Select(row => row.GetAttribute("aria-label") ?? "")
                .ToArray()
        );
    }

    [Fact]
    public void EachFlyoutRowShowsItsGlyphAndDisplayNameAndIsNamedByItsAccessibleName()
    {
        var widgetGlyph = new Glyph(new GlyphPart("M3 3h10v10h-10z"));
        RegisterNumberedTypes(8);
        RegisterComponent("widget", "Widget", "A host widget", icon: widgetGlyph);
        RegisterComponent("gadget", "Gadget", "Gadget");
        var palette = Render<Palette>();

        OpenFlyout(palette);

        var rows = palette.FindAll("[role=menuitem]");
        Assert.Equal("A host widget", rows[0].GetAttribute("aria-label"));
        Assert.Equal("Widget", rows[0].TextContent.Trim());
        DrawnGlyph.AssertDecorativeIcon(widgetGlyph, rows[0]);
        DrawnGlyph.AssertDecorativeIcon(Glyphs.Component, rows[1]);
        Assert.All(rows, row => Assert.Equal("-1", row.GetAttribute("tabindex")));
        Assert.All(rows, row => Assert.Equal("true", row.GetAttribute("draggable")));
        Assert.Empty(palette.FindAll("[title]"));
    }

    [Fact]
    public void PickingAFlyoutRowAddsTheTypeAndClosesTheFlyout()
    {
        RegisterNumberedTypes(9);
        SetupDiagramCanvasJsModule();
        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var palette = Render<Palette>(parameters => parameters.Add(p => p.Canvas, canvas.Instance));

        OpenFlyout(palette);
        palette.Find("[role=menuitem]").Click();

        var instance = Assert.Single(board.Components);
        Assert.Equal("type-9", instance.ComponentTypeKey);
        Assert.Empty(palette.FindAll("[role=menu]"));
    }

    [Fact]
    public void DraggingAFlyoutRowPlacesItWhereItIsDroppedAndClosesTheFlyout()
    {
        RegisterNumberedTypes(9);
        SetupDiagramCanvasJsModule();
        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var palette = Render<Palette>(parameters => parameters.Add(p => p.Canvas, canvas.Instance));

        OpenFlyout(palette);
        palette.Find("[role=menuitem]").DragStart(new DragEventArgs());
        canvas.Find(".diagram-canvas").Drop(new DragEventArgs { ClientX = 300, ClientY = 250 });
        palette.Find("[role=menuitem]").DragEnd(new DragEventArgs());

        var instance = Assert.Single(board.Components);
        Assert.Equal("type-9", instance.ComponentTypeKey);
        Assert.Empty(palette.FindAll("[role=menu]"));
    }

    [Fact]
    public async Task AClosingRequestFromTheFlyoutClosesIt()
    {
        RegisterNumberedTypes(9);
        var palette = Render<Palette>();
        OpenFlyout(palette);

        await palette.InvokeAsync(() => palette.FindComponent<Flyout>().Instance.RequestClose());

        Assert.Empty(palette.FindAll("[role=menu]"));
        Assert.Equal("false", MoreButton(palette).GetAttribute("aria-expanded"));
    }

    [Fact]
    public void TheMoreButtonIsTheLastStopOnTheRail()
    {
        RegisterNumberedTypes(9);
        var palette = Render<Palette>();

        PressKey(palette, "End");

        Assert.Equal("0", MoreButton(palette).GetAttribute("tabindex"));
        PressKey(palette, "ArrowDown");
        Assert.Equal("0", Button(palette, "Edge").GetAttribute("tabindex"));
    }

    [Fact]
    public void TheRailRegistersItsKeysSoTheyDoNotScrollThePage()
    {
        Render<Palette>();

        var registration = Assert.Single(
            JSInterop.Invocations,
            invocation => invocation.Identifier == "registerRail"
        );
        Assert.Single(registration.Arguments);
    }
}
