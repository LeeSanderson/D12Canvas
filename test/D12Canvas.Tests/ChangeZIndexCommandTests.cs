using D12Canvas.History;
using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

public class ChangeZIndexCommandTests
{
    [Fact]
    public void ApplySetsTheInstancesZIndexToAfter()
    {
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(0, 0, 50, 50),
            zIndex: 3
        );
        var command = new ChangeZIndexCommand(instance, before: 3, after: 7);

        command.Apply();

        Assert.Equal(7, instance.ZIndex);
    }

    [Fact]
    public void UndoRestoresTheInstancesZIndexToBefore()
    {
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(0, 0, 50, 50),
            zIndex: 7
        );
        var command = new ChangeZIndexCommand(instance, before: 3, after: 7);

        command.Undo();

        Assert.Equal(3, instance.ZIndex);
    }
}
