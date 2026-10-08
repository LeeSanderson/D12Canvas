using System.Globalization;
using D12Canvas.Model;

namespace D12Canvas;

// One end of an edge as the router sees it: where it resolves, the side it leaves along, and the
// bounds of the shape it is attached to (null for a floating end).
internal readonly record struct RouteEnd(double X, double Y, PortId Side, Bounds? Shape)
{
    public (double X, double Y) Point => (X, Y);
}

// Everything a route depends on. Two equal requests always give the same route, which is what
// lets a route be cached until one of these changes.
internal readonly record struct RouteRequest(EdgeRouting Style, RouteEnd Source, RouteEnd Target);

// A routed edge. Points is the polyline for Straight and Orthogonal, and the start, both control
// points and the end of the cubic for Curved.
internal sealed class EdgeRoute
{
    public EdgeRoute(RouteRequest request, IReadOnlyList<(double X, double Y)> points)
    {
        Request = request;
        Points = points;
        PathData = PathDataOf(request.Style, points);
        LabelAnchor = LabelAnchorOf(request.Style, points);
    }

    public RouteRequest Request { get; }

    public IReadOnlyList<(double X, double Y)> Points { get; }

    public string PathData { get; }

    public (double X, double Y) LabelAnchor { get; }

    private static string PathDataOf(
        EdgeRouting style,
        IReadOnlyList<(double X, double Y)> points
    ) =>
        style == EdgeRouting.Curved
            ? $"M {Format(points[0])} C {Format(points[1])} {Format(points[2])} {Format(points[3])}"
            : "M " + string.Join(" L ", points.Select(Format));

    private static string Format((double X, double Y) point) =>
        $"{point.X.ToString(CultureInfo.InvariantCulture)} {point.Y.ToString(CultureInfo.InvariantCulture)}";

    private static (double X, double Y) LabelAnchorOf(
        EdgeRouting style,
        IReadOnlyList<(double X, double Y)> points
    ) =>
        style == EdgeRouting.Curved
            ? (
                (points[0].X + 3 * points[1].X + 3 * points[2].X + points[3].X) / 8,
                (points[0].Y + 3 * points[1].Y + 3 * points[2].Y + points[3].Y) / 8
            )
            : HalfwayAlong(points);

    private static (double X, double Y) HalfwayAlong(IReadOnlyList<(double X, double Y)> points)
    {
        var remaining = EdgeRouter.LengthOf(points) / 2;
        for (var i = 1; i < points.Count; i++)
        {
            var (from, to) = (points[i - 1], points[i]);
            var segment = Distance(from, to);
            if (remaining <= segment && segment > 0)
            {
                var fraction = remaining / segment;
                return (from.X + (to.X - from.X) * fraction, from.Y + (to.Y - from.Y) * fraction);
            }

            remaining -= segment;
        }

        return points[^1];
    }

    private static double Distance((double X, double Y) from, (double X, double Y) to) =>
        Math.Sqrt((to.X - from.X) * (to.X - from.X) + (to.Y - from.Y) * (to.Y - from.Y));
}

// Draws an edge from its two ends. Orthogonal leaves each end along its side by the stub and takes
// the cheapest orthogonal path clear of both connected shapes inflated by the stub, falling back to
// the cheapest path that ignores them; Curved puts each control point on its end's normal.
internal static class EdgeRouter
{
    public const double Stub = 20;

    public const double BendPenalty = 2 * Stub;

    public const double CurveReach = 0.4;

    private const double Tolerance = 1e-6;

    private const double ClearanceSlack = 0.01;

    public static EdgeRoute Route(RouteRequest request) =>
        new(
            request,
            request.Style switch
            {
                EdgeRouting.Orthogonal => Orthogonal(request.Source, request.Target),
                EdgeRouting.Curved => Curved(request.Source, request.Target),
                _ => [request.Source.Point, request.Target.Point],
            }
        );

    // The side a floating end leaves along: whichever axis points more towards the other end, in
    // that direction, a tie going horizontal.
    public static PortId PseudoSide((double X, double Y) from, (double X, double Y) towards)
    {
        var dx = towards.X - from.X;
        var dy = towards.Y - from.Y;
        if (Math.Abs(dx) >= Math.Abs(dy))
        {
            return dx >= 0 ? PortId.Right : PortId.Left;
        }

        return dy >= 0 ? PortId.Bottom : PortId.Top;
    }

