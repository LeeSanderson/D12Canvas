using Microsoft.Playwright;
using VerifyTests;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

public sealed class CustomPortVisualTests : IAsyncLifetime
{
    private const string CrowdedId = "b0000000-0000-0000-0000-000000000002";
    private const string LargeId = "b0000000-0000-0000-0000-000000000003";
    private const string CustomPortAtFourFifths = "c0000000-0000-0000-0000-000000000006";

    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    // demoApp is otherwise unused: taking it as a constructor parameter documents that this test
    // class depends on the Demo app assembly fixture having finished starting up.
    public CustomPortVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
    {
        _browser = playwright.Browser;
    }

    public async ValueTask InitializeAsync()
    {
        _context = await _browser.NewContextAsync(
            new BrowserNewContextOptions
            {
                BaseURL = DemoAppFixture.BaseUrl,
                ViewportSize = new ViewportSize { Width = 1280, Height = 800 },
            }
        );
        _page = await _context.NewPageAsync();
        await _page.GotoAsync("/border-partition-demo");
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(3);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    private ILocator Instance(string id) =>
        _page.Locator($".component-container[data-d12-entity='{id}']");

    // The crowded rectangle's custom port at 0.8 along its top side, dragged to the large
    // rectangle's left port: pulled and pinned exactly as a standard port is.
    [Fact]
    public async Task InstanceWithACustomPortAndAttachedEdge_MatchesBaseline()
    {
        var crowded = Instance(CrowdedId);
        await crowded.ClickAsync();
        var from = await PortGestures.CentreOfAsync(
            PortGestures.PortSpan(crowded, CustomPortAtFourFifths)
        );

        await _page.Mouse.MoveAsync(from.X, from.Y);
        await _page.Mouse.DownAsync();
        await PortGestures.ArriveAtPortAsync(_page, Instance(LargeId), "Left");
        await _page.Mouse.UpAsync();
        await _page.Keyboard.PressAsync("Escape");

        var line = _page.Locator(".edge-line");
        await Expect(line).ToHaveAttributeAsync("x1", "400");
        await Expect(line).ToHaveAttributeAsync("y1", "40");
        await Expect(line).ToHaveAttributeAsync("x2", "520");
        await Expect(line).ToHaveAttributeAsync("y2", "190");
        await Expect(crowded).Not.ToHaveAttributeAsync("aria-selected", "true");

        await ContentSnapshot.Verify(_page);
    }
}
