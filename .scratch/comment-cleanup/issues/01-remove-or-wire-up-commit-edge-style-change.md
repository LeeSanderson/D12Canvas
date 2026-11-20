# Remove or wire up DiagramCanvas.CommitEdgeStyleChange

Status: ready-for-agent

`DiagramCanvas.CommitEdgeStyleChange` in `lib/D12Canvas/DiagramCanvas.razor.cs` is public, but no UI calls it. The property bar uses `CommitEdgeStyleChangeBatch` in `DiagramCanvas.PropertyBar.cs`. Only `DiagramCanvasEdgeColourTests`, `DiagramCanvasEdgeRoutingAndArrowheadsTests` and `DiagramCanvasLockingTests` call the single-edge method.

## Fix

Move those tests to `CommitEdgeStyleChangeBatch` with a one-item list and delete `CommitEdgeStyleChange`. If a host should be able to restyle one edge, keep it as host API instead and say so in `CONTEXT.md`.
