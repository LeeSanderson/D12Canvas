using D12Canvas;
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
            b.Icon = Glyphs.StickyNote;
            b.Category = "Notes";
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
            b.Icon = new Glyph(
                new GlyphPart("M2.75 2.75h10.5v10.5h-10.5z M7.75 2.75v10.5 M2.75 7.75h10.5")
            );
            b.Category = "Stress Test";
        }
    );
    DemoShapes.RegisterAll(options);
});

await builder.Build().RunAsync();
