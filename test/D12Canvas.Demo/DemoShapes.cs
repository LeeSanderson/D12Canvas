using D12Canvas.Demo.Components;
using D12Canvas.Registration;

namespace D12Canvas.Demo;

// Enough host types to fill the Palette rail and send the rest to its "More components" flyout.
internal static class DemoShapes
{
    private static readonly (string Key, string Name, string Category, string Path)[] Shapes =
    [
        ("demo-process", "Process", "Flowchart", "M2.75 4.75h10.5v6.5H2.75z"),
        ("demo-decision", "Decision", "Flowchart", "M8 2.75 13.25 8 8 13.25 2.75 8z"),
        (
            "demo-terminator",
            "Terminator",
            "Flowchart",
            "M5.75 4.75h4.5a3.25 3.25 0 0 1 0 6.5h-4.5a3.25 3.25 0 0 1 0-6.5z"
        ),
        ("demo-data", "Data", "Flowchart", "M5.25 3.75h8l-2.5 8.5h-8z"),
        ("demo-document", "Document", "Flowchart", "M2.75 2.75h10.5v9c-3.5-2-7 2-10.5 0z"),
        ("demo-callout", "Callout", "Annotations", "M2.75 3.25h10.5v7h-6l-3 2.5v-2.5h-1.5z"),
        ("demo-comment", "Comment", "Annotations", "M2.75 4.25h10.5 M2.75 8h10.5 M2.75 11.75h6"),
        (
            "demo-highlight",
            "Highlight",
            "Annotations",
            "M9.75 2.75l3.5 3.5-6 6h-3.5v-3.5z M2.75 13.25h10.5"
        ),
    ];

    public static void RegisterAll(D12CanvasOptions options)
    {
        foreach (var (key, name, category, path) in Shapes)
        {
            options.RegisterComponent<DemoShapeComponent, DemoShapeProps>(
                key,
                builder =>
                {
                    builder.DisplayName = name;
                    builder.AccessibleName = name;
                    builder.DefaultProps = new DemoShapeProps(name);
                    builder.DefaultSize = new ComponentSize(140, 80);
                    builder.Icon = new Glyph(new GlyphPart(path));
                    builder.Category = category;
                }
            );
        }
    }
}
