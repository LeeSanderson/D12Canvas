# 76 — `DiagramCanvas.DisposeAsync()` throws on every real (non-mocked) disposal

**What's wrong:** `DiagramCanvas.OnAfterRenderAsync` (`D12Canvas/DiagramCanvas.razor.cs`) does:

```csharp
var resizeCleanup = await _jsModule.InvokeAsync<Action>("addResizeListener", ContainerElement, _dotNetObjectRef);
var keyboardCleanup = await _jsModule.InvokeAsync<Action>("addKeyboardListener", ContainerElement, _dotNetObjectRef);
_cleanupFunctions.Add(resizeCleanup);
_cleanupFunctions.Add(keyboardCleanup);
```

`DiagramCanvas.razor.js`'s `addResizeListener`/`addKeyboardListener` each return a real JS closure (`return () => resizeObserver.disconnect();`, etc.). A JS function isn't JSON-serializable, so `InvokeAsync<Action>` can't actually marshal it into an invocable .NET delegate - in a real browser, `resizeCleanup`/`keyboardCleanup` come back `null`. `DisposeAsync()`'s `_cleanupFunctions.ForEach(f => f())` then throws `NullReferenceException` (invoking a null delegate) the moment a `DiagramCanvas` instance is ever actually disposed - logged as a `crit` unhandled-exception by the WebAssembly renderer.

**Status:** needs-triage

**How it was found:** Building ticket 35's save/load demo page, an earlier draft forced a full `DiagramCanvas` remount (`@key` on the component) to sidestep ticket 75's stale-render gap. That was the first place in the whole app that actually disposes a live `DiagramCanvas` mid-session, and it reliably reproduced this crash. Every existing bUnit test (`ComponentTestBase.SetupDiagramCanvasJsModule`) mocks `addResizeListener`/`addKeyboardListener` to directly return a real C# `Action` (`.SetResult(() => { })`), which bypasses the real JSON-marshaling failure entirely - so the bug has been invisible to the test suite since whichever ticket introduced these listeners.

**Why not fixed as part of ticket 35:** Out of scope for JSON round-trip, and the real fix is more invasive than it looks: it needs `_cleanupFunctions` to hold `IJSObjectReference` (an opaque handle to the still-alive JS function) instead of `Action`, plus a small JS helper to invoke-then-dispose that reference, plus `DisposeAsync` becoming properly async over that. That in turn means updating every bUnit test's mocked module setup (`ComponentTestBase.SetupDiagramCanvasJsModule` and any test that overrides it), which touches ~15 test files unrelated to persistence - too wide a blast radius to bundle into this ticket.

**Workaround used in ticket 35's demo page:** the shipped `SaveLoadDemo.razor` never remounts `DiagramCanvas` - `Load` reassigns the `Board` parameter on the same long-lived instance, so disposal (and this bug) is never triggered.
