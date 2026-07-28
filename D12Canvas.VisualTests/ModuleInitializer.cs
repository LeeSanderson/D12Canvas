using System.Runtime.CompilerServices;
using Microsoft.Playwright;
using VerifyTests;

namespace D12Canvas.VisualTests;

public static class ModuleInitializer
{
    // Playwright's built-in default (5s) can be shorter than a cold Blazor WASM boot takes to
    // download/JIT and render its first frame, particularly under disk-I/O pressure (e.g. a
    // bind-mounted Docker volume) - a slow-but-successful boot shouldn't read as a test failure.
    private const float DefaultExpectTimeoutMilliseconds = 20_000;

    [ModuleInitializer]
    public static void Initialize()
    {
        VerifyPlaywright.Initialize(installPlaywright: true);
        Assertions.SetDefaultExpectTimeout(DefaultExpectTimeoutMilliseconds);
        FuzzyPngComparer.Register();
    }
}
