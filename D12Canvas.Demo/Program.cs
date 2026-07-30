using D12Canvas.Demo;
using D12Canvas.Demo.Components;
using D12Canvas.Panel;
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
            b.Icon = "📝";
            b.Category = "Notes";
            // Ticket 58: Color demonstrates the EditorKind.Custom escape hatch (a curated swatch
            // picker, not expressible by the built-in Color/Dropdown kinds) - it can only be
            // declared here, via the builder override, since [PanelEditable] attributes can't
            // carry a RenderFragment.
            b.EditableProperties = new[]
            {
                new EditableProperty(
                    typeof(DemoNoteProps).GetProperty(nameof(DemoNoteProps.Text))!,
                    EditorKind.Text
                ),
                new EditableProperty(
                    typeof(DemoNoteProps).GetProperty(nameof(DemoNoteProps.Color))!,
                    EditorKind.Custom,
                    CustomEditor: DemoNoteColorEditor.Editor
                ),
            };
        }
    );
    options.RegisterComponent<StressItemComponent, StressItemProps>(
        "stress-item",
        b =>
        {
            b.DisplayName = "Stress Item";
            b.AccessibleName = "Stress item";
            b.DefaultProps = new StressItemProps(0, "#e74c3c");
            b.Icon = "▦";
            b.Category = "Stress Test";
        }
    );
});

await builder.Build().RunAsync();
