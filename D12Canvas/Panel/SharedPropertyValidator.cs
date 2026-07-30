namespace D12Canvas.Panel;

// ADR 0008/ticket 59: run once per registration, comparing the newly-registered type's own
// EditableProperties against every property panel schema already registered under any other type.
// A mismatch is a registration-time error ("not a silent merge") - checking incrementally against
// whatever is already registered is enough to catch every pair, regardless of which of the two
// types happens to register first.
public static class SharedPropertyValidator
{
    public static void ValidateAgainstExisting(
        IReadOnlyList<EditableProperty> newProperties,
        IEnumerable<IReadOnlyList<EditableProperty>> existingSchemas
    )
    {
        foreach (var newProperty in newProperties)
        {
            if (newProperty.SharedTag is null)
            {
                continue;
            }

            foreach (var existingSchema in existingSchemas)
            {
                foreach (var existingProperty in existingSchema)
                {
                    if (existingProperty.SharedTag != newProperty.SharedTag)
                    {
                        continue;
                    }

                    if (
                        existingProperty.Kind != newProperty.Kind
                        || existingProperty.Property.PropertyType
                            != newProperty.Property.PropertyType
                    )
                    {
                        throw new SharedPropertyMismatchException(
                            newProperty.SharedTag,
                            existingProperty.Property.DeclaringType!,
                            existingProperty.Property.Name,
                            newProperty.Property.DeclaringType!,
                            newProperty.Property.Name
                        );
                    }
                }
            }
        }
    }
}
