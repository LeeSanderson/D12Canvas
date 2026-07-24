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
}
