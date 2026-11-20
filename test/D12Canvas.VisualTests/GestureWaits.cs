using Microsoft.Playwright;

namespace D12Canvas.VisualTests;

internal static class GestureWaits
{
    public static async Task UntilBoxAsync(
        ILocator locator,
        Func<LocatorBoundingBoxResult, bool> reached,
        string description
    )
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            if (await locator.BoundingBoxAsync() is { } current && reached(current))
            {
                return;
            }

            await Task.Delay(100);
        }

        Assert.Fail($"The page never showed {description}.");
    }
}
