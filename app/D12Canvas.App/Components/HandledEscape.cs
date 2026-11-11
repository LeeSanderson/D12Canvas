namespace D12Canvas.App.Components;

// Blazor can't stop one key's propagation and let the rest through, so a control that acts on
// Escape marks it here and an outer Escape handler further up the same dispatch skips it.
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
