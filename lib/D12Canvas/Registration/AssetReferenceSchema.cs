using System.Reflection;
using D12Canvas.Model;

namespace D12Canvas.Registration;

public static class AssetReferenceSchema
{
    public static IReadOnlyList<PropertyInfo> DiscoverFrom(Type propsType)
    {
        var properties = propsType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetCustomAttribute<AssetReferenceAttribute>() is not null)
            .ToList();

        foreach (var property in properties)
        {
            if (property.PropertyType != typeof(string))
            {
                throw new AssetReferenceStringRequiredException(
                    property.DeclaringType!,
                    property.Name
                );
            }
        }

        return properties;
    }

    public static IEnumerable<(PropertyInfo Property, string AssetId)> ReferencesIn(
        ComponentRegistration registration,
        object props
    )
    {
        foreach (var property in registration.AssetReferences ?? [])
        {
            if (AssetReference.TryGetAssetId(property.GetValue(props) as string, out var assetId))
            {
                yield return (property, assetId);
            }
        }
    }
}
