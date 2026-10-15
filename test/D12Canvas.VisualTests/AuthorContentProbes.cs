using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace D12Canvas.VisualTests;

// A primary press on an author's own content inside an instance belongs to the browser: the
// listener takes no capture and tracks nothing, and C# hears only the press, which adds the
// instance to the selection without taking anything out. An open inline editor is such content.
// Content an author marked rather than a
// control cannot take focus, so the browser would hand focus to the instance's tab stop and select
// it outright; the listener prevents that press and focuses the canvas instead.
public sealed class AuthorContentProbes(PlaywrightFixture playwright, DemoAppFixture demoApp)
    : InteractionProbe(playwright, demoApp)
{
    private const string TargetId = "a0000000-0000-0000-0000-000000000002";
    private const string LooseEndId = "a0000000-0000-0000-0000-000000000003";
    private const string NoteId = "a0000000-0000-0000-0000-000000000004";

    [Fact]
    public async Task ADoubleClickOpensTheInlineEditorAndADragInsideItSelectsTextWithFocusKept()
    {
        await Page.Locator($".component-container[data-d12-entity='{NoteId}']").DblClickAsync();
        var editor = Page.Locator("textarea.d12-sticky-note-editor");
        await Expect(editor).ToBeFocusedAsync();
        await SettleAsync();
        await ClearCallsAsync();
        var box = await editor.BoundingBoxAsync();
        Assert.NotNull(box);

        await Page.Mouse.MoveAsync(box!.X + 4, box.Y + 8);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(box.X + box.Width - 4, box.Y + 8, new() { Steps = 6 });
        await Page.Mouse.UpAsync();
        await SettleAsync();

        await Expect(editor).ToBeFocusedAsync();
        Assert.True(
            await editor.EvaluateAsync<bool>(
                "element => element.selectionEnd > element.selectionStart"
            ),
            "The drag selected no text inside the editor."
        );
        var press = await SinglePressAsync();
        Assert.Equal("author-content", press.GetProperty("role").GetString());
        Assert.Empty(await CallsToAsync("OnPointerMoved"));
        Assert.Empty(await CallsToAsync("OnPointerReleased"));
    }

    [Fact]
    public async Task APressOnMarkedContent_AddsItsInstanceAndFocusesTheCanvasInsteadOfTheInstance()
    {
        await SelectLooseEndAsync();
        await InjectIntoTargetAsync(
            "<div class='probe-author-content' data-d12-author-content>marked</div>"
        );

        await Page.Locator(".probe-author-content").ClickAsync();
        await SettleAsync();

        Assert.True(
            await Page.EvaluateAsync<bool>(
                "() => document.activeElement === document.querySelector('.diagram-canvas')"
            ),
            "Focus did not land on the canvas."
        );
        await ExpectSelectedAsync(LooseEndId, TargetId);
    }

    [Fact]
    public async Task AnAltPressOnMarkedContentIsAPressOnItsInstance()
    {
        await InjectIntoTargetAsync(
            "<div class='probe-author-content' data-d12-author-content>marked</div>"
        );
        await SettleAsync();
        await ClearCallsAsync();

        await Page.Keyboard.DownAsync("Alt");
        await Page.Locator(".probe-author-content").ClickAsync();
        await Page.Keyboard.UpAsync("Alt");
        await SettleAsync();

        var press = await SinglePressAsync();
        Assert.Equal("instance", press.GetProperty("role").GetString());
        Assert.Equal(TargetId, press.GetProperty("entityId").GetString());
        Assert.Single(await CallsToAsync("OnPointerReleased"));
    }

    [Fact]
    public async Task AnAltPressInsideAnOpenEditorStaysWithTheEditor()
    {
        await Page.Locator($".component-container[data-d12-entity='{NoteId}']").DblClickAsync();
        var editor = Page.Locator("textarea.d12-sticky-note-editor");
        await Expect(editor).ToBeFocusedAsync();
        await SettleAsync();
        await ClearCallsAsync();

        await Page.Keyboard.DownAsync("Alt");
        await editor.ClickAsync(
            new()
            {
                Position = new() { X = 8, Y = 8 },
            }
        );
        await Page.Keyboard.UpAsync("Alt");
        await SettleAsync();

        Assert.Equal("author-content", (await SinglePressAsync()).GetProperty("role").GetString());
        Assert.Empty(await CallsToAsync("OnPointerReleased"));
        await Expect(editor).ToBeFocusedAsync();
    }

    private async Task SelectLooseEndAsync()
    {
        var looseEnd = Page.Locator($".component-container[data-d12-entity='{LooseEndId}']");
        await looseEnd.ClickAsync(
            new()
            {
                Position = new() { X = 20, Y = 20 },
            }
        );
        await Expect(looseEnd).ToHaveAttributeAsync("aria-selected", "true");
        await SettleAsync();
        await ClearCallsAsync();
    }

    private Task InjectIntoTargetAsync(string markup) =>
        Page.EvaluateAsync(
            """
            ([selector, markup]) => {
                const host = document.querySelector(selector);
                host.insertAdjacentHTML("beforeend", markup);
                host.lastElementChild.style.cssText =
                    "position: absolute; left: 10px; top: 10px; width: 120px; height: 40px; z-index: 1;";
            }
            """,
            new[] { $"[data-d12-entity='{TargetId}'] .container-content", markup }
        );

    private async Task ExpectSelectedAsync(params string[] entityIds)
    {
        foreach (var entityId in entityIds)
        {
            await Expect(Page.Locator($".component-container[data-d12-entity='{entityId}']"))
                .ToHaveAttributeAsync("aria-selected", "true");
        }
    }
}
