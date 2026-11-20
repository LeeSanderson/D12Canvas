namespace D12Canvas;

internal static class RaisedSurface
{
    private const string Light = """
                --d12-surface: #fff;
                --d12-border: #ccc;
                --d12-accent: #2f80ed;
                --d12-muted-text: #666;
                --d12-text: #212529;
                --d12-shadow: rgba(0, 0, 0, 0.15);
                color-scheme: light;
        """;

    private const string Dark = """
                --d12-surface: #2a2a2a;
                --d12-border: rgba(255, 255, 255, 0.12);
                --d12-accent: #2f80ed;
                --d12-muted-text: #a0a0a0;
                --d12-text: #e8e8e8;
                --d12-shadow: rgba(0, 0, 0, 0.5);
                color-scheme: dark;
        """;

    public const string HoverBackground =
        "color-mix(in srgb, var(--d12-border) 35%, var(--d12-surface))";

    public static string TokenRules(string root) =>
        $$"""

                {{root}} {
                {{Light}}
                }

                @media (prefers-color-scheme: dark) {
                    {{root}} {
                {{Dark}}
                    }
                }

                {{root}}[data-d12-theme=light],
                [data-d12-theme=light] {{root}} {
                {{Light}}
                }

                {{root}}[data-d12-theme=dark],
                [data-d12-theme=dark] {{root}} {
                {{Dark}}
                }
            
            """;
}
