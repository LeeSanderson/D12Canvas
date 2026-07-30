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

    // ADR 0008: editable properties default to whatever a TProps record's own [PanelEditable]
    // attributes declare - PanelTestProps.Content carries none (it's this type's stand-in for a
    // Text-type *content* field, excluded the same way StickyNoteProps.Text is), so only its
    // five [PanelEditable]-carrying fields (one per EditorKind, ticket 56/57) should surface here.
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

    // Ticket 57: a Dropdown-kind property with no choices can't render a usable <select>, so this
    // is caught at registration time rather than surfacing as an empty control in the panel.
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

    // ADR 0008: "attributes set the default schema, the builder is the escape hatch" - setting
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
}
