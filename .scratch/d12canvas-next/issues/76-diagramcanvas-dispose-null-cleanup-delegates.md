# 76 — `DiagramCanvas.DisposeAsync()` throws on every real (non-mocked) disposal

**What's wrong:** `DiagramCanvas.OnAfterRenderAsync` (`D12Canvas/DiagramCanvas.razor.cs`) does:

```csharp
var resizeCleanup = await _jsModule.InvokeAsync<Action>("addResizeListener", ContainerElement, _dotNetObjectRef);
var keyboardCleanup = await _jsModule.InvokeAsync<Action>("addKeyboardListener", ContainerElement, _dotNetObjectRef);
_cleanupFunctions.Add(resizeCleanup);
_cleanupFunctions.Add(keyboardCleanup);
```

`DiagramCanvas.razor.js`'s `addResizeListener`/`addKeyboardListener` each return a real JS closure (`return () => resizeObserver.disconnect();`, etc.). A JS function isn't JSON-serializable, so `InvokeAsync<Action>` can't actually marshal it into an invocable .NET delegate - in a real browser, `resizeCleanup`/`keyboardCleanup` come back `null`. `DisposeAsync()`'s `_cleanupFunctions.ForEach(f => f())` then throws `NullReferenceException` (invoking a null delegate) the moment a `DiagramCanvas` instance is ever actually disposed - logged as a `crit` unhandled-exception by the WebAssembly renderer.

**Status:** resolved

**How it was found:** Building ticket 35's save/load demo page, an earlier draft forced a full `DiagramCanvas` remount (`@key` on the component) to sidestep ticket 75's stale-render gap. That was the first place in the whole app that actually disposes a live `DiagramCanvas` mid-session, and it reliably reproduced this crash. Every existing bUnit test (`ComponentTestBase.SetupDiagramCanvasJsModule`) mocks `addResizeListener`/`addKeyboardListener` to directly return a real C# `Action` (`.SetResult(() => { })`), which bypasses the real JSON-marshaling failure entirely - so the bug has been invisible to the test suite since whichever ticket introduced these listeners.

**Why not fixed as part of ticket 35:** Out of scope for JSON round-trip, and the real fix is more invasive than it looks: it needs `_cleanupFunctions` to hold `IJSObjectReference` (an opaque handle to the still-alive JS function) instead of `Action`, plus a small JS helper to invoke-then-dispose that reference, plus `DisposeAsync` becoming properly async over that. That in turn means updating every bUnit test's mocked module setup (`ComponentTestBase.SetupDiagramCanvasJsModule` and any test that overrides it), which touches ~15 test files unrelated to persistence - too wide a blast radius to bundle into this ticket.

**Workaround used in ticket 35's demo page:** the shipped `SaveLoadDemo.razor` never remounts `DiagramCanvas` - `Load` reassigns the `Board` parameter on the same long-lived instance, so disposal (and this bug) is never triggered.

**Resolution:** the blast radius turned out smaller than feared - `ComponentTestBase.SetupDiagramCanvasJsModule` is the *only* place any test configures these two mocks, so the "~15 test files" concern didn't apply; one shared helper needed updating, not many. Rather than threading `IJSObjectReference` through a bare-function-plus-invoke-trick, `addResizeListener`/`addKeyboardListener` (`DiagramCanvas.razor.js`) now each return a small object with a named `dispose` method instead of a bare closure - a real, named-method object reference marshals cleanly over JS interop, where a bare function reference does not. `_cleanupFunctions` (renamed `_cleanupHandles`) now holds `List<IJSObjectReference>`; `DisposeAsync` awaits `handle.InvokeVoidAsync("dispose")` then `handle.DisposeAsync()` for each, and is now genuinely async throughout rather than a fire-and-forget `ForEach`. `SetupDiagramCanvasJsModule` mocks the new shape via `module.SetupModule("addResizeListener"/"addKeyboardListener", _ => true)` (bUnit's own mechanism for a call that itself returns a further-mockable `IJSObjectReference`), with `SetupVoid("dispose", ...)` on each. Verified as a genuine regression test (`DiagramCanvasDisposalTests`): confirmed red against the pre-fix code (throws during `OnAfterRenderAsync` itself, since the old mock's `Action` result no longer matches the new `IJSObjectReference`-shaped setup) before restoring the fix.
