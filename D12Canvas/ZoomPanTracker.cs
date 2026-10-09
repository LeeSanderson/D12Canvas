using D12Canvas.Model;

namespace D12Canvas;

public class ZoomPanTracker
{
    // A pure numerical-stability floor, not a product-facing zoom limit - keeps Scale strictly
    // positive so Viewport (which divides by Scale) can never blow up even under an unbounded
    // zoom-out with no host-configured MinZoom. Far below any zoom level a host would ever want.
    private const double MinPositiveScale = 0.0001;

    private double _scale = 1.0;
    private double _panX = 0;
    private double _panY = 0;
    private int _containerWidth = 0;
    private int _containerHeight = 0;
    private double? _minZoom;
    private double? _maxZoom;

    public event EventHandler<ZoomPanChangedEventArgs>? Changed;

    // Optional host-configured zoom bounds - both default to unbounded. Set together so a host
    // changing both at once (e.g. via DiagramCanvas's own parameters) can never trip a transient
    // min > max state depending on assignment order. Re-clamps whatever Scale already is, so it
    // never sits outside newly-tightened bounds until the next ZoomIn/ZoomOut.
    public double? MinZoom => _minZoom;

    public double? MaxZoom => _maxZoom;

    public void SetZoomLimits(double? minZoom, double? maxZoom)
    {
        if (minZoom is not (null or > 0))
            throw new ArgumentException(nameof(minZoom));
        if (maxZoom is not (null or > 0))
            throw new ArgumentException(nameof(maxZoom));
        if (minZoom is { } min && maxZoom is { } max && min > max)
            throw new ArgumentException($"{nameof(minZoom)} must not exceed {nameof(maxZoom)}.");

        _minZoom = minZoom;
        _maxZoom = maxZoom;
        SetScale(_scale);
    }

    public double Scale
    {
        get { return _scale; }
        set { SetScale(value); }
    }

    public double PanX => _panX;
    public double PanY => _panY;
    public double ContainerWidth => _containerWidth;
    public double ContainerHeight => _containerHeight;

    // False during the brief window between first render and OnAfterRenderAsync's async
    // container-size JS round trip resolving - both dimensions default to 0 until then.
    public bool HasKnownContainerSize => _containerWidth > 0 && _containerHeight > 0;

    // Canvas-space rect currently visible in the container - the inverse of the
    // pan/scale transform CSS applies to .canvas-content.
    public Bounds Viewport =>
        new(-_panX / _scale, -_panY / _scale, _containerWidth / _scale, _containerHeight / _scale);

    public void SetContainerSize(int width, int height)
    {
        if (width < 0)
            throw new ArgumentException(nameof(width));
        if (height < 0)
            throw new ArgumentException(nameof(height));
        _containerWidth = width;
        _containerHeight = height;
        OnChanged();
    }

    // No fixed board extent - pan is never clamped, so content can be placed and panned to
    // arbitrarily far coordinates.
    public bool Pan(double deltaX, double deltaY) => SetPanPosition(_panX + deltaX, _panY + deltaY);

    public bool SetPanPosition(double panX, double panY)
    {
        if (panX != _panX || panY != _panY)
        {
            _panX = panX;
            _panY = panY;
            OnChanged();
            return true;
        }

        return false;
    }

    public bool Zoom(bool zoomIn) => zoomIn ? ZoomIn() : ZoomOut();

    public bool ZoomIn() => SetScale(_scale + 0.1);

    public bool ZoomOut() => SetScale(_scale - 0.1);

    // Multiplies the scale by factor, clamped to the zoom limits, and moves the pan so the board
    // point under the container point (x, y) stays under it. One change is raised for both.
    public bool ZoomAbout(double x, double y, double factor) =>
        SetScaleAbout(x, y, _scale * factor);

    // Sets the scale, clamped to the zoom limits, keeping the board point under the container
    // point (x, y) where it is. One change is raised for both.
    internal bool SetScaleAbout(double x, double y, double scale)
    {
        var newScale = Clamped(scale);
        if (newScale == _scale)
        {
            return false;
        }

        var boardX = (x - _panX) / _scale;
        var boardY = (y - _panY) / _scale;
        _scale = newScale;
        _panX = x - boardX * newScale;
        _panY = y - boardY * newScale;
        OnChanged();
        return true;
    }

    // The fraction of the container a framed rect may fill, and the scale framing never passes.
    internal const double FramingFill = 0.9;
    internal const double FramingScaleCap = 1.0;

    // The inverse of Viewport. The cap applies before the zoom limits, so a host's limits always
    // win, and the pan is computed against the scale the clamp left, in one change.
    public bool Frame(Bounds target)
    {
        if (!HasKnownContainerSize)
        {
            return false;
        }

        var fit = Math.Min(
            FitAlong(_containerWidth, target.Width),
            FitAlong(_containerHeight, target.Height)
        );
        var newScale = Clamped(Math.Min(fit, FramingScaleCap));
        var newPanX = _containerWidth / 2.0 - (target.X + target.Width / 2) * newScale;
        var newPanY = _containerHeight / 2.0 - (target.Y + target.Height / 2) * newScale;
        if (newScale == _scale && newPanX == _panX && newPanY == _panY)
        {
            return false;
        }

        _scale = newScale;
        _panX = newPanX;
        _panY = newPanY;
        OnChanged();
        return true;
    }

    private static double FitAlong(double container, double target) =>
        target > 0 ? container * FramingFill / target : double.PositiveInfinity;

    private double Clamped(double scale)
    {
        var clamped = Math.Max(_minZoom ?? MinPositiveScale, scale);
        return _maxZoom is { } maxZoom ? Math.Min(maxZoom, clamped) : clamped;
    }

    private bool SetScale(double newScale)
    {
        var clamped = Clamped(newScale);

        if (clamped != _scale)
        {
            _scale = clamped;
            OnChanged();
            return true;
        }

        return false;
    }

    private void OnChanged()
    {
        Changed?.Invoke(
            this,
            new ZoomPanChangedEventArgs(_scale, _panX, _panY, _containerWidth, _containerHeight)
        );
    }
}
