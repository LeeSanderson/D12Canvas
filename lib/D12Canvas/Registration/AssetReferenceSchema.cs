using System.Reflection;
using D12Canvas.Model;

namespace D12Canvas.Registration;

// Finds the [AssetReference] properties of a TProps type in the same registration-time pass that
// finds its [PanelEditable] ones, so a bad declaration fails where it is written, and reads the
// references a props object currently holds through them.
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

    // Every declared property of the registration whose value in these props is a reference,
    // paired with the asset id it names. A declared property holding an ordinary URL is skipped.
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
