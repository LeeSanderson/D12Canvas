using D12Canvas.History;
using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

public class ChangeBoundsCommandTests
{
    [Fact]
    public void ApplySetsTheInstancesBoundsToAfter()
    {
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(0, 0, 50, 50)
        );
        var command = new ChangeBoundsCommand(
            instance,
            new Bounds(0, 0, 50, 50),
            new Bounds(10, 20, 60, 70)
        );

        command.Apply();

        Assert.Equal(new Bounds(10, 20, 60, 70), instance.Bounds);
    }

    [Fact]
    public void UndoRestoresTheInstancesBoundsToBefore()
    {
        var instance = new ComponentInstance(
            "sticky-note",
            new TestProps(),
            new Bounds(10, 20, 60, 70)
        );
        var command = new ChangeBoundsCommand(
            instance,
            new Bounds(0, 0, 50, 50),
            new Bounds(10, 20, 60, 70)
        );

        command.Undo();

        Assert.Equal(new Bounds(0, 0, 50, 50), instance.Bounds);
    }
}
