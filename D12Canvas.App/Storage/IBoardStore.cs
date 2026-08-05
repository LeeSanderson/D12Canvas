using D12Canvas.Model;

namespace D12Canvas.App.Storage;

public interface IBoardStore
{
    Task<IReadOnlyList<BoardSummary>> ListAsync();
    Task<SavedBoard> CreateAsync(string name);
    Task<SavedBoard> LoadAsync(Guid id);
    Task SaveAsync(Guid id, Board board);
    Task RenameAsync(Guid id, string name);
    Task DeleteAsync(Guid id);
}

public sealed record BoardSummary(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

public sealed record SavedBoard(Guid Id, string Name, Board Board);
