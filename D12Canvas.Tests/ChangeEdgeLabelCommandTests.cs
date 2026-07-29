using D12Canvas.History;
using D12Canvas.Model;
using Xunit;

namespace D12Canvas.Tests;

public class ChangeEdgeLabelCommandTests
{
    private static Edge NewEdge() =>
        new(
            new PortEndpoint(Guid.NewGuid(), PortId.Right),
            new PortEndpoint(Guid.NewGuid(), PortId.Left)
        );

    private static ComponentInstance NewLabel(string text = "") =>
        new("text", text, new Bounds(0, 0, 80, 24));

    [Fact]
    public void ApplyAddsALabelFromNoneToTheAfterSnapshot()
    {
        var edge = NewEdge();
        var label = NewLabel();
        var command = new ChangeEdgeLabelCommand(edge, before: null, after: label);

        command.Apply();

        Assert.Same(label, edge.Label);
    }

    [Fact]
    public void UndoRemovesTheLabelBackToTheBeforeSnapshot()
    {
        var edge = NewEdge();
        var label = NewLabel();
        var command = new ChangeEdgeLabelCommand(edge, before: null, after: label);
        command.Apply();

        command.Undo();

        Assert.Null(edge.Label);
    }

    [Fact]
    public void RedoReappliesTheAfterSnapshot()
    {
        var edge = NewEdge();
        var label = NewLabel();
        var command = new ChangeEdgeLabelCommand(edge, before: null, after: label);
        command.Apply();
        command.Undo();

        command.Apply(); // redo

        Assert.Same(label, edge.Label);
    }

    [Fact]
    public void ChangingOneEdgesLabelNeverAffectsAnotherEdge()
    {
        var edge = NewEdge();
        var other = NewEdge();
        var command = new ChangeEdgeLabelCommand(edge, before: null, after: NewLabel());

        command.Apply();

        Assert.Null(other.Label);
    }

    [Fact]
    public void ApplyCanRemoveAnExistingLabelByGoingToNull()
    {
        var edge = NewEdge();
        var existing = NewLabel("Existing");
        edge.Label = existing;
        var command = new ChangeEdgeLabelCommand(edge, before: existing, after: null);

        command.Apply();

        Assert.Null(edge.Label);
    }

    [Fact]
    public void UndoRestoresARemovedLabel()
    {
        var edge = NewEdge();
        var existing = NewLabel("Existing");
        edge.Label = existing;
        var command = new ChangeEdgeLabelCommand(edge, before: existing, after: null);
        command.Apply();

        command.Undo();

        Assert.Same(existing, edge.Label);
    }
}
