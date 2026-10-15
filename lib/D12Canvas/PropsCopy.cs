using System.Reflection;

namespace D12Canvas;

// A TProps record is immutable and only ever seen here as an opaque object, so there is no
// compile-time type to write a `with` expression against. MemberwiseClone plus reflected
// overwrites is the generic equivalent: every other field is copied byte-for-byte and the
// original object is left untouched, which is what lets it stand as a command's "before".
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
