using System.Text;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace D12Canvas.Tests;

// A props type with one declared asset reference beside an ordinary string, standing in for any
// host component that shows stored content: it writes Src straight into an <img>, exactly as an
// author who has never heard of assets would, and commits an edit of its caption back through the
// canvas the way the editing built-ins do.
internal sealed record AssetTestProps(
    [property: AssetReference] string Src = "",
    string Caption = ""
);

internal sealed class AssetTestComponent : ComponentBase
{
    public const string Key = "asset-component";
    public static readonly byte[] PngBytes = Encoding.ASCII.GetBytes("not really a png");

    public static void Register(D12CanvasOptions options) =>
        options.RegisterComponent<AssetTestComponent, AssetTestProps>(
            Key,
            builder =>
            {
                builder.DisplayName = "Asset component";
                builder.AccessibleName = "Asset component";
                builder.DefaultProps = new AssetTestProps();
            }
        );

    [Parameter]
    public AssetTestProps Props { get; set; } = new();

    [CascadingParameter(Name = "ParentCanvas")]
    private DiagramCanvas? ParentCanvas { get; set; }

    [CascadingParameter(Name = "InstanceId")]
    private Guid InstanceId { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "img");
        builder.AddAttribute(1, "class", "asset-test-image");
        builder.AddAttribute(2, "src", Props.Src);
        builder.AddAttribute(3, "alt", Props.Caption);
        builder.CloseElement();
        builder.OpenElement(4, "button");
        builder.AddAttribute(5, "class", "asset-test-edit");
        builder.AddAttribute(
            6,
            "onclick",
            EventCallback.Factory.Create(
                this,
                () =>
                    ParentCanvas?.CommitPropsChange(
                        InstanceId,
                        Props,
                        Props with
                        {
                            Caption = "edited",
                        }
                    )
            )
        );
        builder.CloseElement();
    }
}
