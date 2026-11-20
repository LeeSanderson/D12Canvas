using D12Canvas.Model;

namespace D12Canvas.History;

public static class CustomPortRemoval
{
    public static ICommand? Compose(Board board, ComponentInstance instance, Guid portId)
    {
        var index = instance.CustomPorts.FindIndex(port => port.Id == portId);
        if (instance.Locked || index < 0)
        {
            return null;
        }

        var pinned = new CustomPortEndpoint(instance.Id, portId);
        var auto = new AutoPortEndpoint(instance.Id);
        var pinnedEnds = board
            .Edges.SelectMany(edge =>
                new[] { (Edge: edge, IsSource: true), (Edge: edge, IsSource: false) }
            )
            .Where(end => (end.IsSource ? end.Edge.Source : end.Edge.Target).Equals(pinned))
            .ToList();
        if (pinnedEnds.Any(end => end.Edge.Locked))
        {
            return null;
        }

        var commands = new List<ICommand>
        {
            new RemoveCustomPortCommand(instance, instance.CustomPorts[index]),
        };
        commands.AddRange(
            pinnedEnds.Select(end => new ChangeEdgeEndpointCommand(
                end.Edge,
                end.IsSource,
                pinned,
                auto
            ))
        );
        return new CompositeCommand(commands);
    }
}
