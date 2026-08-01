using Bunit;
using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

public abstract class ComponentTestBase : BunitContext
{
    protected ComponentTestBase()
    {
        Services.AddScoped<IServiceProvider>(sp => sp);
        Services.AddSingleton<IComponentRegistry>(new ComponentRegistry());
    }

    protected void SetupDiagramCanvasJsModule()
    {
        var module = JSInterop.SetupModule("./_content/D12Canvas/DiagramCanvas.razor.js");
        module
            .Setup<Dictionary<string, double>>("getContainerDimensions", _ => true)
            .SetResult(
                new Dictionary<string, double>
                {
                    ["width"] = 800,
                    ["height"] = 600,
                    ["left"] = 0,
                    ["top"] = 0,
                }
            );
        module.Setup<Action>("addResizeListener", _ => true).SetResult(() => { });
        module.Setup<Action>("addKeyboardListener", _ => true).SetResult(() => { });
        module.SetupVoid("focusGroupTabStop", _ => true).SetVoidResult();
        module.SetupVoid("focusTabStopAt", _ => true).SetVoidResult();
    }

    // Every test that renders a ComponentContainer needs this - a plain click always attempts
    // the click-driven half of focus-follows-selection (ComponentContainer.HandleClick calling
    // focusElement), regardless of whether a given test cares about edit-mode's click-outside
    // behaviour too, so all three are configured together rather than per-test.
    protected void SetupComponentContainerJsModule()
    {
        var module = JSInterop.SetupModule("./_content/D12Canvas/ComponentContainer.razor.js");
        module.SetupVoid("registerClickOutside", _ => true).SetVoidResult();
        module.SetupVoid("unregisterClickOutside").SetVoidResult();
        module.SetupVoid("focusElement", _ => true).SetVoidResult();
    }
}
