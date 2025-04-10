namespace D12Canvas;

public class ZoomPanChangedEventArgs : EventArgs
{
    public double Scale { get; }
    public double PanX { get; }
    public double PanY { get; }
    public int ContainerWidth { get; }
    public int ContainerHeight { get; }

    public ZoomPanChangedEventArgs(
        double scale,
        double panX,
        double panY,
        int containerWidth,
        int containerHeight
    )
    {
        Scale = scale;
        PanX = panX;
        PanY = panY;
        ContainerWidth = containerWidth;
        ContainerHeight = containerHeight;
    }
}
