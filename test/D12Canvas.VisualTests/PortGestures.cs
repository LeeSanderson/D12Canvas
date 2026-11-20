using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

internal static class PortGestures
{
    public static ILocator PortSpan(ILocator container, string part) =>
        container.Locator($".port-span[data-d12-part='{part}']");

    public static ILocator Dot(ILocator container, string side) =>
        container.Locator($".port-{side.ToLowerInvariant()}");

    public static async Task<(float X, float Y)> CentreOfAsync(ILocator locator)
    {
        var box = await locator.BoundingBoxAsync();
        Assert.NotNull(box);
        return ((float)(box!.X + box.Width / 2), (float)(box.Y + box.Height / 2));
    }

    public static async Task<(float X, float Y)> SelectAndAimAtPortAsync(
        ILocator container,
        string side
    )
    {
        await container.ClickAsync();
        await Expect(PortSpan(container, side)).ToHaveCountAsync(1);
        return await CentreOfAsync(Dot(container, side));
    }

    public static async Task<(float X, float Y)> ArriveAtPortAsync(
        IPage page,
        ILocator target,
        string side
    )
    {
        var body = await CentreOfAsync(target);
        await page.Mouse.MoveAsync(body.X, body.Y, new() { Steps = 4 });
        await Expect(PortSpan(target, side)).ToHaveCountAsync(1);
        var port = await CentreOfAsync(Dot(target, side));
        await page.Mouse.MoveAsync(port.X, port.Y, new() { Steps = 2 });
        return port;
    }

    public static async Task ConnectAsync(
        IPage page,
        ILocator source,
        string sourceSide,
        ILocator target,
        string targetSide
    )
    {
        var from = await SelectAndAimAtPortAsync(source, sourceSide);
        await page.Mouse.MoveAsync(from.X, from.Y);
        await page.Mouse.DownAsync();
        await ArriveAtPortAsync(page, target, targetSide);
        await page.Mouse.UpAsync();
        await Expect(page.Locator(".connector-drag-preview")).ToHaveCountAsync(0);
        await page.Keyboard.PressAsync("Escape");
        await Expect(source).Not.ToHaveAttributeAsync("aria-selected", "true");
    }
}
