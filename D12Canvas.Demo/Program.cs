using D12Canvas.Demo;
using D12Canvas.Demo.Components;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
});

builder.Services.AddD12Canvas(options =>
{
    options.RegisterComponent<DemoNoteComponent, DemoNoteProps>(
        "demo-note",
        b =>
        {
            b.DisplayName = "Demo Note";
            b.AccessibleName = "Demo note";
            b.DefaultProps = new DemoNoteProps("New note", "#ffd966");
            b.DefaultSize = new ComponentSize(200, 150);
        }
    );
});

await builder.Build().RunAsync();
