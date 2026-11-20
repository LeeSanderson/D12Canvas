namespace D12Canvas.History;

public sealed class CompositeCommand : ICommand
{
    private readonly IReadOnlyList<ICommand> _commands;

    public CompositeCommand(IReadOnlyList<ICommand> commands)
    {
        _commands = commands;
    }

    public void Apply()
    {
        foreach (var command in _commands)
        {
            command.Apply();
        }
    }

    public void Undo()
    {
        for (var i = _commands.Count - 1; i >= 0; i--)
        {
            _commands[i].Undo();
        }
    }
}
