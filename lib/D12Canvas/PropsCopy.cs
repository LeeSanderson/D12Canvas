using System.Reflection;

namespace D12Canvas;

internal static class PropsCopy
{
    private static readonly MethodInfo MemberwiseCloneMethod = typeof(object).GetMethod(
        "MemberwiseClone",
        BindingFlags.NonPublic | BindingFlags.Instance
    )!;

    public static object With(object props, PropertyInfo property, object? value) =>
        With(props, [(property, value)]);

    public static object With(
        object props,
        IEnumerable<(PropertyInfo Property, object? Value)> changes
    )
    {
        var clone = MemberwiseCloneMethod.Invoke(props, null)!;
        foreach (var (property, value) in changes)
        {
            property.SetValue(clone, value);
        }

        return clone;
    }
}
