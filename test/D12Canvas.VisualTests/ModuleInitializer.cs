using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Playwright;
using VerifyTests;

namespace D12Canvas.VisualTests;

public static partial class ModuleInitializer
{
    private const float DefaultExpectTimeoutMilliseconds = 20_000;

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
