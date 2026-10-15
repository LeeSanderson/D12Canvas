using System.Diagnostics.CodeAnalysis;

namespace D12Canvas.Model;

// How a declared props property points at an Asset: the literal prefix followed by the asset id.
// The prefix is what tells a reference apart from an ordinary URL held in the same property.
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
