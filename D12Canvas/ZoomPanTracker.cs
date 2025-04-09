namespace D12Canvas;

public class ZoomPanTracker
{
    private const double MIN_SCALE = 0.6;
    private const double MAX_SCALE = 6.0;

    private double _scale = 1.0;
    private double _panX = 0;
    private double _panY = 0;
    private int _containerWidth = 0;
    private int _containerHeight = 0;
    private int _canvasWidth = 3000;
    private int _canvasHeight = 3000;

    public event EventHandler<ZoomPanChangedEventArgs>? Changed;

    public double Scale
    {
        get { return _scale; }
        set { SetScale(value); }
    }

    public double PanX => _panX;
    public double PanY => _panY;
    public double CanvasWidth => _canvasWidth;
    public double CanvasHeight => _canvasHeight;
    public double ContainerWidth => _containerWidth;
    public double ContainerHeight => _containerHeight;

    public void SetContainerSize(int width, int height)
    {
        if (width < 0)
            throw new ArgumentException(nameof(width));
        if (height < 0)
            throw new ArgumentException(nameof(height));
        _containerWidth = width;
        _containerHeight = height;
        ApplyPanPositionConstaints();
    }

    public void SetCanvasSize(int width, int height)
    {
        if (width < 0)
            throw new ArgumentException(nameof(width));
        if (height < 0)
            throw new ArgumentException(nameof(height));
        _canvasWidth = width;
        _canvasHeight = height;
        ApplyPanPositionConstaints();
    }

    public bool Pan(double deltaX, double deltaY)
    {
        _panX += deltaX;
        _panY += deltaY;
        return ApplyPanPositionConstaints();
    }

    public bool SetPanPosition(double panX, double panY)
    {
        if (panX != _panX || panY != _panY)
        {
            _panX = panX;
            _panY = panY;
            return ApplyPanPositionConstaints();
        }

        return false;
    }

    public bool Zoom(bool zoomIn) => zoomIn ? ZoomIn() : ZoomOut();

    public bool ZoomIn() => SetScale(Math.Min(MAX_SCALE, _scale + 0.1));

    public bool ZoomOut() => SetScale(Math.Max(MIN_SCALE, _scale - 0.1));

    private bool SetScale(double newScale)
    {
        if (newScale != _scale)
        {
            _scale = newScale;
            ApplyPanPositionConstaints();
            OnChanged();
            return true;
        }

        return false;
    }

    private bool ApplyPanPositionConstaints()
    {
        var maxPanX = _containerWidth - (_canvasWidth * _scale);
        var maxPanY = _containerHeight - (_canvasHeight * _scale);

        var newPanX = Math.Clamp(_panX, maxPanX, 0);
        var newPanY = Math.Clamp(_panY, maxPanY, 0);

        if (newPanX != _panX || newPanY != _panY)
        {
            _panX = newPanX;
            _panY = newPanY;
            OnChanged();
            return true;
        }

        return false;
    }

    private void OnChanged()
    {
        Changed?.Invoke(this, new ZoomPanChangedEventArgs(_scale, _panX, _panY));
    }
}
