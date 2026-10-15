using D12Canvas.Panel;
using Xunit;

namespace D12Canvas.Tests;

// Every role in the closed set declares what it expects, and the validator checks a property
// against its own role's declaration rather than against another registered type.
public class PropertyRolesTests
{
    public static IEnumerable<object[]> EveryRole() =>
        Enum.GetValues<PropertyRole>().Select(role => new object[] { role });

    [Theory]
    [MemberData(nameof(EveryRole))]
    public void EveryRoleDeclaresAKindATypeAndALabel(PropertyRole role)
    {
        var declaration = PropertyRoleDeclarations.For(role);

        Assert.NotEqual(EditorKind.Custom, declaration.Kind);
        Assert.NotNull(declaration.ClrType);
        Assert.False(string.IsNullOrWhiteSpace(declaration.Label));
    }

    [Fact]
    public void APropertyMatchingItsRoleValidates()
    {
        var schema = EditablePropertySchema.DiscoverFrom(typeof(PropsWithMatchingRole));

        var exception = Record.Exception(() => PropertyRoleValidator.Validate(schema));

        Assert.Null(exception);
        Assert.Equal(PropertyRole.Fill, Assert.Single(schema).Role);
    }

    [Fact]
    public void APropertyWhoseKindDisagreesWithItsRoleThrowsNamingTheProperty()
    {
        var schema = EditablePropertySchema.DiscoverFrom(typeof(PropsWithRoleKindMismatch));

        var exception = Assert.Throws<PropertyRoleMismatchException>(
            () => PropertyRoleValidator.Validate(schema)
        );

        Assert.Equal(typeof(PropsWithRoleKindMismatch), exception.PropsType);
        Assert.Equal(nameof(PropsWithRoleKindMismatch.Tint), exception.PropertyName);
        Assert.Equal(PropertyRole.Fill, exception.Role);
        Assert.Equal(EditorKind.Color, exception.ExpectedKind);
        Assert.Equal(EditorKind.Text, exception.ActualKind);
        Assert.Contains(nameof(PropsWithRoleKindMismatch), exception.Message);
        Assert.Contains(nameof(PropsWithRoleKindMismatch.Tint), exception.Message);
    }

    [Fact]
    public void APropertyWhoseClrTypeDisagreesWithItsRoleThrowsNamingTheProperty()
    {
        var schema = EditablePropertySchema.DiscoverFrom(typeof(PropsWithRoleClrTypeMismatch));

        var exception = Assert.Throws<PropertyRoleMismatchException>(
            () => PropertyRoleValidator.Validate(schema)
        );

        Assert.Equal(typeof(PropsWithRoleClrTypeMismatch), exception.PropsType);
        Assert.Equal(nameof(PropsWithRoleClrTypeMismatch.Tint), exception.PropertyName);
        Assert.Equal(typeof(string), exception.ExpectedType);
        Assert.Equal(typeof(int), exception.ActualType);
    }

    [Fact]
    public void ATypeDeclaringOneRoleOnTwoPropertiesThrowsNamingBoth()
    {
        var schema = EditablePropertySchema.DiscoverFrom(typeof(PropsDeclaringFillTwice));

        var exception = Assert.Throws<PropertyRoleDeclaredTwiceException>(
            () => PropertyRoleValidator.Validate(schema)
        );

        Assert.Equal(typeof(PropsDeclaringFillTwice), exception.PropsType);
        Assert.Equal(PropertyRole.Fill, exception.Role);
        Assert.Equal(nameof(PropsDeclaringFillTwice.Background), exception.FirstPropertyName);
        Assert.Equal(nameof(PropsDeclaringFillTwice.Tint), exception.SecondPropertyName);
    }

    [Fact]
    public void ARolelessPropertyIsNeverChecked()
    {
        var schema = EditablePropertySchema.DiscoverFrom(typeof(TestProps));

        var exception = Record.Exception(() => PropertyRoleValidator.Validate(schema));

        Assert.Null(exception);
    }

    private sealed record PropsWithMatchingRole(
        [property: PanelEditable(EditorKind.Color, PropertyRole.Fill)] string Tint = ""
    );

    private sealed record PropsDeclaringFillTwice(
        [property: PanelEditable(EditorKind.Color, PropertyRole.Fill)] string Background = "",
        [property: PanelEditable(EditorKind.Color, PropertyRole.Fill)] string Tint = ""
    );
}
