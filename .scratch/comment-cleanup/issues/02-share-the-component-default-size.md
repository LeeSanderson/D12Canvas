# Share ComponentContainer's default size with DiagramCanvas

Status: ready-for-agent

`DiagramCanvas.FallbackWidth` and `FallbackHeight` in `lib/D12Canvas/DiagramCanvas.razor.cs` hard-code 200×150 to match the default `Width` and `Height` parameters of `ComponentContainer` (`lib/D12Canvas/ComponentContainer.razor.cs`). Nothing keeps the two in step.

## Fix

Declare the size once, for example as `ComponentContainer.DefaultWidth` and `DefaultHeight`, and use it in both places.
