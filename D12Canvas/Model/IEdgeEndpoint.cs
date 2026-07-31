namespace D12Canvas.Model;

// The closed set of shapes an Edge's Source/Target can take - attached to a component instance's
// standard port (PortEndpoint) or custom port (CustomPortEndpoint), or floating at a fixed board
// point (FloatingEndpoint). A marker interface rather than an abstract record base: every
// implementation is a cheap-equality value type (record struct), and PortEndpoint already shipped
// as a standalone struct beforehand.
public interface IEdgeEndpoint { }
