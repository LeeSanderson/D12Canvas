using D12Canvas.Model;

namespace D12Canvas.History;

// Adding a custom port to an instance is its own undoable gesture, parallel to
// AddEntityCommand/AddEdgeCommand. Undo removes exactly the port that was added (by its own Id,
// not just "the last one"), since further ports may have been added to the same instance since.
public sealed class AddCustomPortCommand : ICommand
{
    private readonly ComponentInstance _instance;
    private readonly PortDef _port;

    public AddCustomPortCommand(ComponentInstance instance, PortDef port)
    {
        _instance = instance;
        _port = port;
    }

    public void Apply() => _instance.CustomPorts.Add(_port);

    public void Undo() => _instance.CustomPorts.Remove(_port);
}
