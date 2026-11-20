using System.Diagnostics.CodeAnalysis;

namespace D12Canvas.Model;

public static class AssetReference
{
    public const string Prefix = "asset:";

    public static string Format(string assetId) => Prefix + assetId;

    public static bool TryGetAssetId(string? value, [NotNullWhen(true)] out string? assetId)
    {
        if (value is not null && value.StartsWith(Prefix, StringComparison.Ordinal))
        {
            assetId = value[Prefix.Length..];
            return true;
        }

        assetId = null;
        return false;
    }
}
