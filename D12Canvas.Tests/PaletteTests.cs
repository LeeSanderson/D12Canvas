using System.Linq;
using Bunit;
using D12Canvas.Registration;
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

        var entries = palette.FindAll(".d12-palette-entry-button");
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

        var entry = palette.Find(".d12-palette-entry-button");
        Assert.Null(entry.QuerySelector(".d12-palette-entry-icon"));
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

        var entry = palette.Find(".d12-palette-entry-button");
        Assert.Equal("A yellow sticky note", entry.GetAttribute("aria-label"));
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
}
