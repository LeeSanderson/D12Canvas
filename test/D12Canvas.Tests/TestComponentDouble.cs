using Microsoft.AspNetCore.Components;

namespace D12Canvas.Tests;

internal sealed class TestComponentDouble : IComponent
{
    public void Attach(RenderHandle renderHandle) { }

    public Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
}

internal sealed record TestProps(string Text = "");
