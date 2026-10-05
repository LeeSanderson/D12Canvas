using System.Linq;
using D12Canvas.Panel;
using D12Canvas.Registration;
using Xunit;

namespace D12Canvas.Tests;

public class D12CanvasOptionsTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RegisterComponentWithAnEmptyOrWhitespaceKeyThrows(string key)
    {
        var options = new D12CanvasOptions();

        Assert.Throws<ArgumentException>(
            () =>
                options.RegisterComponent<TestComponentDouble, TestProps>(
                    key,
                    builder =>
                    {
                        builder.DisplayName = "Widget";
                        builder.AccessibleName = "Widget";
                        builder.DefaultProps = new TestProps();
                    }
                )
        );
    }

    [Fact]
    public void RegisterComponentWithoutDisplayNameThrowsNamingTheMissingField()
    {
        var options = new D12CanvasOptions();

        var exception = Assert.Throws<ComponentRegistrationException>(
            () =>
                options.RegisterComponent<TestComponentDouble, TestProps>(
                    "widget",
                    builder =>
                    {
                        builder.AccessibleName = "Widget";
                        builder.DefaultProps = new TestProps();
                    }
                )
        );

        Assert.Equal("widget", exception.Key);
        Assert.Equal(
            nameof(ComponentRegistrationBuilder<TestProps>.DisplayName),
            exception.MissingField
        );
    }

    [Fact]
    public void RegisterComponentWithoutAccessibleNameThrowsNamingTheMissingField()
    {
        var options = new D12CanvasOptions();

        var exception = Assert.Throws<ComponentRegistrationException>(
            () =>
                options.RegisterComponent<TestComponentDouble, TestProps>(
                    "widget",
                    builder =>
                    {
                        builder.DisplayName = "Widget";
                        builder.DefaultProps = new TestProps();
                    }
                )
        );

        Assert.Equal("widget", exception.Key);
        Assert.Equal(
            nameof(ComponentRegistrationBuilder<TestProps>.AccessibleName),
            exception.MissingField
        );
    }

    [Fact]
    public void RegisterComponentWithoutDefaultPropsThrowsNamingTheMissingField()
    {
        var options = new D12CanvasOptions();

        var exception = Assert.Throws<ComponentRegistrationException>(
            () =>
                options.RegisterComponent<TestComponentDouble, TestProps>(
                    "widget",
                    builder =>
                    {
                        builder.DisplayName = "Widget";
                        builder.AccessibleName = "Widget";
                    }
                )
        );

        Assert.Equal("widget", exception.Key);
        Assert.Equal(
            nameof(ComponentRegistrationBuilder<TestProps>.DefaultProps),
            exception.MissingField
        );
    }

    [Fact]
    public void RegisterComponentDefaultsRoleToGroupWhenNotSpecified()
    {
        var options = new D12CanvasOptions();

        options.RegisterComponent<TestComponentDouble, TestProps>(
            "widget",
            builder =>
            {
                builder.DisplayName = "Widget";
                builder.AccessibleName = "Widget";
                builder.DefaultProps = new TestProps();
            }
        );

        var registration = options.Registry.Resolve("widget");
        Assert.Equal("group", registration.Role);
    }

    [Fact]
    public void RegisterComponentStoresOptionalMetadataWhenSpecified()
    {
        var options = new D12CanvasOptions();
        var defaultProps = new TestProps("hello");

        options.RegisterComponent<TestComponentDouble, TestProps>(
            "widget",
            builder =>
            {
                builder.DisplayName = "Widget";
                builder.AccessibleName = "Widget";
                builder.DefaultProps = defaultProps;
                builder.Icon = "widget-icon";
                builder.Role = "img";
                builder.DefaultSize = new ComponentSize(200, 150);
                builder.Category = "Basic Shapes";
            }
        );

        var registration = options.Registry.Resolve("widget");
        Assert.Equal("widget-icon", registration.Icon);
        Assert.Equal("img", registration.Role);
        Assert.Equal(new ComponentSize(200, 150), registration.DefaultSize);
        Assert.Equal("Basic Shapes", registration.Category);
        Assert.Same(defaultProps, registration.DefaultProps);
    }

    [Fact]
    public void RegisterComponentKeyIsDecoupledFromTheClrTypeName()
    {
        var options = new D12CanvasOptions();

        options.RegisterComponent<TestComponentDouble, TestProps>(
            "totally-unrelated-key",
            builder =>
            {
                builder.DisplayName = "Widget";
                builder.AccessibleName = "Widget";
                builder.DefaultProps = new TestProps();
            }
        );

        var registration = options.Registry.Resolve("totally-unrelated-key");
        Assert.Equal(typeof(TestComponentDouble), registration.ComponentType);
    }

    [Fact]
    public void RegisterComponentDefaultsToNoEditablePropertiesWhenNoneAreDeclared()
    {
        var options = new D12CanvasOptions();

        options.RegisterComponent<TestComponentDouble, TestProps>(
            "widget",
            builder =>
            {
                builder.DisplayName = "Widget";
                builder.AccessibleName = "Widget";
                builder.DefaultProps = new TestProps();
            }
        );

        Assert.Empty(options.Registry.Resolve("widget").EditableProperties!);
    }

    // Editable properties default to whatever a TProps record's own [PanelEditable] attributes
    // declare - PanelTestProps.Content carries none (it's this type's stand-in for a Text-type
    // *content* field, excluded the same way StickyNoteProps.Text is), so only its five
    // [PanelEditable]-carrying fields (one per EditorKind) should surface here.
    [Fact]
    public void RegisterComponentDiscoversEditablePropertiesFromPanelEditableAttributesByDefault()
    {
        var options = new D12CanvasOptions();

        options.RegisterComponent<TestComponentDouble, PanelTestProps>(
            "widget",
            builder =>
            {
                builder.DisplayName = "Widget";
                builder.AccessibleName = "Widget";
                builder.DefaultProps = new PanelTestProps("", "", 0);
            }
        );

        var editableProperties = options.Registry.Resolve("widget").EditableProperties!;

        Assert.Equal(5, editableProperties.Count);
        Assert.Contains(
            editableProperties,
            p => p.Property.Name == nameof(PanelTestProps.Label) && p.Kind == EditorKind.Text
        );
        Assert.Contains(
            editableProperties,
            p => p.Property.Name == nameof(PanelTestProps.Count) && p.Kind == EditorKind.Number
        );
        Assert.Contains(
            editableProperties,
            p => p.Property.Name == nameof(PanelTestProps.Tint) && p.Kind == EditorKind.Color
        );
        Assert.Contains(
            editableProperties,
            p => p.Property.Name == nameof(PanelTestProps.Flag) && p.Kind == EditorKind.Checkbox
        );
        Assert.Contains(
            editableProperties,
            p =>
                p.Property.Name == nameof(PanelTestProps.Mode)
                && p.Kind == EditorKind.Dropdown
                && p.Options!.SequenceEqual(["a", "b", "c"])
        );
        Assert.DoesNotContain(
            editableProperties,
            p => p.Property.Name == nameof(PanelTestProps.Content)
        );
    }

    // A Dropdown-kind property with no choices can't render a usable <select>, so this is caught
    // at registration time rather than surfacing as an empty control in the panel.
    [Fact]
    public void RegisterComponentWithADropdownPropertyMissingOptionsThrowsNamingTheProperty()
    {
        var options = new D12CanvasOptions();

        var exception = Assert.Throws<DropdownOptionsRequiredException>(
            () =>
                options.RegisterComponent<TestComponentDouble, PropsWithMissingDropdownOptions>(
                    "widget",
                    builder =>
                    {
                        builder.DisplayName = "Widget";
                        builder.AccessibleName = "Widget";
                        builder.DefaultProps = new PropsWithMissingDropdownOptions();
                    }
                )
        );

        Assert.Equal(typeof(PropsWithMissingDropdownOptions), exception.PropsType);
        Assert.Equal(nameof(PropsWithMissingDropdownOptions.Mode), exception.PropertyName);
    }

    // EditorKind.Custom needs a RenderFragment, which an attribute argument can never supply
    // (attributes require compile-time constants) - so declaring it via [PanelEditable] alone is
    // always a registration-time error; a Custom-kind property must come from the builder's
    // EditableProperties override instead.
    [Fact]
    public void RegisterComponentWithACustomKindPropertyDeclaredViaAttributeThrowsNamingTheProperty()
    {
        var options = new D12CanvasOptions();

        var exception = Assert.Throws<CustomEditorRequiredException>(
            () =>
                options.RegisterComponent<TestComponentDouble, PropsWithCustomEditorAttribute>(
                    "widget",
                    builder =>
                    {
                        builder.DisplayName = "Widget";
                        builder.AccessibleName = "Widget";
                        builder.DefaultProps = new PropsWithCustomEditorAttribute();
                    }
                )
        );

        Assert.Equal(typeof(PropsWithCustomEditorAttribute), exception.PropsType);
        Assert.Equal(nameof(PropsWithCustomEditorAttribute.Value), exception.PropertyName);
    }

    // "Attributes set the default schema, the builder is the escape hatch" - setting
    // EditableProperties replaces whatever attribute discovery would otherwise have produced.
    [Fact]
    public void RegisterComponentBuilderOverridesTheAttributeDeclaredEditableSchema()
    {
        var options = new D12CanvasOptions();
        var overrideSchema = new List<EditableProperty>
        {
            new(
                typeof(PanelTestProps).GetProperty(nameof(PanelTestProps.Content))!,
                EditorKind.Text
            ),
        };

        options.RegisterComponent<TestComponentDouble, PanelTestProps>(
            "widget",
            builder =>
            {
                builder.DisplayName = "Widget";
                builder.AccessibleName = "Widget";
                builder.DefaultProps = new PanelTestProps("", "", 0);
                builder.EditableProperties = overrideSchema;
            }
        );

        Assert.Same(overrideSchema, options.Registry.Resolve("widget").EditableProperties);
    }

    // The role flows from [PanelEditable] into the discovered EditableProperty schema exactly
    // like Kind/Options - it's what PropertyRoleValidator checks and what the panel merges on.
    [Fact]
    public void RegisterComponentDiscoversTheRoleFromPanelEditableAttribute()
    {
        var options = new D12CanvasOptions();

        options.RegisterComponent<TestComponentDouble, PanelTestProps>(
            "widget",
            builder =>
            {
                builder.DisplayName = "Widget";
                builder.AccessibleName = "Widget";
                builder.DefaultProps = new PanelTestProps("", "", 0);
            }
        );

        var tint = options
            .Registry.Resolve("widget")
            .EditableProperties!.Single(p => p.Property.Name == nameof(PanelTestProps.Tint));

        Assert.Equal(PropertyRole.Fill, tint.Role);
    }

    // Two properties on different types can declare the same role; each is checked against the
    // role's own declaration, never against the other type, so registering both must not throw.
    [Fact]
    public void RegisterComponentWithTheSameRoleOnADifferentTypeDoesNotThrow()
    {
        var options = new D12CanvasOptions();
        options.RegisterComponent<TestComponentDouble, PanelTestProps>(
            "widget-a",
            builder =>
            {
                builder.DisplayName = "Widget A";
                builder.AccessibleName = "Widget A";
                builder.DefaultProps = new PanelTestProps("", "", 0);
            }
        );

        var exception = Record.Exception(
            () =>
                options.RegisterComponent<TestComponentDouble, PanelTestPropsSecondary>(
                    "widget-b",
                    builder =>
                    {
                        builder.DisplayName = "Widget B";
                        builder.AccessibleName = "Widget B";
                        builder.DefaultProps = new PanelTestPropsSecondary();
                    }
                )
        );

        Assert.Null(exception);
        Assert.Contains(
            options.Registry.Resolve("widget-b").EditableProperties!,
            p =>
                p.Property.Name == nameof(PanelTestPropsSecondary.AccentColor)
                && p.Role == PropertyRole.Fill
        );
    }

    // "A mismatch is a registration-time error, not a silent merge" - a property whose EditorKind
    // disagrees with its role fails on its own registration, with no other type involved, naming
    // the type and property.
    [Fact]
    public void RegisterComponentWithARoleKindMismatchThrowsNamingTheProperty()
    {
        var options = new D12CanvasOptions();

        var exception = Assert.Throws<PropertyRoleMismatchException>(
            () =>
                options.RegisterComponent<TestComponentDouble, PropsWithRoleKindMismatch>(
                    "widget",
                    builder =>
                    {
                        builder.DisplayName = "Widget";
                        builder.AccessibleName = "Widget";
                        builder.DefaultProps = new PropsWithRoleKindMismatch();
                    }
                )
        );

        Assert.Equal(nameof(PropsWithRoleKindMismatch.Tint), exception.PropertyName);
        Assert.Throws<UnknownComponentKeyException>(() => options.Registry.Resolve("widget"));
    }

    // Same as above, but the mismatch is in CLR type (int vs string) rather than EditorKind. What
    // the exception carries is PropertyRolesTests' concern; here it is enough that registration
    // refuses the type.
    [Fact]
    public void RegisterComponentWithARoleClrTypeMismatchThrowsNamingTheProperty()
    {
        var options = new D12CanvasOptions();

        var exception = Assert.Throws<PropertyRoleMismatchException>(
            () =>
                options.RegisterComponent<TestComponentDouble, PropsWithRoleClrTypeMismatch>(
                    "widget",
                    builder =>
                    {
                        builder.DisplayName = "Widget";
                        builder.AccessibleName = "Widget";
                        builder.DefaultProps = new PropsWithRoleClrTypeMismatch();
                    }
                )
        );

        Assert.Equal(nameof(PropsWithRoleClrTypeMismatch.Tint), exception.PropertyName);
    }

    // A builder-supplied schema bypasses attribute discovery but not role validation.
    [Fact]
    public void RegisterComponentValidatesRolesOnABuilderSuppliedSchemaToo()
    {
        var options = new D12CanvasOptions();
        var mismatched = new EditableProperty(
            typeof(PanelTestProps).GetProperty(nameof(PanelTestProps.Label))!,
            EditorKind.Text,
            Role: PropertyRole.Fill
        );

        var exception = Assert.Throws<PropertyRoleMismatchException>(
            () =>
                options.RegisterComponent<TestComponentDouble, PanelTestProps>(
                    "widget",
                    builder =>
                    {
                        builder.DisplayName = "Widget";
                        builder.AccessibleName = "Widget";
                        builder.DefaultProps = new PanelTestProps("", "", 0);
                        builder.EditableProperties = [mismatched];
                    }
                )
        );

        Assert.Equal(nameof(PanelTestProps.Label), exception.PropertyName);
    }
}
