# Fix ComponentContainer's dynamic JS import path

Status: ready-for-agent

## Problem

See `../spec.md`. `ComponentContainer.razor.cs`'s `OnAfterRenderAsync` calls:

```csharp
_jsModule = await JavaScriptRuntime.InvokeAsync<IJSObjectReference>(
    "import",
    "./js/componentContainer.js"
);
```

This 404s on every instance when the library is consumed from a separate host app (confirmed on the existing `/componentcontainer-demo` page). `registerClickOutside`/`unregisterClickOutside` silently never run as a result.

## Fix

Change the import path to the RCL static-asset path, e.g.:

```csharp
"_content/D12Canvas/js/componentContainer.js"
```

or migrate `componentContainer.js` to Blazor's colocated JS-isolation convention (`ComponentContainer.razor.js`) so the framework resolves the URL automatically.

## Verify

Load `/componentcontainer-demo`, double-click a container to enter edit mode, click outside it, and confirm it exits edit mode with no console error (previously: `Microsoft.JSInterop.JSException: Failed to fetch dynamically imported module`).
