namespace D12Canvas.Model;

// ADR 0005: the closed set of shapes an Edge's Source/Target can take - attached to a component
// instance's standard port (PortEndpoint) or custom port (CustomPortEndpoint, ticket 55), or
// floating at a fixed board point (FloatingEndpoint, ticket 49). A marker interface rather than an
// abstract record base: every implementation is a cheap-equality value type (record struct), and
// PortEndpoint already shipped as a standalone struct in ticket 48.
public interface IEdgeEndpoint { }
