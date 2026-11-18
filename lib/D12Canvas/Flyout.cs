using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace D12Canvas;

// The library's flyout: a menu that opens beside the button that opened it. Opening moves focus
// to its first item, and the arrow keys, Home and End move between items. Escape, Tab or a press
// outside it and its opener asks the owner to close it, and on closing it hands focus back to the
// opener when focus was inside it. The owner keeps whether it is open, renders it through Around,
// places it with its own class and carries Rules once in its style block. Rows are buttons with
// the menuitem role and the d12-flyout-item class; a column of rows is a group.
internal sealed class Flyout : ComponentBase, IAsyncDisposable
{
    internal const string ModulePath = "./_content/D12Canvas/flyout.js";

    public const string Rules = """

            .d12-flyout {
                position: absolute;
                z-index: 30;
                box-sizing: border-box;
                padding: 8px;
                background: var(--d12-surface);
                color: var(--d12-text);
                border: 1px solid var(--d12-border);
                border-radius: 8px;
                box-shadow: 0 4px 16px var(--d12-shadow);
                font-size: 13px;
                --d12-icon-size: 16px;
            }

            .d12-flyout:focus {
                outline: none;
            }

            .d12-flyout-heading {
                margin: 0 0 4px 8px;
                font-size: 11px;
                font-weight: 600;
                letter-spacing: 0.03em;
                text-transform: uppercase;
                color: var(--d12-muted-text);
                white-space: nowrap;
            }

            .d12-flyout-item {
                display: flex;
                align-items: center;
                gap: 8px;
                margin: 0;
                padding: 6px 8px;
                border: 0;
                border-radius: 6px;
                background: transparent;
                color: inherit;
                font: inherit;
                text-align: left;
                white-space: nowrap;
                cursor: pointer;
            }

            .d12-flyout-item:hover {
                background: color-mix(in srgb, var(--d12-border) 35%, var(--d12-surface));
            }

            .d12-flyout-item:focus-visible {
                outline: 2px solid var(--d12-accent);
                outline-offset: -2px;
            }

        """;

    private ElementReference _element;
    private DotNetObjectReference<Flyout>? _dotNetRef;
    private IJSObjectReference? _module;
    private IJSObjectReference? _registration;

    [Inject]
    private IJSRuntime JS { get; set; } = null!;

    [Parameter, EditorRequired]
    public string Label { get; set; } = "";

    [Parameter]
    public string? Class { get; set; }

    [Parameter]
    public ElementReference Opener { get; set; }

    [Parameter]
    public EventCallback OnRequestClose { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    public static RenderFragment Around(
        string label,
        string placementClass,
        ElementReference opener,
        EventCallback onRequestClose,
        RenderFragment content
    ) =>
        builder =>
        {
            builder.OpenComponent<Flyout>(0);
            builder.AddComponentParameter(1, nameof(Label), label);
            builder.AddComponentParameter(2, nameof(Class), placementClass);
            builder.AddComponentParameter(3, nameof(Opener), opener);
            builder.AddComponentParameter(4, nameof(OnRequestClose), onRequestClose);
            builder.AddComponentParameter(5, nameof(ChildContent), content);
            builder.CloseComponent();
        };

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", Class is null ? "d12-flyout" : $"d12-flyout {Class}");
        builder.AddAttribute(2, "role", "menu");
        builder.AddAttribute(3, "aria-label", Label);
        builder.AddAttribute(4, "tabindex", "-1");
        builder.AddElementReferenceCapture(5, element => _element = element);
        builder.AddContent(6, ChildContent);
        builder.CloseElement();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        _module = await JS.InvokeAsync<IJSObjectReference>("import", ModulePath);
        _dotNetRef = DotNetObjectReference.Create(this);
        _registration = await _module.InvokeAsync<IJSObjectReference>(
            "registerFlyout",
            _element,
            Opener,
            _dotNetRef
        );
    }

    [JSInvokable]
    public Task RequestClose() => OnRequestClose.InvokeAsync();

    public async ValueTask DisposeAsync()
    {
        if (_registration is not null)
        {
            await _registration.InvokeVoidAsync("dispose");
            await _registration.DisposeAsync();
        }

        if (_module is not null)
        {
            await _module.DisposeAsync();
        }

        _dotNetRef?.Dispose();
    }
}
