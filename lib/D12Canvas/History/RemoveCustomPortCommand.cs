using D12Canvas.Model;

namespace D12Canvas.History;

// The inverse of AddCustomPortCommand. Undo puts the port back at the index it was removed from,
// since that order is both the Space cycle's and the saved file's.
public sealed class RemoveCustomPortCommand : ICommand
{
    private readonly ComponentInstance _instance;
    private readonly PortDef _port;
    private int _index;

    public RemoveCustomPortCommand(ComponentInstance instance, PortDef port)
    {
        _instance = instance;
        _port = port;
    }

    public void Apply()
    {
        _index = _instance.CustomPorts.IndexOf(_port);
        _instance.CustomPorts.RemoveAt(_index);
    }

    public void Undo() => _instance.CustomPorts.Insert(_index, _port);
}
