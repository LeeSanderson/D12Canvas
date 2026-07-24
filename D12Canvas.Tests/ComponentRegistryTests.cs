using D12Canvas.Registration;
using Xunit;

namespace D12Canvas.Tests;

public class ComponentRegistryTests
{
    [Fact]
    public void ResolveReturnsTheRegistrationStoredUnderThatKey()
    {
        var registry = new ComponentRegistry();
        var registration = new ComponentRegistration(
            Key: "sticky-note",
            ComponentType: typeof(TestComponentDouble),
            PropsType: typeof(TestProps),
            DisplayName: "Sticky Note",
            AccessibleName: "Sticky note",
            DefaultProps: new TestProps(),
            Icon: null,
            Role: "group",
            DefaultSize: null,
            Category: null
        );

        registry.Register(registration);

        Assert.Same(registration, registry.Resolve("sticky-note"));
    }

    [Fact]
    public void ResolveUnknownKeyThrowsUnknownComponentKeyExceptionNamingTheKey()
    {
        var registry = new ComponentRegistry();

        var exception = Assert.Throws<UnknownComponentKeyException>(
            () => registry.Resolve("no-such-key")
        );

        Assert.Equal("no-such-key", exception.Key);
    }

    [Fact]
    public void RegisteringADuplicateKeyThrowsDuplicateComponentKeyExceptionNamingTheKey()
    {
        var registry = new ComponentRegistry();
        var firstRegistration = new ComponentRegistration(
            Key: "sticky-note",
            ComponentType: typeof(TestComponentDouble),
            PropsType: typeof(TestProps),
            DisplayName: "Sticky Note",
            AccessibleName: "Sticky note",
            DefaultProps: new TestProps(),
            Icon: null,
            Role: "group",
            DefaultSize: null,
            Category: null
        );
        var secondRegistration = firstRegistration with { DisplayName = "Sticky Note 2" };
        registry.Register(firstRegistration);

        var exception = Assert.Throws<DuplicateComponentKeyException>(
            () => registry.Register(secondRegistration)
        );

        Assert.Equal("sticky-note", exception.Key);
    }
}
