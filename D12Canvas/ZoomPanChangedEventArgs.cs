namespace D12Canvas;

public class ZoomPanChangedEventArgs : EventArgs
{
    public double Scale { get; }
    public double PanX { get; }
    public double PanY { get; }

    public ZoomPanChangedEventArgs(double scale, double panX, double panY)
    {
        Scale = scale;
        PanX = panX;
        PanY = panY;
    }
}
