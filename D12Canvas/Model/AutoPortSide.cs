namespace D12Canvas.Model;

// Which standard port an auto endpoint attaches at: the side that the line from the bounds' centre
// to the aimed-at point crosses, or, for a point inside the bounds, the side nearest it. A tie
// goes to the side listed first in StandardPorts.All.
public static class AutoPortSide
{
    public static PortId Facing(Bounds bounds, (double X, double Y) aimedAt)
    {
        if (bounds.Contains(aimedAt.X, aimedAt.Y))
        {
            return NearestSide(bounds, aimedAt);
        }

        var dx = aimedAt.X - (bounds.X + bounds.Width / 2);
        var dy = aimedAt.Y - (bounds.Y + bounds.Height / 2);
        var horizontalReach = Math.Abs(dx) * bounds.Height;
        var verticalReach = Math.Abs(dy) * bounds.Width;
        var vertical = dy < 0 ? PortId.Top : PortId.Bottom;
        var horizontal = dx > 0 ? PortId.Right : PortId.Left;

        if (verticalReach > horizontalReach)
        {
            return vertical;
        }

        if (horizontalReach > verticalReach)
        {
            return horizontal;
        }

        return StandardPorts.All.First(side => side == vertical || side == horizontal);
    }

    private static PortId NearestSide(Bounds bounds, (double X, double Y) point)
    {
        var nearest = StandardPorts.All[0];
        var nearestDistance = double.MaxValue;
        foreach (var side in StandardPorts.All)
        {
            var distance = side switch
            {
                PortId.Top => point.Y - bounds.Y,
                PortId.Right => bounds.Right - point.X,
                PortId.Bottom => bounds.Bottom - point.Y,
                PortId.Left => point.X - bounds.X,
                _ => throw new ArgumentOutOfRangeException(nameof(side)),
            };
            if (distance < nearestDistance)
            {
                nearest = side;
                nearestDistance = distance;
            }
        }

        return nearest;
    }
}
