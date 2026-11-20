namespace D12Canvas.App.Components;

public sealed class HandledEscape
{
    private bool _marked;

    public void Mark() => _marked = true;

    public bool TryTake()
    {
        var marked = _marked;
        _marked = false;
        return marked;
    }
}