    // The border a custom port's fraction lies on, a corner taking the first matching side in
    // StandardPorts.All, or the nearest border for a fraction inside the shape.
    public static PortId SideOfFraction(double fractionX, double fractionY)
    {
        foreach (var side in StandardPorts.All)
        {
            var onSide = side switch
            {
                PortId.Top => fractionY <= 0,
                PortId.Right => fractionX >= 1,
                PortId.Bottom => fractionY >= 1,
                _ => fractionX <= 0,
            };
            if (onSide)
            {
                return side;
            }
        }

        return AutoPortSide.Facing(new Bounds(0, 0, 1, 1), (fractionX, fractionY));
    }

    public static (double X, double Y) Normal(PortId side) =>
        side switch
        {
            PortId.Top => (0, -1),
            PortId.Right => (1, 0),
            PortId.Bottom => (0, 1),
            _ => (-1, 0),
        };

    public static double LengthOf(IReadOnlyList<(double X, double Y)> points)
    {
        var total = 0.0;
        for (var i = 1; i < points.Count; i++)
        {
            var dx = points[i].X - points[i - 1].X;
            var dy = points[i].Y - points[i - 1].Y;
            total += Math.Sqrt(dx * dx + dy * dy);
        }

        return total;
    }

    private static List<(double X, double Y)> Curved(RouteEnd source, RouteEnd target)
    {
        var distance = LengthOf([source.Point, target.Point]);
        var offset = Math.Max(Stub, CurveReach * distance);
        return
        [
            source.Point,
            Offset(source.Point, source.Side, offset),
            Offset(target.Point, target.Side, offset),
            target.Point,
        ];
    }

    private static List<(double X, double Y)> Orthogonal(RouteEnd source, RouteEnd target)
    {
        var start = source.Point;
        var end = target.Point;
        var startStub = Offset(start, source.Side, Stub);
        var endStub = Offset(end, target.Side, Stub);
        var shapes = new[] { source.Shape, target.Shape }.OfType<Bounds>().ToList();
        var clearances = shapes.Select(shape => Inflate(shape, Stub - ClearanceSlack)).ToList();

        List<(double X, double Y)>? clear = null;
        var clearCost = double.MaxValue;
        List<(double X, double Y)>? any = null;
        var anyCost = double.MaxValue;

        foreach (var middle in Candidates(startStub, endStub, shapes))
        {
            var path = Simplify([start, .. middle, end]);
            if (!IsOrthogonal(path) || DoublesBack(path))
            {
                continue;
            }

            if (!LeavesAlong(path, source.Side) || !LeavesAlong(Reversed(path), target.Side))
            {
                continue;
            }

            var cost = LengthOf(path) + BendPenalty * (path.Count - 2);
            if (cost < anyCost)
            {
                (any, anyCost) = (path, cost);
            }

            if (cost < clearCost && !Crosses(Simplify(middle), clearances))
            {
                (clear, clearCost) = (path, cost);
            }
        }

        return clear ?? any ?? Simplify([start, startStub, (endStub.X, startStub.Y), endStub, end]);
    }

    private static IEnumerable<List<(double X, double Y)>> Candidates(
        (double X, double Y) startStub,
        (double X, double Y) endStub,
        IReadOnlyList<Bounds> shapes
    )
    {
        var xs = Lanes(startStub.X, endStub.X, shapes, shape => shape.X, shape => shape.Right);
        var ys = Lanes(startStub.Y, endStub.Y, shapes, shape => shape.Y, shape => shape.Bottom);

        yield return [startStub, endStub];
        foreach (var x in xs)
        {
            yield return [startStub, (x, startStub.Y), (x, endStub.Y), endStub];
        }

        foreach (var y in ys)
        {
            yield return [startStub, (startStub.X, y), (endStub.X, y), endStub];
        }

        yield return [startStub, (endStub.X, startStub.Y), endStub];
        yield return [startStub, (startStub.X, endStub.Y), endStub];

        foreach (var x in xs)
        {
            foreach (var y in ys)
            {
                yield return [startStub, (x, startStub.Y), (x, y), (endStub.X, y), endStub];
                yield return [startStub, (startStub.X, y), (x, y), (x, endStub.Y), endStub];
            }
        }
    }

