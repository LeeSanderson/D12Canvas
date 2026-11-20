using Microsoft.AspNetCore.Components;

namespace D12Canvas.App.Components;

public sealed class PendingFocus
{
    private Func<ElementReference>? _target;

    public void Request(Func<ElementReference> target) => _target = target;

    public async Task ApplyAsync()
    {
        if (_target is not { } target)
        {
            return;
        }

        _target = null;
        await target().FocusAsync();
    }
}
