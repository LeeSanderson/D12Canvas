namespace D12Canvas.Model;

internal static class EndpointAttachment
{
    // The component an attached endpoint tracks, or null for an endpoint fixed at a board point.
    public static Guid? ComponentIdOf(IEdgeEndpoint endpoint) =>
        endpoint switch
        {
            PortEndpoint port => port.ComponentId,
            CustomPortEndpoint custom => custom.ComponentId,
            _ => null,
        };
}
