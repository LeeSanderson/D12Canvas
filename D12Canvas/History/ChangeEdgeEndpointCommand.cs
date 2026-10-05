using D12Canvas.Model;

namespace D12Canvas.History;

// Moves one end of an edge, attached or floating, to another endpoint. The edge counterpart to
// ChangeBoundsCommand for geometry, where ChangeEdgeStyleCommand covers its looks.
public sealed class ChangeEdgeEndpointCommand : ICommand
{
    private readonly Edge _edge;
    private readonly bool _isSource;
    private readonly IEdgeEndpoint _before;
    private readonly IEdgeEndpoint _after;

    public ChangeEdgeEndpointCommand(
        Edge edge,
        bool isSource,
        IEdgeEndpoint before,
        IEdgeEndpoint after
    )
    {
        _edge = edge;
        _isSource = isSource;
        _before = before;
        _after = after;
    }

    public void Apply() => Set(_after);

    public void Undo() => Set(_before);

    private void Set(IEdgeEndpoint endpoint)
    {
        if (_isSource)
        {
            _edge.Source = endpoint;
        }
        else
        {
            _edge.Target = endpoint;
        }
    }
}
