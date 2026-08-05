using D12Canvas.App;
using D12Canvas.App.Storage;
using D12Canvas.Registration;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddD12Canvas(_ => { });
builder.Services.AddSingleton<IBoardStore, IndexedDbBoardStore>();

await builder.Build().RunAsync();