    // Coordinates a path may run along on one axis, the gap between the shapes and the midpoint
    // between the stubs first, so that among equally cheap paths the centred one wins.
    private static List<double> Lanes(
        double startStub,
        double endStub,
        IReadOnlyList<Bounds> shapes,
        Func<Bounds, double> low,
        Func<Bounds, double> high
    )
    {
        var lanes = new List<double>();
        if (shapes.Count == 2)
        {
            var (first, second) = (shapes[0], shapes[1]);
            if (high(first) < low(second))
            {
                lanes.Add((high(first) + low(second)) / 2);
            }

            if (high(second) < low(first))
            {
                lanes.Add((high(second) + low(first)) / 2);
            }
        }

        lanes.Add((startStub + endStub) / 2);
        lanes.Add(startStub);
        lanes.Add(endStub);
        lanes.Add(Math.Min(startStub, endStub) - Stub);
        lanes.Add(Math.Max(startStub, endStub) + Stub);
        foreach (var shape in shapes)
        {
            lanes.Add(low(shape) - Stub);
            lanes.Add(high(shape) + Stub);
        }

        return lanes;
    }

    private static (double X, double Y) Offset((double X, double Y) point, PortId side, double by)
    {
        var normal = Normal(side);
        return (point.X + normal.X * by, point.Y + normal.Y * by);
    }

    private static Bounds Inflate(Bounds bounds, double by) =>
        new(bounds.X - by, bounds.Y - by, bounds.Width + 2 * by, bounds.Height + 2 * by);

    private static bool LeavesAlong(IReadOnlyList<(double X, double Y)> path, PortId side)
    {
        var normal = Normal(side);
        var dx = path[1].X - path[0].X;
        var dy = path[1].Y - path[0].Y;
        var along = dx * normal.X + dy * normal.Y;
        var across = Math.Abs(dx * normal.Y - dy * normal.X);
        return across <= Tolerance && along >= Stub - Tolerance;
    }

    private static bool IsOrthogonal(IReadOnlyList<(double X, double Y)> path)
    {
        for (var i = 1; i < path.Count; i++)
        {
            if (
                Math.Abs(path[i].X - path[i - 1].X) > Tolerance
                && Math.Abs(path[i].Y - path[i - 1].Y) > Tolerance
            )
            {
                return false;
            }
        }

        return path.Count >= 2;
    }

    private static bool DoublesBack(IReadOnlyList<(double X, double Y)> path)
    {
        for (var i = 2; i < path.Count; i++)
        {
            var (a, b, c) = (path[i - 2], path[i - 1], path[i]);
            var (cross, dot) = Turn(a, b, c);
            if (Math.Abs(cross) <= Tolerance && dot < 0)
            {
                return true;
            }
        }

        return false;
    }

    private static bool Crosses(
        IReadOnlyList<(double X, double Y)> path,
        IReadOnlyList<Bounds> areas
    )
    {
        for (var i = 1; i < path.Count; i++)
        {
            var (a, b) = (path[i - 1], path[i]);
            var (minX, maxX) = (Math.Min(a.X, b.X), Math.Max(a.X, b.X));
            var (minY, maxY) = (Math.Min(a.Y, b.Y), Math.Max(a.Y, b.Y));
            foreach (var area in areas)
            {
                if (maxX > area.X && minX < area.Right && maxY > area.Y && minY < area.Bottom)
                {
                    return true;
                }
            }
        }

        return false;
    }

    // How the run b-c turns from the run a-b: a zero cross product means it carries on in line,
    // and then a negative dot product means it goes straight back.
    private static (double Cross, double Dot) Turn(
        (double X, double Y) a,
        (double X, double Y) b,
        (double X, double Y) c
    ) =>
        (
            (b.X - a.X) * (c.Y - b.Y) - (b.Y - a.Y) * (c.X - b.X),
            (b.X - a.X) * (c.X - b.X) + (b.Y - a.Y) * (c.Y - b.Y)
        );

    private static List<(double X, double Y)> Reversed(IReadOnlyList<(double X, double Y)> path) =>
        Enumerable.Reverse(path).ToList();

    // Drops repeated points and merges runs that carry on in the same direction; a run that
    // turns straight back is kept, so DoublesBack can reject it.
    private static List<(double X, double Y)> Simplify(IEnumerable<(double X, double Y)> points)
    {
        var result = new List<(double X, double Y)>();
        foreach (var point in points)
        {
            if (
                result.Count > 0
                && Math.Abs(result[^1].X - point.X) <= Tolerance
                && Math.Abs(result[^1].Y - point.Y) <= Tolerance
            )
            {
                continue;
            }

            while (result.Count >= 2)
            {
                var (a, b) = (result[^2], result[^1]);
                var (cross, dot) = Turn(a, b, point);
                if (Math.Abs(cross) > Tolerance || dot < 0)
                {
                    break;
                }

                result.RemoveAt(result.Count - 1);
            }

            result.Add(point);
        }

        return result;
    }
}
