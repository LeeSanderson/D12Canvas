namespace D12Canvas.Registration;

// Declares that a string TProps property may hold an asset reference ("asset:<id>") in place of
// an ordinary URL. DiagramCanvas swaps the reference for a data: URI before binding props, so the
// component itself only ever sees a URL. Declared by the author, never inferred from a property's
// name or value; any other value in the property is left exactly as written.
[AttributeUsage(AttributeTargets.Property)]
public sealed class AssetReferenceAttribute : Attribute;
