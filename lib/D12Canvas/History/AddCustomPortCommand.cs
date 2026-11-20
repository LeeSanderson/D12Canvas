using D12Canvas.Model;

namespace D12Canvas.History;

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
