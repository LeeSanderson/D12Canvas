using D12Canvas.App.Storage;
using D12Canvas.Model;

namespace D12Canvas.App.Tests;

public sealed class InMemoryBoardStore : IBoardStore
{
    private readonly Dictionary<Guid, StoredBoard> _boards = [];

    public DateTimeOffset Now { get; set; } = new(2026, 1, 15, 9, 30, 0, TimeSpan.Zero);

    public List<BoardSave> Saves { get; } = [];

    public Guid Add(string name, Board board)
    {
        var id = Guid.NewGuid();
        _boards[id] = new StoredBoard(id, name, Now, Now, board);
        return id;
    }

    public StoredBoard Get(Guid id) =>
        _boards.TryGetValue(id, out var stored) ? stored : throw new BoardNotFoundException(id);

    public TaskCompletionSource? ListGate { get; set; }

    public TaskCompletionSource? LoadGate { get; set; }

    public async Task<IReadOnlyList<BoardSummary>> ListAsync()
    {
        if (ListGate is { } gate)
        {
            await gate.Task;
        }

        return _boards
            .Values.Select(stored => new BoardSummary(
                stored.Id,
                stored.Name,
                stored.CreatedAt,
                stored.UpdatedAt
            ))
            .ToList();
    }

    public Task<SavedBoard> CreateAsync(string name)
    {
        var board = new Board();
        var id = Add(name, board);
        return Task.FromResult(new SavedBoard(id, name, board));
    }

    public async Task<SavedBoard> LoadAsync(Guid id)
    {
        if (LoadGate is { } gate)
        {
            await gate.Task;
        }

        return new SavedBoard(id, Get(id).Name, Get(id).Board);
    }

    public TaskCompletionSource? SaveGate { get; set; }

    public async Task SaveAsync(Guid id, Board board)
    {
        _boards[id] = Get(id) with { Board = board, UpdatedAt = Now };
        Saves.Add(new BoardSave(id, board, Now));
        if (SaveGate is { } gate)
        {
            await gate.Task;
        }
    }

    public Task RenameAsync(Guid id, string name)
    {
        _boards[id] = Get(id) with { Name = name, UpdatedAt = Now };
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id)
    {
        _boards.Remove(id);
        return Task.CompletedTask;
    }

    public sealed record StoredBoard(
        Guid Id,
        string Name,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        Board Board
    );

    public sealed record BoardSave(Guid Id, Board Board, DateTimeOffset At);
}
