using Bunit;
using D12Canvas.Registration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;

namespace D12Canvas.Tests;

public abstract class ComponentTestBase : BunitContext
{
    protected ComponentTestBase()
    {
        Services.AddScoped<IServiceProvider>(sp => sp);
        Services.AddSingleton<IComponentRegistry>(new ComponentRegistry());
        JSInterop
            .SetupModule(Palette.ModulePath)
            .SetupVoid("registerRail", _ => true)
            .SetVoidResult();
        FlyoutModule = JSInterop.SetupModule(Flyout.ModulePath);
        SetupDisposableCleanupHandle(FlyoutModule, "registerFlyout");
        PropertyPanelModule = JSInterop.SetupModule(PropertyPanel.ModulePath);
        PropertyPanelModule.SetupVoid("setIndeterminate", _ => true).SetVoidResult();
        PropertyPanelModule.SetupVoid("registerTileKeys", _ => true).SetVoidResult();
    }

    protected BunitJSModuleInterop FlyoutModule { get; }

    protected IRenderedComponent<TComponent> RenderInIconScope<TComponent>(
        Action<ComponentParameterCollectionBuilder<TComponent>>? parameters = null
    )
        where TComponent : Microsoft.AspNetCore.Components.IComponent =>
        Render<Microsoft.AspNetCore.Components.CascadingValue<D12IconStyles>>(root =>
                root.Add(p => p.Value, D12IconStyles.InScope)
                    .Add(p => p.IsFixed, true)
                    .AddChildContent<TComponent>(parameters ?? (_ => { }))
            )
            .FindComponent<TComponent>();

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

    protected static string ExtractEveryBlock(string css, string marker)
    {
        var blocks = new List<string>();
        var rest = css;
        while (rest.Contains(marker, StringComparison.Ordinal))
        {
            blocks.Add(ExtractBlock(rest, marker));
            rest = rest[(rest.IndexOf(marker, StringComparison.Ordinal) + marker.Length)..];
        }

        Assert.NotEmpty(blocks);
        return string.Join("\n", blocks);
    }

    protected void SetupDiagramCanvasJsModule()
    {
        var module = JSInterop.SetupModule("./_content/D12Canvas/DiagramCanvas.razor.js");
        CanvasModule = module;
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
        ReportPlatform(applePlatform: false);
        SetupDisposableCleanupHandle(module, "addResizeListener");
        SetupDisposableCleanupHandle(module, "addKeyboardListener");
        SetupDisposableCleanupHandle(module, "addWheelListener");
        SetupDisposableCleanupHandle(module, "addClipboardListener");
        SetupDisposableCleanupHandle(module, "addFileDropListener");
        module.Setup<HeldImage[]>("readClipboardImages", _ => true).SetResult([]);
        PointerListener = SetupDisposableCleanupHandle(module, "addPointerListener");
        PointerListener.SetupVoid("promote", _ => true).SetVoidResult();

        module.SetupVoid("focusGroupTabStop", _ => true).SetVoidResult();
        module.SetupVoid("focusTabStopAt", _ => true).SetVoidResult();
        module.SetupVoid("focusCanvas", _ => true).SetVoidResult();
        SetupInlineEditorJsModule();

        var menuModule = JSInterop.SetupModule("./_content/D12Canvas/ContextMenu.razor.js");
        SetupDisposableCleanupHandle(menuModule, "registerMenu");

        PropertyBarModule = JSInterop.SetupModule(PropertyBar.ModulePath);
        SetupDisposableCleanupHandle(PropertyBarModule, "registerPropertyBar");
        PropertyBarModule.SetupVoid("focusFirstControl", _ => true).SetVoidResult();
        ReportPropertyBarWidth(120);
    }

    protected BunitJSModuleInterop PropertyBarModule { get; private set; } = null!;

    protected BunitJSModuleInterop PropertyPanelModule { get; }

    protected void ReportPropertyBarWidth(double width) =>
        PropertyBarModule.Setup<double>("measurePropertyBar", _ => true).SetResult(width);

    protected BunitJSModuleInterop SetupInlineEditorJsModule()
    {
        var module = JSInterop.SetupModule(D12Canvas.BuiltIns.InlineEditorEntry.ModulePath);
        module.SetupVoid("focusAndSelectAll", _ => true).SetVoidResult();
        module.SetupVoid("growWithContent", _ => true).SetVoidResult();
        return module;
    }

    protected void ReportPlatform(bool applePlatform, bool asyncClipboard = false) =>
        CanvasModule
            .Setup<InitialFacts>("initialFacts", _ => true)
            .SetResult(new InitialFacts(800, 600, applePlatform, asyncClipboard));

    protected BunitJSModuleInterop CanvasModule { get; private set; } = null!;

    protected static HeldImage HoldImage(
        BunitJSModuleInterop module,
        int token,
        byte[] bytes,
        double width = 64,
        double height = 32,
        string mimeType = "image/png"
    )
    {
        module
            .Setup<IJSStreamReference>(
                "takeImageBytes",
                invocation => Equals(invocation.Arguments[0], token)
            )
            .SetResult(new BytesStreamReference(bytes));
        return new HeldImage(token, mimeType, width, height);
    }

    private sealed class BytesStreamReference(byte[] bytes) : IJSStreamReference
    {
        public long Length => bytes.Length;

        public ValueTask<Stream> OpenReadStreamAsync(
            long maxAllowedSize = 512000,
            CancellationToken cancellationToken = default
        ) => ValueTask.FromResult<Stream>(new MemoryStream(bytes));

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    protected BunitJSModuleInterop PointerListener { get; private set; } = null!;

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
