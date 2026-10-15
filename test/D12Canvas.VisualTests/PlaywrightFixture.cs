using Microsoft.Playwright;
using Xunit;

[assembly: AssemblyFixture(typeof(D12Canvas.VisualTests.PlaywrightFixture))]

namespace D12Canvas.VisualTests;

// Launches one headless Chromium instance for the whole assembly; individual tests get their own
// IBrowserContext/IPage so they stay isolated from each other.
public sealed class PlaywrightFixture : IAsyncLifetime
{
    private IPlaywright _playwright = null!;

    public IBrowser Browser { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await Browser.DisposeAsync();
        _playwright.Dispose();
    }
}
