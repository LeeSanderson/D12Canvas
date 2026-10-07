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

    // The shared theme-token layer every independently-mounted chrome root declares its own
    // copy of.
    protected static readonly string[] ThemeTokens =
    [
        "--d12-surface",
        "--d12-border",
        "--d12-accent",
        "--d12-muted-text",
        "--d12-text",
    ];

    protected static string StyleBlockText<TComponent>(IRenderedComponent<TComponent> component)
        where TComponent : Microsoft.AspNetCore.Components.IComponent =>
        component.Find("style").InnerHtml;

    // Extracts the (possibly nested) `{ ... }` block immediately following the first occurrence
    // of `marker` - used both for a plain rule's own declaration block and for an @media block's
    // whole body (braces included), by counting nesting depth rather than matching the first `}`.
    protected static string ExtractBlock(string css, string marker)
    {
        var markerIndex = css.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(markerIndex >= 0, $"Expected to find `{marker}` in the style block.");

        var openIndex = css.IndexOf('{', markerIndex);
        Assert.True(openIndex >= 0, $"Expected `{{` after `{marker}`.");

        var depth = 0;
        for (var i = openIndex; i < css.Length; i++)
        {
            if (css[i] == '{')
            {
                depth++;
            }
            else if (css[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return css.Substring(openIndex + 1, i - openIndex - 1);
                }
            }
        }

        throw new Xunit.Sdk.XunitException($"Unbalanced braces after `{marker}`.");
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
        SetupDisposableCleanupHandle(module, "addResizeListener");
        SetupDisposableCleanupHandle(module, "addKeyboardListener");
        SetupDisposableCleanupHandle(module, "addWheelListener");
        PointerListener = SetupDisposableCleanupHandle(module, "addPointerListener");
        PointerListener.SetupVoid("promote", _ => true).SetVoidResult();

        module.SetupVoid("focusGroupTabStop", _ => true).SetVoidResult();
        module.SetupVoid("focusTabStopAt", _ => true).SetVoidResult();
    }

    // The handle the pointer listener returns, for asserting what the canvas tells the listener.
    protected BunitJSModuleInterop PointerListener { get; private set; } = null!;

    // Mocks a call that returns a disposable IJSObjectReference handle (a "dispose"-shaped object,
    // not a bare function - see DiagramCanvas.razor.js) - shared by both cleanup-registering calls
    // DiagramCanvas.OnAfterRenderAsync makes.
    private static BunitJSModuleInterop SetupDisposableCleanupHandle(
        BunitJSModuleInterop module,
        string identifier
    )
    {
        var handle = module.SetupModule(identifier, _ => true);
        handle.SetupVoid("dispose", _ => true).SetVoidResult();
        return handle;
    }
}
