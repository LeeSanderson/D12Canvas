namespace D12Canvas.Model;

// ADR 0005/ticket 55: a custom port an end user adds to a specific instance at runtime - the same
// fractional border representation as a standard port (StandardPorts.FractionOf), just
// user-authored and instance-scoped (ComponentInstance.CustomPorts) rather than automatic and
// type-scoped.
public readonly record struct PortDef(Guid Id, double FractionX, double FractionY)
{
    public PortDef(double fractionX, double fractionY)
        : this(Guid.NewGuid(), fractionX, fractionY) { }
}
