namespace D12Canvas.Model;

// An edge's own routing choice, never board-wide. Default is Straight.
public enum EdgeRouting
{
    Straight,
    Orthogonal,
    Curved,
}
