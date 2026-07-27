using System.Linq;
using D12Canvas.BuiltIns;
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
