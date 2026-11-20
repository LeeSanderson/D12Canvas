using D12Canvas.Persistence;

namespace D12Canvas;

public sealed class PasteWarningsEventArgs(IReadOnlyList<BoardDeserializeWarning> warnings)
    : EventArgs
{
    public IReadOnlyList<BoardDeserializeWarning> Warnings { get; } = warnings;
}
