using D12Canvas.Model;
using D12Canvas.Registration;

namespace D12Canvas.Persistence;

internal static class ReferencedAssets
{
    public static IEnumerable<string> IdsReferencedBy(
        IEnumerable<ComponentInstance> instances,
        IComponentRegistry registry
    )
    {
        foreach (var instance in instances)
        {
            var registration = registry.All.FirstOrDefault(candidate =>
                candidate.Key == instance.ComponentTypeKey
            );
            if (registration is null)
            {
                continue;
            }

            foreach (
                var (_, assetId) in AssetReferenceSchema.ReferencesIn(registration, instance.Props)
            )
            {
                yield return assetId;
            }
        }
    }
}
