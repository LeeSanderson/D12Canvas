using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

public class ComponentInstanceTests
{
    [Fact]
    public void EachInstanceIsAssignedAUniqueGuidIdentityAtCreation()
    {
        var first = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(0, 0, 100, 100)
        );
        var second = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(0, 0, 100, 100)
        );

        Assert.NotEqual(Guid.Empty, first.Id);
        Assert.NotEqual(Guid.Empty, second.Id);
        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void StoresComponentTypeKeyPropsBoundsAndZIndexAsProvided()
    {
        var props = new TestProps("hello");
        var bounds = new Bounds(10, 20, 100, 50);

        var instance = new ComponentInstance("sticky-note", props, bounds, zIndex: 3);

        Assert.Equal("sticky-note", instance.ComponentTypeKey);
        Assert.Same(props, instance.Props);
        Assert.Equal(bounds, instance.Bounds);
        Assert.Equal(3, instance.ZIndex);
    }

    [Fact]
    public void DefaultsZIndexToZeroWhenNotSpecified()
    {
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(0, 0, 100, 100)
        );

        Assert.Equal(0, instance.ZIndex);
    }

    [Fact]
    public void BoundsCanBeMutatedInPlaceAfterCreation()
    {
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(0, 0, 100, 100)
        );

        instance.Bounds = new Bounds(50, 60, 200, 150);

        Assert.Equal(new Bounds(50, 60, 200, 150), instance.Bounds);
    }

    // Ticket 55/ADR 0005: custom ports are nothing a component type's developer declares at
    // registration - a fresh instance starts with none, regardless of its type.
    [Fact]
    public void DefaultsToNoCustomPortsWhenNoneAreProvided()
    {
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(0, 0, 100, 100)
        );

        Assert.Empty(instance.CustomPorts);
    }

    [Fact]
    public void CustomPortsCanBeAddedInPlaceAfterCreation()
    {
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(0, 0, 100, 100)
        );
        var port = new PortDef(0.25, 0);

        instance.CustomPorts.Add(port);

        Assert.Equal(new[] { port }, instance.CustomPorts);
    }

    // Ticket 51-style persistence round-trip precedent: FromComponentEnvelope reconstructs an
    // instance with its custom ports already populated at construction, not added afterwards.
    [Fact]
    public void CustomPortsProvidedAtConstructionAreCopiedNotSharedByReference()
    {
        var seedPorts = new List<PortDef> { new(0.25, 0) };

        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(0, 0, 100, 100),
            customPorts: seedPorts
        );
        seedPorts.Add(new PortDef(0.75, 1));

        Assert.Single(instance.CustomPorts);
    }
}
