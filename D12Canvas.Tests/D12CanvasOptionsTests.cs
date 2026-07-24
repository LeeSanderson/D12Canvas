using D12Canvas.Registration;
using Xunit;

namespace D12Canvas.Tests;

public class D12CanvasOptionsTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RegisterComponentWithAnEmptyOrWhitespaceKeyThrows(string key)
    {
        var options = new D12CanvasOptions();

        Assert.Throws<ArgumentException>(
            () =>
                options.RegisterComponent<TestComponentDouble, TestProps>(
                    key,
                    builder =>
                    {
                        builder.DisplayName = "Widget";
                        builder.AccessibleName = "Widget";
                        builder.DefaultProps = new TestProps();
                    }
                )
        );
    }

    [Fact]
    public void RegisterComponentWithoutDisplayNameThrowsNamingTheMissingField()
    {
        var options = new D12CanvasOptions();

        var exception = Assert.Throws<ComponentRegistrationException>(
            () =>
                options.RegisterComponent<TestComponentDouble, TestProps>(
                    "widget",
                    builder =>
                    {
                        builder.AccessibleName = "Widget";
                        builder.DefaultProps = new TestProps();
                    }
                )
        );

        Assert.Equal("widget", exception.Key);
        Assert.Equal(
            nameof(ComponentRegistrationBuilder<TestProps>.DisplayName),
            exception.MissingField
        );
    }

    [Fact]
    public void RegisterComponentWithoutAccessibleNameThrowsNamingTheMissingField()
    {
        var options = new D12CanvasOptions();

        var exception = Assert.Throws<ComponentRegistrationException>(
            () =>
                options.RegisterComponent<TestComponentDouble, TestProps>(
                    "widget",
                    builder =>
                    {
                        builder.DisplayName = "Widget";
                        builder.DefaultProps = new TestProps();
                    }
                )
        );

        Assert.Equal("widget", exception.Key);
        Assert.Equal(
            nameof(ComponentRegistrationBuilder<TestProps>.AccessibleName),
            exception.MissingField
        );
    }

    [Fact]
    public void RegisterComponentWithoutDefaultPropsThrowsNamingTheMissingField()
    {
        var options = new D12CanvasOptions();

        var exception = Assert.Throws<ComponentRegistrationException>(
            () =>
                options.RegisterComponent<TestComponentDouble, TestProps>(
                    "widget",
                    builder =>
                    {
                        builder.DisplayName = "Widget";
                        builder.AccessibleName = "Widget";
                    }
                )
        );

        Assert.Equal("widget", exception.Key);
        Assert.Equal(
            nameof(ComponentRegistrationBuilder<TestProps>.DefaultProps),
            exception.MissingField
        );
    }

    [Fact]
    public void RegisterComponentDefaultsRoleToGroupWhenNotSpecified()
    {
        var options = new D12CanvasOptions();

        options.RegisterComponent<TestComponentDouble, TestProps>(
            "widget",
            builder =>
            {
                builder.DisplayName = "Widget";
                builder.AccessibleName = "Widget";
                builder.DefaultProps = new TestProps();
            }
        );

        var registration = options.Registry.Resolve("widget");
        Assert.Equal("group", registration.Role);
    }

    [Fact]
    public void RegisterComponentStoresOptionalMetadataWhenSpecified()
    {
        var options = new D12CanvasOptions();
        var defaultProps = new TestProps("hello");

        options.RegisterComponent<TestComponentDouble, TestProps>(
            "widget",
            builder =>
            {
                builder.DisplayName = "Widget";
                builder.AccessibleName = "Widget";
                builder.DefaultProps = defaultProps;
                builder.Icon = "widget-icon";
                builder.Role = "img";
                builder.DefaultSize = new ComponentSize(200, 150);
                builder.Category = "Basic Shapes";
            }
        );

        var registration = options.Registry.Resolve("widget");
        Assert.Equal("widget-icon", registration.Icon);
        Assert.Equal("img", registration.Role);
        Assert.Equal(new ComponentSize(200, 150), registration.DefaultSize);
        Assert.Equal("Basic Shapes", registration.Category);
        Assert.Same(defaultProps, registration.DefaultProps);
    }

    [Fact]
    public void RegisterComponentKeyIsDecoupledFromTheClrTypeName()
    {
        var options = new D12CanvasOptions();

        options.RegisterComponent<TestComponentDouble, TestProps>(
            "totally-unrelated-key",
            builder =>
            {
                builder.DisplayName = "Widget";
                builder.AccessibleName = "Widget";
                builder.DefaultProps = new TestProps();
            }
        );

        var registration = options.Registry.Resolve("totally-unrelated-key");
        Assert.Equal(typeof(TestComponentDouble), registration.ComponentType);
    }
}
