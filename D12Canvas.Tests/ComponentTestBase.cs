using Bunit;
using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

public abstract class ComponentTestBase : BunitContext
{
    protected ComponentTestBase()
    {
        // Add any common services or configurations here
        Services.AddScoped<IServiceProvider>(sp => sp);
        Services.AddSingleton<IComponentRegistry>(new ComponentRegistry());
    }

    protected void SetupDiagramCanvasJsModule()
    {
        var module = JSInterop.SetupModule("./_content/D12Canvas/DiagramCanvas.razor.js");
        module
            .Setup<Dictionary<string, double>>("getContainerDimensions", _ => true)
            .SetResult(new Dictionary<string, double> { ["width"] = 800, ["height"] = 600 });
        module.Setup<Action>("addResizeListener", _ => true).SetResult(() => { });
        module.Setup<Action>("addKeyboardListener", _ => true).SetResult(() => { });
    }
}
