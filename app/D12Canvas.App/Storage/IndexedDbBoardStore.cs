using D12Canvas.Model;
using D12Canvas.Persistence;
using Microsoft.JSInterop;

namespace D12Canvas.App.Storage;

public sealed class IndexedDbBoardStore : IBoardStore, IAsyncDisposable
{
    private readonly IBoardSerializer _serializer;
    private readonly Lazy<Task<IJSObjectReference>> _module;

    public IndexedDbBoardStore(IJSRuntime js, IBoardSerializer serializer)
    {
        _serializer = serializer;
        _module = new Lazy<Task<IJSObjectReference>>(
            () =>
                js.InvokeAsync<IJSObjectReference>("import", "./js/indexedDbBoardStore.js").AsTask()
        );
    }

    public async Task<IReadOnlyList<BoardSummary>> ListAsync()
    {
        var module = await _module.Value;
        var records = await module.InvokeAsync<List<BoardSummaryRecord>>("listBoards");
        return records
            .Select(r => new BoardSummary(r.Id, r.Name, r.CreatedAt, r.UpdatedAt))
            .ToList();
    }

    public async Task<SavedBoard> CreateAsync(string name)
    {
        var module = await _module.Value;
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var board = new Board();

        await module.InvokeVoidAsync(
            "createBoard",
            id,
            name,
            now,
            now,
            _serializer.Serialize(board)
        );

        return new SavedBoard(id, name, board);
    }

    public async Task<SavedBoard> LoadAsync(Guid id)
    {
        var module = await _module.Value;
        var record = await module.InvokeAsync<BoardRecord?>("getBoard", id);
        if (record is null)
        {
            throw new BoardNotFoundException(id);
        }

        return new SavedBoard(id, record.Name, _serializer.Deserialize(record.BoardJson));
    }

    public async Task SaveAsync(Guid id, Board board)
    {
        var module = await _module.Value;
        await module.InvokeVoidAsync(
            "saveBoard",
            id,
            _serializer.Serialize(board),
            DateTimeOffset.UtcNow
        );
    }

    public async Task RenameAsync(Guid id, string name)
    {
        var module = await _module.Value;
        await module.InvokeVoidAsync("renameBoard", id, name, DateTimeOffset.UtcNow);
    }

    public async Task DeleteAsync(Guid id)
    {
        var module = await _module.Value;
        await module.InvokeVoidAsync("deleteBoard", id);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module.IsValueCreated)
        {
            var module = await _module.Value;
            await module.DisposeAsync();
        }
    }

    private sealed record BoardSummaryRecord(
        Guid Id,
        string Name,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt
    );

    private sealed record BoardRecord(
        Guid Id,
        string Name,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        string BoardJson
    );
}
