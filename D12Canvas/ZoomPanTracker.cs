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

    private bool SetScale(double newScale)
    {
        var clamped = Math.Max(_minZoom ?? MinPositiveScale, newScale);
        if (_maxZoom is { } maxZoom)
        {
            clamped = Math.Min(maxZoom, clamped);
        }

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
