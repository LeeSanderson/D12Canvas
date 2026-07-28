namespace D12Canvas.Model;

// ADR 0005: the four standard ports every ComponentInstance automatically exposes at its border
// centers - no registration opt-in needed (CONTEXT.md's Port term).
public enum PortId
{
    Top,
    Right,
    Bottom,
    Left,
}
