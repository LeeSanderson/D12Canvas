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

    // Finds one palette entry button by its aria-label - every test past ticket 54 needs to scope
    // past the always-present built-in Connector entry rather than assume there's only one button
    // on the page.
    private static IElement EntryButton(IRenderedComponent<Palette> palette, string ariaLabel) =>
        palette
            .FindAll(".d12-palette-entry-button")
            .Single(e => e.GetAttribute("aria-label") == ariaLabel);

    private void RegisterComponent(
        string key,
        string displayName,
        string accessibleName,
        string? icon = null,
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

    [Fact]
    public void RendersEveryRegisteredTypeWithItsDisplayNameAndIcon()
    {
        RegisterComponent("rectangle", "Rectangle", "Rectangle", icon: "▭");
        RegisterComponent("sticky-note", "Sticky Note", "Sticky note", icon: "🗒");

        var palette = Render<Palette>();

        // The built-in Connector entry (ticket 54) always renders alongside registered types -
        // excluded here since this test is only about registrations.
        var entries = palette
            .FindAll(".d12-palette-entry-button")
            .Where(e => e.GetAttribute("aria-label") != "Connector")
            .ToList();
        Assert.Equal(2, entries.Count);
        Assert.Contains(
            entries,
            e =>
                e.QuerySelector(".d12-palette-entry-name")!.TextContent == "Rectangle"
                && e.QuerySelector(".d12-palette-entry-icon")!.TextContent == "▭"
        );
        Assert.Contains(
            entries,
            e =>
                e.QuerySelector(".d12-palette-entry-name")!.TextContent == "Sticky Note"
                && e.QuerySelector(".d12-palette-entry-icon")!.TextContent == "🗒"
        );
    }

    [Fact]
    public void OmitsTheIconElementWhenNoIconIsRegistered()
    {
        RegisterComponent("rectangle", "Rectangle", "Rectangle");

        var palette = Render<Palette>();

        Assert.Null(EntryButton(palette, "Rectangle").QuerySelector(".d12-palette-entry-icon"));
    }

    [Fact]
    public void GroupsEntriesUnderTheirRegisteredCategoryHeading()
    {
        RegisterComponent("rectangle", "Rectangle", "Rectangle", category: "Basic Shapes");
        RegisterComponent("sticky-note", "Sticky Note", "Sticky note", category: "Basic Shapes");
        RegisterComponent("connector", "Connector", "Connector", category: "Connectors");

        var palette = Render<Palette>();

        var categories = palette.FindAll(".d12-palette-category");
        Assert.Equal(2, categories.Count);

        var basicShapes = categories.Single(c =>
            c.QuerySelector(".d12-palette-category-title")!.TextContent == "Basic Shapes"
        );
        Assert.Equal(
            ["Rectangle", "Sticky Note"],
            basicShapes
                .QuerySelectorAll(".d12-palette-entry-name")
                .Select(e => e.TextContent)
                .ToArray()
        );

        var connectors = categories.Single(c =>
            c.QuerySelector(".d12-palette-category-title")!.TextContent == "Connectors"
        );
        Assert.Equal(
            ["Connector"],
            connectors
                .QuerySelectorAll(".d12-palette-entry-name")
                .Select(e => e.TextContent)
                .ToArray()
        );
    }

    [Fact]
    public void GroupsEntriesWithNoCategoryUnderAnUncategorizedHeading()
    {
        RegisterComponent("widget", "Widget", "Widget");

        var palette = Render<Palette>();

        var category = palette.Find(".d12-palette-category-title");
        Assert.Equal("Uncategorized", category.TextContent);
    }

    [Fact]
    public void PaletteEntriesCarryTheirRegisteredAccessibleNameAsAriaLabel()
    {
        RegisterComponent("sticky-note", "Sticky Note", "A yellow sticky note");

        var palette = Render<Palette>();

        Assert.Equal(
            "A yellow sticky note",
            EntryButton(palette, "A yellow sticky note").GetAttribute("aria-label")
        );
    }

    [Fact]
    public void RendersStandaloneWithoutRequiringAParentDiagramCanvas()
    {
        RegisterComponent("widget", "Widget", "Widget");

        var palette = Render<Palette>();

        Assert.NotNull(palette.Find(".d12-palette"));
    }

    [Fact]
    public void RendersNoCategoriesWhenNothingIsRegistered()
    {
        var palette = Render<Palette>();

        Assert.Empty(palette.FindAll(".d12-palette-category"));
    }

    [Fact]
    public void EntryButtonsAreDraggable()
    {
        RegisterComponent("rectangle", "Rectangle", "Rectangle");

        var palette = Render<Palette>();

        Assert.Equal("true", EntryButton(palette, "Rectangle").GetAttribute("draggable"));
    }

    [Fact]
    public void DragStartOnAnEntryBeginsAPaletteDragOnTheWiredCanvas()
    {
        RegisterComponent("rectangle", "Rectangle", "Rectangle");
        SetupDiagramCanvasJsModule();
        JSInterop.SetupModule("./_content/D12Canvas/ComponentContainer.razor.js");

        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var palette = Render<Palette>(parameters => parameters.Add(p => p.Canvas, canvas.Instance));

        EntryButton(palette, "Rectangle").DragStart(new DragEventArgs());
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
            () =>
                palette.FindAll(".d12-palette-entry-button").First().DragStart(new DragEventArgs())
        );
        Assert.Null(exception);
    }

    [Fact]
    public void ClickingAnEntryPlacesANewInstanceOnTheWiredCanvas()
    {
        RegisterComponent("rectangle", "Rectangle", "Rectangle");
        SetupDiagramCanvasJsModule();
        JSInterop.SetupModule("./_content/D12Canvas/ComponentContainer.razor.js");

        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var palette = Render<Palette>(parameters => parameters.Add(p => p.Canvas, canvas.Instance));

        EntryButton(palette, "Rectangle").Click();

        var instance = Assert.Single(board.Components);
        Assert.Equal("rectangle", instance.ComponentTypeKey);
    }

    [Fact]
    public void ClickIsANoOpWhenNoCanvasIsWired()
    {
        RegisterComponent("rectangle", "Rectangle", "Rectangle");

        var palette = Render<Palette>();

        var exception = Record.Exception(
            () => palette.FindAll(".d12-palette-entry-button").First().Click()
        );
        Assert.Null(exception);
    }

    // Ticket 54/ADR 0009: the built-in "Connector" entry - not a registry registration - appears
    // regardless of what (if anything) is registered, and is never grouped into a
    // .d12-palette-category heading (see GroupsEntriesUnderTheirRegisteredCategoryHeading /
    // RendersNoCategoriesWhenNothingIsRegistered above, both unaffected by its presence).
    [Fact]
    public void TheConnectorEntryAppearsEvenWhenNothingIsRegistered()
    {
        var palette = Render<Palette>();

        Assert.Equal("true", EntryButton(palette, "Connector").GetAttribute("draggable"));
        Assert.Empty(palette.FindAll(".d12-palette-category"));
    }

    [Fact]
    public void DragStartOnTheConnectorEntryBeginsAConnectorPaletteDragOnTheWiredCanvas()
    {
        SetupDiagramCanvasJsModule();
        JSInterop.SetupModule("./_content/D12Canvas/ComponentContainer.razor.js");

        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var palette = Render<Palette>(parameters => parameters.Add(p => p.Canvas, canvas.Instance));

        EntryButton(palette, "Connector").DragStart(new DragEventArgs());
        canvas.Find(".diagram-canvas").Drop(new DragEventArgs { ClientX = 300, ClientY = 250 });

        Assert.Empty(board.Components);
        Assert.Single(board.Edges);
    }

    [Fact]
    public void ClickingTheConnectorEntryPlacesAFloatingEdgeOnTheWiredCanvas()
    {
        SetupDiagramCanvasJsModule();
        JSInterop.SetupModule("./_content/D12Canvas/ComponentContainer.razor.js");

        var board = new Board();
        var canvas = Render<DiagramCanvas>(parameters => parameters.Add(p => p.Board, board));
        var palette = Render<Palette>(parameters => parameters.Add(p => p.Canvas, canvas.Instance));

        EntryButton(palette, "Connector").Click();

        Assert.Empty(board.Components);
        Assert.Single(board.Edges);
    }
}
