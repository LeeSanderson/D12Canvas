namespace D12Canvas.History;

// Wraps an ordered list of child commands into one atomic history-stack entry - needed whenever a
// single gesture produces more than one primitive command (e.g. a multi-select move, one
// ChangeBoundsCommand per selected instance). Children undo in reverse order.
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
