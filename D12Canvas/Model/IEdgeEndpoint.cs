namespace D12Canvas.Model;

// The closed set of shapes an Edge's Source/Target can take - attached to a component instance's
// standard port (PortEndpoint), custom port (CustomPortEndpoint) or whichever standard port faces
// the other end (AutoPortEndpoint), or floating at a fixed board point (FloatingEndpoint). Every
// implementation is a cheap-equality value type (record struct). ComponentId is the instance an
// attached end tracks, and null for a floating end.
public interface IEdgeEndpoint
{
    Guid? ComponentId { get; }
}
