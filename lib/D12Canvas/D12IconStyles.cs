namespace D12Canvas;

// The rules every D12Icon needs. A library root that draws icons (the palette, the canvas)
// carries them once in its own style block and cascades InScope, so the icons inside it leave out
// their own copy; an icon with nothing in scope, such as one a host places on its own page, still
// brings the rules itself.
internal sealed class D12IconStyles
{
    public static readonly D12IconStyles InScope = new();

    private D12IconStyles() { }

    public const string Rules = """

            .d12-icon {
                width: var(--d12-icon-size, 16px);
                height: var(--d12-icon-size, 16px);
                flex: none;
                stroke-width: var(--d12-icon-stroke, 1.5);
            }

            .d12-icon .d12-icon-fill {
                fill: currentColor;
                stroke: none;
            }

            .d12-icon .d12-icon-accent {
                stroke: var(--d12-icon-accent, currentColor);
            }

            .d12-icon .d12-icon-accent.d12-icon-fill {
                fill: var(--d12-icon-accent, currentColor);
                stroke: none;
            }

        """;
}
