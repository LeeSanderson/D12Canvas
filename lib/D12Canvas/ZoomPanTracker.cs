using D12Canvas.Model;

namespace D12Canvas;

public class ZoomPanTracker
{
    private const double MinPositiveScale = 0.0001;

    private double _scale = 1.0;
    private double _panX = 0;
    private double _panY = 0;
    private int _containerWidth = 0;
    private int _containerHeight = 0;
    private double? _minZoom;
    private double? _maxZoom;

    public event EventHandler<ZoomPanChangedEventArgs>? Changed;

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

    public bool HasKnownContainerSize => _containerWidth > 0 && _containerHeight > 0;

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

    public bool ZoomAbout(double x, double y, double factor) =>
        SetScaleAbout(x, y, _scale * factor);

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

    internal const double FramingFill = 0.9;
    internal const double FramingScaleCap = 1.0;

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
