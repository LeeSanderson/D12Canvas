using System.Linq;
using D12Canvas.BuiltIns;
using D12Canvas.Panel;
using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

public class BuiltInComponentsTests
{
    [Fact]
    public void AddD12CanvasRegistersRectangleWithoutAnyHostRegistration()
    {
        var services = new ServiceCollection();

        services.AddD12Canvas(_ => { });

        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IComponentRegistry>();

        var registration = registry.Resolve("rectangle");
        Assert.Equal(typeof(Rectangle), registration.ComponentType);
        Assert.Equal(typeof(RectangleProps), registration.PropsType);
        Assert.Equal("Rectangle", registration.DisplayName);
        Assert.Equal("Rectangle", registration.AccessibleName);
        Assert.Equal("Basic Shapes", registration.Category);
        Assert.Equal(new ComponentSize(160, 100), registration.DefaultSize);
        Assert.Equal(new RectangleProps("#FFFFFF", "#333333", 2), registration.DefaultProps);
        Assert.False(string.IsNullOrEmpty(registration.Icon));
    }

    [Fact]
    public void AddD12CanvasRegistersStickyNoteWithoutAnyHostRegistration()
    {
        var services = new ServiceCollection();

        services.AddD12Canvas(_ => { });

        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IComponentRegistry>();

        var registration = registry.Resolve("sticky-note");
        Assert.Equal(typeof(StickyNote), registration.ComponentType);
        Assert.Equal(typeof(StickyNoteProps), registration.PropsType);
        Assert.Equal("Sticky Note", registration.DisplayName);
        Assert.Equal("Sticky Note", registration.AccessibleName);
        Assert.Equal("Basic Shapes", registration.Category);
        Assert.Equal(new ComponentSize(200, 200), registration.DefaultSize);
        Assert.Equal(new StickyNoteProps("", "#FFEB3B", "#000000", 14), registration.DefaultProps);
        Assert.False(string.IsNullOrEmpty(registration.Icon));
    }

    [Fact]
    public void AddD12CanvasRegistersTextWithoutAnyHostRegistration()
    {
        var services = new ServiceCollection();

        services.AddD12Canvas(_ => { });

        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IComponentRegistry>();

        var registration = registry.Resolve("text");
        Assert.Equal(typeof(Text), registration.ComponentType);
        Assert.Equal(typeof(TextProps), registration.PropsType);
        Assert.Equal("Text", registration.DisplayName);
        Assert.Equal("Text", registration.AccessibleName);
        Assert.Equal("Basic Shapes", registration.Category);
        Assert.Equal(new ComponentSize(200, 40), registration.DefaultSize);
        Assert.Equal(new TextProps("", "#000000", 16, "normal", "left"), registration.DefaultProps);
        Assert.False(string.IsNullOrEmpty(registration.Icon));
    }

    [Fact]
    public void AddD12CanvasRegistersImageWithoutAnyHostRegistration()
    {
        var services = new ServiceCollection();

        services.AddD12Canvas(_ => { });

        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IComponentRegistry>();

        var registration = registry.Resolve("image");
        Assert.Equal(typeof(Image), registration.ComponentType);
        Assert.Equal(typeof(ImageProps), registration.PropsType);
        Assert.Equal("Image", registration.DisplayName);
        Assert.Equal("Image", registration.AccessibleName);
        Assert.Equal("Basic Shapes", registration.Category);
        Assert.Equal(new ComponentSize(240, 180), registration.DefaultSize);
        Assert.Equal(new ImageProps("", "", "cover"), registration.DefaultProps);
        Assert.False(string.IsNullOrEmpty(registration.Icon));
    }

    // Ticket 57: every visual/style prop the four built-ins declare (per ticket 10) is
    // panel-editable through one of the full built-in EditorKind set - Text content fields
    // (StickyNote/Text's Text, Image's Url which is reserved for the ticket 58 Custom escape
    // hatch) stay deliberately excluded.
    [Fact]
    public void RectangleEditablePropertiesCoverFillColorStrokeColorAndStrokeWidth()
    {
        var registration = RegisterAndResolve("rectangle");

        Assert.Equivalent(
            new[]
            {
                ("FillColor", EditorKind.Color),
                ("StrokeColor", EditorKind.Color),
                ("StrokeWidth", EditorKind.Number),
            },
            registration.EditableProperties!.Select(p => (p.Property.Name, p.Kind))
        );
    }

    [Fact]
    public void StickyNoteEditablePropertiesCoverColorTextColorAndFontSizeButNotText()
    {
        var registration = RegisterAndResolve("sticky-note");

        Assert.Equivalent(
            new[]
            {
                ("Color", EditorKind.Color),
                ("TextColor", EditorKind.Color),
                ("FontSize", EditorKind.Number),
            },
            registration.EditableProperties!.Select(p => (p.Property.Name, p.Kind))
        );
    }

    [Fact]
    public void TextEditablePropertiesCoverColorFontSizeFontWeightAndTextAlignButNotText()
    {
        var registration = RegisterAndResolve("text");

        Assert.Equivalent(
            new[]
            {
                ("Color", EditorKind.Color),
                ("FontSize", EditorKind.Number),
                ("FontWeight", EditorKind.Dropdown),
                ("TextAlign", EditorKind.Dropdown),
            },
            registration.EditableProperties!.Select(p => (p.Property.Name, p.Kind))
        );
        Assert.Equal(
            ["normal", "bold"],
            registration.EditableProperties!.Single(p => p.Property.Name == "FontWeight").Options
        );
        Assert.Equal(
            ["left", "center", "right"],
            registration.EditableProperties!.Single(p => p.Property.Name == "TextAlign").Options
        );
    }

    [Fact]
    public void ImageEditablePropertiesCoverAltTextAndFitButNotUrl()
    {
        var registration = RegisterAndResolve("image");

        Assert.Equivalent(
            new[] { ("AltText", EditorKind.Text), ("Fit", EditorKind.Dropdown) },
            registration.EditableProperties!.Select(p => (p.Property.Name, p.Kind))
        );
        Assert.Equal(
            ["cover", "contain", "fill"],
            registration.EditableProperties!.Single(p => p.Property.Name == "Fit").Options
        );
    }

    private static ComponentRegistration RegisterAndResolve(string key)
    {
        var services = new ServiceCollection();
        services.AddD12Canvas(_ => { });
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IComponentRegistry>().Resolve(key);
    }

    [Fact]
    public void CallingAddD12CanvasMoreThanOnceDoesNotDuplicateTheRectangleRegistration()
    {
        var services = new ServiceCollection();

        services.AddD12Canvas(_ => { });
        var exception = Record.Exception(() => services.AddD12Canvas(_ => { }));

        Assert.Null(exception);
    }

    [Fact]
    public void HostRegisteredComponentsPrecedeBuiltInsInPaletteOrder()
    {
        var services = new ServiceCollection();

        services.AddD12Canvas(options =>
            options.RegisterComponent<TestComponentDouble, TestProps>(
                "widget",
                builder =>
                {
                    builder.DisplayName = "Widget";
                    builder.AccessibleName = "Widget";
                    builder.DefaultProps = new TestProps();
                }
            )
        );

        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IComponentRegistry>();

        Assert.Equal(
            ["widget", "rectangle", "sticky-note", "text", "image"],
            registry.All.Select(r => r.Key)
        );
    }
}
