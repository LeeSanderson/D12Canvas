using D12Canvas.History;
using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

public class MutateEntityCommandTests
{
    [Fact]
    public void ApplySetsTheInstancesPropsToAfter()
    {
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps("before"),
            new Bounds(0, 0, 50, 50)
        );
        var command = new MutateEntityCommand(
            instance,
            new TestProps("before"),
            new TestProps("after")
        );

        command.Apply();

        Assert.Equal("after", ((TestProps)instance.Props).Text);
    }

    [Fact]
    public void UndoRestoresTheInstancesPropsToBefore()
    {
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps("after"),
            new Bounds(0, 0, 50, 50)
        );
        var command = new MutateEntityCommand(
            instance,
            new TestProps("before"),
            new TestProps("after")
        );

        command.Undo();

        Assert.Equal("before", ((TestProps)instance.Props).Text);
    }
}
