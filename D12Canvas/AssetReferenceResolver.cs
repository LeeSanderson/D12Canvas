using System.Reflection;
using System.Runtime.CompilerServices;
using D12Canvas.Model;
using D12Canvas.Registration;

namespace D12Canvas;

// Hands back the props object a component binds: the instance's own Props when its type declares
// no asset reference, otherwise a copy in which every reference that resolves has been swapped
// for the asset's data: URI. Cached by props reference identity, which is exact because
// MutateEntityCommand swaps Props wholesale rather than mutating in place. A copy that still
// holds an unresolved reference is never cached, so an asset arriving later takes effect on the
// next render with no invalidation signal.
internal sealed class AssetReferenceResolver
{
    private readonly ConditionalWeakTable<object, object> _resolvedByProps = new();

    public object Resolve(
        ComponentInstance instance,
        ComponentRegistration registration,
        Board board
    )
    {
        if (registration.AssetReferences is not { Count: > 0 })
        {
            return instance.Props;
        }

        var props = instance.Props;
        if (_resolvedByProps.TryGetValue(props, out var resolved))
        {
            return resolved;
        }

        var swaps = new List<(PropertyInfo Property, object? Value)>();
        var anyUnresolved = false;
        foreach (var (property, assetId) in AssetReferenceSchema.ReferencesIn(registration, props))
        {
            if (board.GetAsset(assetId) is { } asset)
            {
                swaps.Add((property, asset.DataUri));
            }
            else
            {
                anyUnresolved = true;
            }
        }

        resolved = swaps.Count == 0 ? props : PropsCopy.With(props, swaps);
        if (!anyUnresolved)
        {
            _resolvedByProps.AddOrUpdate(props, resolved);
        }

        return resolved;
    }

    // A component that commits an edit hands the canvas the props object it was bound with, which
    // for a type with a declared reference is the resolved copy. Recognise that copy by its data
    // URIs, take the committed object as the command's "before", and put every reference the edit
    // did not touch back into "after", so the bytes never travel into the board as a data URI.
    public (object Before, object After) Unresolve(
        ComponentInstance instance,
        ComponentRegistration registration,
        Board board,
        object before,
        object after
    )
    {
        var committed = instance.Props;
        if (ReferenceEquals(before, committed))
        {
            return (before, after);
        }

        var swapped = AssetReferenceSchema
            .ReferencesIn(registration, committed)
            .Select(reference =>
                (
                    reference.Property,
                    Reference: reference.Property.GetValue(committed),
                    Asset: board.GetAsset(reference.AssetId)
                )
            )
            .Where(entry => entry.Asset is not null)
            .ToList();

        var beforeIsTheBoundCopy =
            swapped.Count > 0
            && swapped.All(entry => Equals(entry.Property.GetValue(before), entry.Asset!.DataUri));
        if (!beforeIsTheBoundCopy)
        {
            return (before, after);
        }

        var restores = swapped
            .Where(entry => Equals(entry.Property.GetValue(after), entry.Asset!.DataUri))
            .Select(entry => (entry.Property, entry.Reference))
            .ToList();

        return (committed, restores.Count == 0 ? after : PropsCopy.With(after, restores));
    }
}
