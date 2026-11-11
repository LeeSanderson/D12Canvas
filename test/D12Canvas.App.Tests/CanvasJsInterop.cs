using Bunit;
using D12Canvas.BuiltIns;

namespace D12Canvas.App.Tests;

public sealed class CanvasJsInterop
{
    public CanvasJsInterop(BunitJSInterop jsInterop)
    {
        CanvasModule = jsInterop.SetupModule("./_content/D12Canvas/DiagramCanvas.razor.js");
        CanvasModule
            .Setup<InitialFacts>("initialFacts", _ => true)
            .SetResult(new InitialFacts(800, 600, ApplePlatform: false));
        CanvasModule
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
        SetupCleanupHandle(CanvasModule, "addResizeListener");
        SetupCleanupHandle(CanvasModule, "addKeyboardListener");
        SetupCleanupHandle(CanvasModule, "addWheelListener");
        SetupCleanupHandle(CanvasModule, "addClipboardListener");
        SetupCleanupHandle(CanvasModule, "addFileDropListener");
        SetupCleanupHandle(CanvasModule, "addPointerListener")
            .SetupVoid("promote", _ => true)
            .SetVoidResult();
        CanvasModule.Setup<HeldImage[]>("readClipboardImages", _ => true).SetResult([]);
        CanvasModule.SetupVoid("focusGroupTabStop", _ => true).SetVoidResult();
        CanvasModule.SetupVoid("focusTabStopAt", _ => true).SetVoidResult();
        CanvasModule.SetupVoid("focusCanvas", _ => true).SetVoidResult();

        var inlineEditorModule = jsInterop.SetupModule(InlineEditorEntry.ModulePath);
        inlineEditorModule.SetupVoid("focusAndSelectAll", _ => true).SetVoidResult();
        inlineEditorModule.SetupVoid("growWithContent", _ => true).SetVoidResult();

        SetupCleanupHandle(
            jsInterop.SetupModule("./_content/D12Canvas/ContextMenu.razor.js"),
            "registerMenu"
        );

        jsInterop
            .SetupModule(PropertyPanel.ModulePath)
            .SetupVoid("setIndeterminate", _ => true)
            .SetVoidResult();

        var propertyBarModule = jsInterop.SetupModule(PropertyBar.ModulePath);
        SetupCleanupHandle(propertyBarModule, "registerPropertyBar");
        propertyBarModule.Setup<double>("measurePropertyBar", _ => true).SetResult(120);
    }

    public BunitJSModuleInterop CanvasModule { get; }

    private static BunitJSModuleInterop SetupCleanupHandle(
        BunitJSModuleInterop module,
        string identifier
    )
    {
        var handle = module.SetupModule(identifier, _ => true);
        handle.SetupVoid("dispose", _ => true).SetVoidResult();
        return handle;
    }
}
