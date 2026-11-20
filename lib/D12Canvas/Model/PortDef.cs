namespace D12Canvas.Model;

public readonly record struct PortDef(Guid Id, double FractionX, double FractionY)
{
    public PortDef(double fractionX, double fractionY)
        : this(Guid.NewGuid(), fractionX, fractionY) { }
}
