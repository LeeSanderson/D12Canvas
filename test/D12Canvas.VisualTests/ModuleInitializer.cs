using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using VerifyTests;

namespace D12Canvas.VisualTests;

public static partial class ModuleInitializer
{
    // Playwright's built-in default (5s) can be shorter than a cold Blazor WASM boot takes to
    // download/JIT and render its first frame, particularly under disk-I/O pressure (e.g. a
    // bind-mounted Docker volume) - a slow-but-successful boot shouldn't read as a test failure.
    private const float DefaultExpectTimeoutMilliseconds = 20_000;

    // Two things in the rendered HTML carry no meaning for a snapshot and do not repeat between
    // runs. Every board entity renders its id as a hit-target marker, and a demo page seeds fresh
    // ids on each load, so the snapshot holds them as numbered placeholders in order of first
    // appearance. Blazor stamps each captured element reference with an `_bl_N` attribute whose
    // number follows the order the references were captured in, which two canvases on one page
    // settle differently from run to run, so those attributes are dropped.
    [ModuleInitializer]
    public static void Initialize()
    {
        VerifyPlaywright.Initialize(installPlaywright: true);
        VerifierSettings.ScrubInlineGuids();
        VerifierSettings.ScrubLinesWithReplace(line =>
            BlazorElementReferenceMarker().Replace(line, "")
        );
        VerifierSettings.AddScrubber("html", RemoveStyleElements);
        Assertions.SetDefaultExpectTimeout(DefaultExpectTimeoutMilliseconds);
        FuzzyPngComparer.Register();
    }

    // Components render their CSS as inline style elements, so every snapshot would otherwise
    // carry a copy of every rule and a one-line CSS edit would move every HTML baseline. The
    // screenshot already shows what the CSS does, so the HTML snapshot keeps markup only.
    private static void RemoveStyleElements(StringBuilder html)
    {
        var scrubbed = StyleElement().Replace(html.ToString(), "");
        html.Clear().Append(scrubbed);
    }

    [GeneratedRegex(@" _bl_\d+=""""")]
    private static partial Regex BlazorElementReferenceMarker();

    [GeneratedRegex(@"<style\b[^>]*>.*?</style>", RegexOptions.Singleline)]
    private static partial Regex StyleElement();
}
