using D12Canvas.Persistence;

namespace D12Canvas;

// One warning per entity a paste left out or could not fully restore, in the same shape a tolerant
// board load reports.
public sealed class PasteWarningsEventArgs(IReadOnlyList<BoardDeserializeWarning> warnings)
    : EventArgs
{
    public IReadOnlyList<BoardDeserializeWarning> Warnings { get; } = warnings;
}
