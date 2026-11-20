using Microsoft.AspNetCore.Components;

namespace D12Canvas;

internal enum TooltipSide
{
    Right,
    Above,
}

internal static class Tooltip
{
    public const string Rules = """

            .d12-tooltip-anchor {
                position: relative;
                display: inline-flex;
            }

            .d12-tooltip {
                display: none;
                position: absolute;
                z-index: 20;
                padding: 4px 8px;
                border-radius: 4px;
                background: var(--d12-text);
                color: var(--d12-surface);
                font-size: 12px;
                line-height: 1.4;
                white-space: nowrap;
                pointer-events: none;
            }

            .d12-tooltip-anchor:hover > .d12-tooltip,
            .d12-tooltip-anchor:has(:focus-visible) > .d12-tooltip {
                display: block;
            }

            .d12-tooltip-right {
                left: calc(100% + var(--d12-tooltip-offset, 8px));
                top: 50%;
                transform: translateY(-50%);
            }

            .d12-tooltip-above {
                bottom: calc(100% + var(--d12-tooltip-offset, 8px));
                left: 50%;
                transform: translateX(-50%);
            }

        """;

    public static RenderFragment Around(string text, TooltipSide side, RenderFragment control) =>
        builder =>
        {
            builder.OpenElement(0, "span");
            builder.AddAttribute(1, "class", "d12-tooltip-anchor");
            builder.AddContent(2, control);
            builder.OpenElement(3, "span");
            builder.AddAttribute(4, "class", TooltipClass(side));
            builder.AddAttribute(5, "aria-hidden", "true");
            builder.AddContent(6, text);
            builder.CloseElement();
            builder.CloseElement();
        };

    private static string TooltipClass(TooltipSide side) =>
        side switch
        {
            TooltipSide.Above => "d12-tooltip d12-tooltip-above",
            _ => "d12-tooltip d12-tooltip-right",
        };
}
