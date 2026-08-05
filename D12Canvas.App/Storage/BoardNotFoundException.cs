namespace D12Canvas.App.Storage;

public sealed class BoardNotFoundException : Exception
{
    public Guid Id { get; }

    public BoardNotFoundException(Guid id)
        : base($"No board found with id '{id}'.")
    {
        Id = id;
    }
}
