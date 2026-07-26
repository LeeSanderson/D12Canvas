using D12Canvas.Model;

namespace D12Canvas.Persistence;

public sealed record BoardDeserializeWarning(string Entity, string Reason);

public sealed record PartialBoardDeserializeResult(
    Board Board,
    IReadOnlyList<BoardDeserializeWarning> Warnings
);
