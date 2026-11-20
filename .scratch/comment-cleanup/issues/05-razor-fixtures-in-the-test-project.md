# Let D12Canvas.Tests author .razor fixtures

Status: ready-for-agent

`test/D12Canvas.Tests/PanelTestCustomEditor.cs` builds its `RenderFragment<CustomEditorContext>` by hand through `RenderTreeBuilder`. It has to, because `D12Canvas.Tests` uses `Microsoft.NET.Sdk` rather than `Microsoft.NET.Sdk.Razor`, so `.razor` syntax isn't available.

## Fix

Switch the test project to the Razor SDK and rewrite the fixture as a `.razor` component. Check that bUnit discovery, the csharpier build step and the linked `GlyphGalleryEntries.cs` still work.
