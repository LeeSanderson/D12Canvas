namespace D12Canvas;

// The text and button values the canvas chrome pins on itself, so a host stylesheet such as
// Bootstrap's Reboot, which restyles body text and buttons, leaves the chrome as the library drew it.
internal static class PinnedChrome
{
    public static string Text(string root, int fontSizePx) =>
        $$"""

                {{root}} {
                    font-family:
                        -apple-system,
                        BlinkMacSystemFont,
                        "Segoe UI",
                        sans-serif;
                    font-size: {{fontSizePx}}px;
                    font-weight: 400;
                    line-height: 1.5;
                }
            
            """;

    public static string Button(string selector) =>
        $$"""

                {{selector}} {
                    margin: 0;
                    font: inherit;
                    color: inherit;
                    text-transform: none;
                    appearance: button;
                }
            
            """;
}
