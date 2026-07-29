namespace D12Canvas.Model;

// ADR 0005/ticket 52: an edge's own routing choice, never board-wide. Default is Straight.
public enum EdgeRouting
{
    Straight,
    Orthogonal,
    Curved,
}
