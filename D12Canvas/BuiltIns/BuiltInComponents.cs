using D12Canvas.Registration;

namespace D12Canvas.BuiltIns;

// Registers D12Canvas's own shipped component types through the exact same RegisterComponent
// mechanism a host uses for its own types (ADR 0001) - there is no separate "built-in" path. Called
// once from ServiceCollectionExtensions.AddD12Canvas, after the host's own configure callback has
// already run, so built-ins are appended after any host-registered types.
internal static class BuiltInComponents
{
    public static void RegisterAll(D12CanvasOptions options)
    {
        options.RegisterComponent<Rectangle, RectangleProps>(
            "rectangle",
            builder =>
            {
                builder.DisplayName = "Rectangle";
                builder.AccessibleName = "Rectangle";
                builder.DefaultProps = new RectangleProps("#FFFFFF", "#333333", 2);
                builder.Icon = "▭";
                builder.Category = "Basic Shapes";
                builder.DefaultSize = new ComponentSize(160, 100);
            }
        );
    }
}
