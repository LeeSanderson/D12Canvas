using System.Reflection;
using D12Canvas.History;
using D12Canvas.Model;

namespace D12Canvas.Panel;

internal static class TargetedEdit
{
    public static object? Normalise(EditorKind kind, bool canHoldNull, object? value) =>
        kind == EditorKind.Color && canHoldNull && value is "" ? null : value;

    public static IReadOnlyList<PropsChange> ChangesFor(
        IEnumerable<(ComponentInstance Instance, PropertyInfo Property)> targets,
        EditorKind kind,
        object? value
    ) =>
        targets
            .Where(target =>
                !MixedValue.AreEqual(kind, target.Property.GetValue(target.Instance.Props), value)
            )
            .Select(target => new PropsChange(
                target.Instance.Id,
                target.Instance.Props,
                PropsCopy.With(target.Instance.Props, target.Property, value)
            ))
            .ToList();
}
