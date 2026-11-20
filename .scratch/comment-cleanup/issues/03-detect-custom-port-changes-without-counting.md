# Detect CustomPorts changes without comparing counts

Status: ready-for-agent

`ComponentContainer.ShouldRender` in `lib/D12Canvas/ComponentContainer.razor.cs` decides whether custom ports changed by comparing `CustomPorts.Count` with the count it last rendered. It can't compare references, because `ComponentInstance.CustomPorts` is one mutable `List<PortDef>` that lives across renders.

A count misses any change that keeps the count: a port removed and another added between two renders, or a port moved in place once ports can move.

## Fix

Either make `ComponentInstance.CustomPorts` an immutable list that every port command replaces, so `ShouldRender` compares references, or give the instance a port revision that every port command bumps and compare that. Add a bUnit test that removes one port and adds another between two renders and expects the container to re-render.
