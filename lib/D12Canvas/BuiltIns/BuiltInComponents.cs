using D12Canvas.Panel;
using D12Canvas.Registration;

namespace D12Canvas.BuiltIns;

internal static class BuiltInComponents
{
    private static readonly IReadOnlyDictionary<string, Glyph> TextAlignGlyphs = new Dictionary<
        string,
        Glyph
    >
    {
        ["left"] = Glyphs.TextAlignLeft,
        ["center"] = Glyphs.TextAlignCentre,
        ["right"] = Glyphs.TextAlignRight,
    };

    private static readonly IReadOnlyDictionary<string, Glyph> VerticalAlignGlyphs = new Dictionary<
        string,
        Glyph
    >
    {
        ["top"] = Glyphs.TextVerticalTop,
        ["middle"] = Glyphs.TextVerticalMiddle,
        ["bottom"] = Glyphs.TextVerticalBottom,
    };

    private static readonly IReadOnlyDictionary<string, Glyph> FontWeightGlyphs = new Dictionary<
        string,
        Glyph
    >
    {
        ["normal"] = Glyphs.WeightNormal,
        ["bold"] = Glyphs.WeightBold,
    };

    private static IReadOnlyList<EditableProperty> WithOptionGlyphs(
        Type propsType,
        params (string PropertyName, IReadOnlyDictionary<string, Glyph> Glyphs)[] optionGlyphs
    ) =>
        EditablePropertySchema
            .DiscoverFrom(propsType)
            .Select(property =>
                optionGlyphs.FirstOrDefault(entry => entry.PropertyName == property.Property.Name)
                    is { Glyphs: { } glyphs }
                    ? property with
                    {
                        OptionGlyphs = glyphs,
                    }
                    : property
            )
            .ToList();

    public static void RegisterAll(D12CanvasOptions options)
    {
        options.RegisterComponent<Rectangle, RectangleProps>(
            "rectangle",
            builder =>
            {
                builder.DisplayName = "Rectangle";
                builder.AccessibleName = "Rectangle";
                builder.DefaultProps = new RectangleProps(null, null, 2);
                builder.Icon = Glyphs.Rectangle;
                builder.Category = "Basic Shapes";
                builder.DefaultSize = new ComponentSize(160, 100);
            }
        );

        options.RegisterComponent<StickyNote, StickyNoteProps>(
            "sticky-note",
            builder =>
            {
                builder.DisplayName = "Sticky Note";
                builder.AccessibleName = "Sticky Note";
                builder.DefaultProps = new StickyNoteProps(
                    "",
                    "#FFEB3B",
                    "#000000",
                    14,
                    "center",
                    "middle"
                );
                builder.Icon = Glyphs.StickyNote;
                builder.Category = "Basic Shapes";
                builder.DefaultSize = new ComponentSize(200, 200);
                builder.EditableProperties = WithOptionGlyphs(
                    typeof(StickyNoteProps),
                    (nameof(StickyNoteProps.TextAlign), TextAlignGlyphs),
                    (nameof(StickyNoteProps.VerticalAlign), VerticalAlignGlyphs)
                );
            }
        );

        options.RegisterComponent<Text, TextProps>(
            "text",
            builder =>
            {
                builder.DisplayName = "Text";
                builder.AccessibleName = "Text";
                builder.DefaultProps = new TextProps("", null, 16, "normal", "left");
                builder.Icon = Glyphs.Text;
                builder.Category = "Basic Shapes";
                builder.DefaultSize = new ComponentSize(200, 40);
                builder.IsEmpty = props => string.IsNullOrWhiteSpace(props.Text);
                builder.EditableProperties = WithOptionGlyphs(
                    typeof(TextProps),
                    (nameof(TextProps.FontWeight), FontWeightGlyphs),
                    (nameof(TextProps.TextAlign), TextAlignGlyphs)
                );
            }
        );

        options.RegisterComponent<Image, ImageProps>(
            ImagePicture.ComponentKey,
            builder =>
            {
                builder.DisplayName = "Image";
                builder.AccessibleName = "Image";
                builder.DefaultProps = new ImageProps("", "", "cover");
                builder.Icon = Glyphs.Image;
                builder.Category = "Basic Shapes";
                builder.DefaultSize = new ComponentSize(240, 180);
                builder.EditableProperties =
                [
                    new EditableProperty(
                        typeof(ImageProps).GetProperty(nameof(ImageProps.Url))!,
                        EditorKind.Custom,
                        CustomEditor: context =>
                            builder =>
                            {
                                builder.OpenComponent<ImagePictureEditor>(0);
                                builder.AddComponentParameter(
                                    1,
                                    nameof(ImagePictureEditor.Context),
                                    context
                                );
                                builder.CloseComponent();
                            }
                    ),
                    .. EditablePropertySchema.DiscoverFrom(typeof(ImageProps)),
                ];
            }
        );
    }
}
