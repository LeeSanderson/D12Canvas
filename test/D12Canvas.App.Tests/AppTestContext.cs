using Bunit;
using Bunit.TestDoubles;
using D12Canvas.App.Layout;
using D12Canvas.App.Pages;
using D12Canvas.App.Preferences;
using D12Canvas.App.Storage;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace D12Canvas.App.Tests;

public abstract class AppTestContext : BunitContext
{
    protected AppTestContext()
    {
        JSInterop.Mode = JSRuntimeMode.Strict;
        Services.AddD12Canvas(_ => { });
        Services.AddSingleton<IBoardStore>(BoardStore);
        Services.AddSingleton<IPreferences>(Preferences);
        Services.AddSingleton<TimeProvider>(Clock);
        Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri("http://localhost/") });
        CanvasJs = new CanvasJsInterop(JSInterop);
    }

    protected InMemoryBoardStore BoardStore { get; } = new();

    protected InMemoryPreferences Preferences { get; } = new();

    protected FakeTimeProvider Clock { get; } = new();

    protected BunitNavigationManager Navigation =>
        Services.GetRequiredService<BunitNavigationManager>();

    protected CanvasJsInterop CanvasJs { get; }

    protected IRenderedComponent<MainLayout> RenderBoardList() => RenderPage<BoardList>("/");

    protected IRenderedComponent<MainLayout> RenderBoardEditor(Guid boardId) =>
        RenderPage<BoardEditor>(
            $"/board/{boardId}",
            parameters => parameters.Add(page => page.Id, boardId)
        );

    protected IRenderedComponent<MainLayout> RenderPage<TPage>(
        string uri,
        Action<ComponentParameterCollectionBuilder<TPage>>? parameters = null
    )
        where TPage : IComponent
    {
        Navigation.NavigateTo(uri);
        return Render<MainLayout>(layout =>
            layout.Add<TPage>(layoutParameters => layoutParameters.Body, parameters ?? (_ => { }))
        );
    }
}
