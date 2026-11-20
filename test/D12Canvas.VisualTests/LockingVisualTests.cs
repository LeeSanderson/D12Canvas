using Microsoft.Playwright;
using Xunit;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

public sealed class LockingVisualTests : IAsyncLifetime
{
    private const string FreeId = "10c00000-0000-0000-0000-000000000001";
    private const string LockedId = "10c00000-0000-0000-0000-000000000002";

    private readonly IBrowser _browser;
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    public LockingVisualTests(PlaywrightFixture playwright, DemoAppFixture demoApp)
    {
        _browser = playwright.Browser;
    }

    public ValueTask InitializeAsync() => new(OpenPageAsync(colorScheme: null));

    private async Task OpenPageAsync(ColorScheme? colorScheme)
    {
        _context = await _browser.NewContextAsync(
            new BrowserNewContextOptions
            {
                BaseURL = DemoAppFixture.BaseUrl,
                ReducedMotion = ReducedMotion.Reduce,
                ViewportSize = new ViewportSize { Width = 1000, Height = 700 },
                ColorScheme = colorScheme,
            }
        );
        _page = await _context.NewPageAsync();
        await _page.GotoAsync("/locking-demo");
        await Expect(_page.Locator(".component-container")).ToHaveCountAsync(2);
    }

    private async Task ReopenInTheDarkColorSchemeAsync()
    {
        await _context.DisposeAsync();
        await OpenPageAsync(ColorScheme.Dark);
    }

    public async ValueTask DisposeAsync() => await _context.DisposeAsync();

    private ILocator Shape(string id) =>
        _page.Locator($".component-container[data-d12-entity='{id}']");

    private ILocator Bar => _page.Locator(".d12-property-bar");

    [Fact]
    public async Task RightClickOnALockedShapeOffersUnlockAndThePanelDisablesItsFields_MatchesBaseline()
    {
        await Shape(LockedId).ClickAsync(new() { Button = MouseButton.Right });

        await Expect(_page.Locator(".d12-context-menu")).ToBeVisibleAsync();
        await Expect(_page.Locator(".d12-property-panel-unlock")).ToBeVisibleAsync();

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task AFullyLockedSelectionShowsABarOfOnlyUnlock_MatchesBaseline()
    {
        await SelectTheLockedShapeAsync();

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task AFullyLockedSelectionShowsABarOfOnlyUnlock_DarkColorScheme_MatchesBaseline()
    {
        await ReopenInTheDarkColorSchemeAsync();
        await SelectTheLockedShapeAsync();

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task APartlyLockedSelectionEndsTheBarWithItsLockCount_MatchesBaseline()
    {
        await SelectBothShapesAsync();

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task APartlyLockedSelectionEndsTheBarWithItsLockCount_DarkColorScheme_MatchesBaseline()
    {
        await ReopenInTheDarkColorSchemeAsync();
        await SelectBothShapesAsync();

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task APartlyLockedSelectionsPanelSaysItsEditSkipsTheLockedShape_MatchesBaseline()
    {
        await SelectBothShapesAndRecolourThemFromThePanelAsync();

        await ContentSnapshot.Verify(_page);
    }

    [Fact]
    public async Task APartlyLockedSelectionsPanelSaysItsEditSkipsTheLockedShape_DarkColorScheme_MatchesBaseline()
    {
        await ReopenInTheDarkColorSchemeAsync();
        await SelectBothShapesAndRecolourThemFromThePanelAsync();

        await ContentSnapshot.Verify(_page);
    }

    private async Task SelectBothShapesAndRecolourThemFromThePanelAsync()
    {
        await SelectBothShapesAsync();
        await Expect(_page.Locator(".d12-property-panel-locks"))
            .ToHaveTextAsync("1 of 2 locked; changes skip them");

        await _page.Locator("#d12-property-panel-field-FillColor").FillAsync("#b8e6c1");

        await Expect(Bar.Locator(".d12-property-bar-chip").First)
            .ToHaveAttributeAsync("aria-label", "Fill: Mixed");
    }

    private async Task SelectTheLockedShapeAsync()
    {
        await Shape(LockedId).ClickAsync(new() { Button = MouseButton.Right });
        await Expect(_page.Locator(".d12-context-menu")).ToBeVisibleAsync();
        await _page.Keyboard.PressAsync("Escape");
        await Expect(_page.Locator(".d12-context-menu")).ToHaveCountAsync(0);
        await Expect(_page.Locator(".component-container.selected")).ToHaveCountAsync(1);
        await Expect(Bar.Locator(".d12-property-bar-unlock")).ToBeVisibleAsync();
    }

    private async Task SelectBothShapesAsync()
    {
        await SelectTheLockedShapeAsync();
        await Shape(FreeId).ClickAsync(new() { Modifiers = [KeyboardModifier.Shift] });
        await Expect(_page.Locator(".component-container.selected")).ToHaveCountAsync(2);
        await Expect(Bar.Locator(".d12-property-bar-locks")).ToHaveTextAsync("1/2");
    }
}
