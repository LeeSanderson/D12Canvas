using D12Canvas.Model;

namespace D12Canvas.History;

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
