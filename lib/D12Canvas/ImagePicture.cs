using System.Text.RegularExpressions;
using D12Canvas.BuiltIns;
using D12Canvas.Model;
using D12Canvas.Registration;

namespace D12Canvas;

internal static partial class ImagePicture
{
    public const string ComponentKey = "image";

    public static bool IsImage(ComponentInstance? instance) =>
        instance is { ComponentTypeKey: ComponentKey, Props: ImageProps };

    public static bool IsEmptyImage(ComponentInstance? instance) =>
        IsImage(instance) && string.IsNullOrWhiteSpace(((ImageProps)instance!.Props).Url);

    public static ComponentInstance? FillTarget(Board board, IEnumerable<Guid> hitsTopmostFirst)
    {
        foreach (var id in hitsTopmostFirst)
        {
            if (board.GetComponent(id) is { } instance)
            {
                return IsEmptyImage(instance) && !instance.Locked ? instance : null;
            }

            if (board.GetEdge(id) is not null || board.FindEdgeLabel(id) is not null)
            {
                return null;
            }
        }

        return null;
    }

    public static ComponentSize SizeFor(
        double pixelWidth,
        double pixelHeight,
        Bounds viewport,
        ComponentSize fallback
    )
    {
        if (
            !double.IsFinite(pixelWidth)
            || !double.IsFinite(pixelHeight)
            || pixelWidth <= 0
            || pixelHeight <= 0
        )
        {
            return fallback;
        }

        var scale = Math.Min(
            1,
            Math.Min(viewport.Width / 2 / pixelWidth, viewport.Height / 2 / pixelHeight)
        );
        return new ComponentSize(pixelWidth * scale, pixelHeight * scale);
    }

    public static bool IsAcceptedMimeType(string? mimeType) =>
        mimeType is not null && ImageMimeType().IsMatch(mimeType);

    public static object WithUrl(object props, string url) =>
        props is ImageProps image ? image with { Url = url } : props;

    [GeneratedRegex("^image/[a-z0-9][a-z0-9.+-]*$", RegexOptions.IgnoreCase)]
    private static partial Regex ImageMimeType();
}
