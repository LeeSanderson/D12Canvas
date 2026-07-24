using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddD12CanvasRegistersAResolvableComponentRegistry()
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

        var registration = registry.Resolve("widget");
        Assert.Equal("Widget", registration.DisplayName);
    }

    [Fact]
    public void CallingAddD12CanvasMoreThanOnceAccumulatesRegistrationsFromEachCall()
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
        services.AddD12Canvas(options =>
            options.RegisterComponent<TestComponentDouble, TestProps>(
                "gadget",
                builder =>
                {
                    builder.DisplayName = "Gadget";
                    builder.AccessibleName = "Gadget";
                    builder.DefaultProps = new TestProps();
                }
            )
        );

        var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<IComponentRegistry>();

        Assert.Equal("Widget", registry.Resolve("widget").DisplayName);
        Assert.Equal("Gadget", registry.Resolve("gadget").DisplayName);
    }
}
