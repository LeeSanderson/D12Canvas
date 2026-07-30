using D12Canvas.Panel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace D12Canvas.Tests;

// A minimal RenderFragment<CustomEditorContext> fixture for PropertyPanelTests - a single button
// that commits a fixed value on click, hand-built via RenderTreeBuilder (D12Canvas.Tests is a
// plain classlib, not Razor SDK, so there's no .razor template syntax available here).
internal static class PanelTestCustomEditor
{
    public const string CommitButtonId = "panel-test-custom-editor-commit";
    public const string CommittedValue = "custom-committed";

    public static RenderFragment<CustomEditorContext> Fragment =>
        context =>
            builder =>
            {
                builder.OpenElement(0, "button");
                builder.AddAttribute(1, "type", "button");
                builder.AddAttribute(2, "id", CommitButtonId);
                builder.AddAttribute(
                    3,
                    "onclick",
                    EventCallback.Factory.Create(context, () => context.Commit(CommittedValue))
                );
                builder.CloseElement();
            };
}
