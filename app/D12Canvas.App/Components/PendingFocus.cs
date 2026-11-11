using Microsoft.AspNetCore.Components;

namespace D12Canvas.App.Components;

// An element can only take focus once the render that shows it has reached the DOM, and its
// reference is only assigned by that render, so the target is read when focus is applied.
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
