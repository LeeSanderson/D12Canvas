# ComponentContainer's dynamic JS import 404s on every instance

`ComponentContainer.razor.cs`'s `OnAfterRenderAsync` imports its JS module with a path relative to the host app:

```csharp
_jsModule = await JavaScriptRuntime.InvokeAsync<IJSObjectReference>(
    "import",
    "./js/componentContainer.js"
);
```

`D12Canvas` is a Razor Class Library — its static web assets are served to a consuming host app under `_content/D12Canvas/js/componentContainer.js`, not `./js/componentContainer.js`. The relative path only resolves correctly if the library's own `wwwroot` happens to be served at the host's root, which isn't the case for `D12Canvas.Demo`.

Confirmed via a headless Chromium run against the existing, unmodified `/componentcontainer-demo` page: every mounted `ComponentContainer` throws

```
Microsoft.JSInterop.JSException: Failed to fetch dynamically imported module: http://localhost:5124/js/componentContainer.js
```

Blazor WASM logs the unhandled exception and carries on rendering (it doesn't crash the app), so this has been silent. Practical effect: `registerClickOutside`/`unregisterClickOutside` never run, so **clicking outside a component in edit mode never auto-exits edit mode** — the feature has apparently never worked in any deployed or dev usage.

Discovered as a side effect of the wayfinder ticket "Virtualization/windowing mitigation stress test" (`../d12canvas-next/issues/02-virtualization-prototype.md`) — unrelated to that ticket's question, so split out here rather than folded into it.

## Fix

Use the RCL static-asset path (`_content/D12Canvas/js/componentContainer.js`) for the dynamic import, or adopt Blazor's colocated JS-isolation convention (`ComponentContainer.razor.js`, served automatically at the matching computed URL) instead of a hand-rolled relative path.
