using System.Text.Json;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// The base for tests that drive a real browser and assert DOM or interop state rather than
// pixels. Every call the page's JavaScript makes into a .NET object is recorded before it is
// dispatched, so a probe reads what C# was handed. A console error logged at any point fails the
// test when it is disposed.
public abstract class InteractionProbe : IAsyncLifetime
{
    private static readonly string[] PointerEntryPoints =
    [
        "OnPointerPressed",
        "OnPointerMoved",
        "OnPointerReleased",
        "OnPointerCancelled",
    ];

    private const string InteropRecorderScript = """
        (() => {
            const calls = [];
            const pending = [];
            const wrap = (dotNet) => {
                const prototype = dotNet?.DotNetObject?.prototype;
                if (!prototype || prototype.__d12ProbeRecorded) {
                    return;
                }
                const invoke = prototype.invokeMethodAsync;
                prototype.invokeMethodAsync = function (method, ...args) {
                    calls.push({ method, args: JSON.parse(JSON.stringify(args)) });
                    const result = invoke.call(this, method, ...args);
                    pending.push(Promise.resolve(result).catch(() => {}));
                    return result;
                };
                prototype.__d12ProbeRecorded = true;
            };
            let dotNet;
            Object.defineProperty(window, "DotNet", {
                configurable: true,
                get() {
                    wrap(dotNet);
                    return dotNet;
                },
                set(value) {
                    dotNet = value;
                    wrap(dotNet);
                }
            });
            const nextFrame = () => new Promise((resolve) => requestAnimationFrame(resolve));
            window.__d12Probe = {
                calls,
                clear: () => {
                    calls.length = 0;
                },
                settle: async () => {
                    await Promise.allSettled(pending);
                    await nextFrame();
                    await nextFrame();
                    await Promise.allSettled(pending);
                }
            };
        })();
        """;

    protected static readonly MouseDownOptions MiddleDown = new() { Button = MouseButton.Middle };
    protected static readonly MouseUpOptions MiddleUp = new() { Button = MouseButton.Middle };

    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;

    // demoApp is otherwise unused: taking it documents that every probe depends on the Demo app
    // assembly fixture having finished starting up.
    protected InteractionProbe(PlaywrightFixture playwright, DemoAppFixture demoApp)
    {
        _browser = playwright.Browser;
    }

    protected IPage Page { get; private set; } = null!;

    // The page a probe drives, and how many instances it seeds, which the probe waits for.
    protected virtual string ProbePagePath => "/interaction-probe-demo";

    protected virtual int ProbePageInstanceCount => 4;

    // Appended to the probe page's path, for a probe that needs the canvas configured otherwise.
    protected virtual string ProbePageQuery => "";

    // Run before any page script, for a probe that needs the browser to report itself otherwise.
    protected virtual string? BrowserInitScript => null;

    protected ConsoleErrorTrap ConsoleErrors { get; } = new();

    public async ValueTask InitializeAsync()
    {
        _context = await _browser.NewContextAsync(
            new BrowserNewContextOptions
            {
                BaseURL = DemoAppFixture.BaseUrl,
                ViewportSize = new ViewportSize { Width = 1400, Height = 900 },
            }
        );
        await _context.AddInitScriptAsync(InteropRecorderScript);
        if (BrowserInitScript is { } browserInitScript)
        {
            await _context.AddInitScriptAsync(browserInitScript);
        }

        Page = await _context.NewPageAsync();
        ConsoleErrors.Attach(Page);
        await Page.GotoAsync(ProbePagePath + ProbePageQuery);
        await Expect(Page.Locator(".component-container")).ToHaveCountAsync(ProbePageInstanceCount);
        await SettleAsync();
        await ClearCallsAsync();
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await Page.EvaluateAsync("() => 0");
            ConsoleErrors.AssertNone();
        }
        finally
        {
            await _context.DisposeAsync();
        }
    }

    protected Task SettleAsync() => Page.EvaluateAsync("() => window.__d12Probe.settle()");

    protected Task ClearCallsAsync() => Page.EvaluateAsync("() => window.__d12Probe.clear()");

    protected async Task<IReadOnlyList<JsonElement>> CallsToAsync(string method)
    {
        var calls = await Page.EvaluateAsync<JsonElement>(
            "(method) => window.__d12Probe.calls.filter(call => call.method === method)",
            method
        );
        return [.. calls.EnumerateArray().Select(call => call.GetProperty("args"))];
    }

    protected async Task<int> PressedPointerIdAsync() =>
        (await SinglePressAsync()).GetProperty("pointerId").GetInt32();

    protected async Task<JsonElement> SinglePressAsync()
    {
        var presses = await CallsToAsync("OnPointerPressed");
        return Assert.Single(presses)[0];
    }

    protected async Task<int> PointerEntryPointCallCountAsync()
    {
        var count = 0;
        foreach (var method in PointerEntryPoints)
        {
            count += (await CallsToAsync(method)).Count;
        }

        return count;
    }

    protected async Task<(float X, float Y)> PagePointOnCanvasAsync(double x, double y)
    {
        var box = await Page.Locator(".diagram-container").BoundingBoxAsync();
        Assert.NotNull(box);
        return ((float)(box!.X + x), (float)(box.Y + y));
    }

    protected Task<(float X, float Y)> EmptyCanvasPointAsync() => PagePointOnCanvasAsync(560, 420);

    // Holds the middle button on empty canvas and drags far enough to pan, so a gesture is live.
    protected async Task<int> StartMiddlePanAsync()
    {
        var start = await EmptyCanvasPointAsync();
        await Page.Mouse.MoveAsync(start.X, start.Y);
        await Page.Mouse.DownAsync(MiddleDown);
        await Page.Mouse.MoveAsync(start.X - 40, start.Y - 30, new() { Steps = 4 });
        await SettleAsync();
        return await PressedPointerIdAsync();
    }

    protected async Task<(float X, float Y)> CentreOfAsync(ILocator locator)
    {
        var box = await locator.BoundingBoxAsync();
        Assert.NotNull(box);
        return ((float)(box!.X + box.Width / 2), (float)(box.Y + box.Height / 2));
    }

    // A response to a pointer with no button held is a leaked gesture. The pointer is swept over
    // empty canvas and across an instance, and neither the interop layer nor the viewport, the
    // selection or the marquee may react.
    protected async Task ExpectNothingRespondsToAButtonlessMoveAsync()
    {
        await SettleAsync();
        var before = await CanvasResponseStateAsync();
        await ClearCallsAsync();

        var emptyCanvas = await EmptyCanvasPointAsync();
        var instance = await CentreOfAsync(
            Page.Locator(".component-container[aria-label='Rectangle']").First
        );
        await Page.Mouse.MoveAsync(emptyCanvas.X, emptyCanvas.Y, new() { Steps = 4 });
        await Page.Mouse.MoveAsync(instance.X, instance.Y, new() { Steps = 8 });
        await Page.Mouse.MoveAsync(emptyCanvas.X, emptyCanvas.Y, new() { Steps = 8 });
        await SettleAsync();

        Assert.Equal(0, await PointerEntryPointCallCountAsync());
        Assert.Equal(before, await CanvasResponseStateAsync());
    }

    private Task<string> CanvasResponseStateAsync() =>
        Page.EvaluateAsync<string>(
            """
            () => JSON.stringify({
                viewport: document.querySelector(".canvas-content").getAttribute("style"),
                marquee: document.querySelector(".marquee-select") !== null,
                selected: [...document.querySelectorAll("[aria-selected='true']")]
                    .map(element => element.getAttribute("data-d12-entity"))
            })
            """
        );
}
