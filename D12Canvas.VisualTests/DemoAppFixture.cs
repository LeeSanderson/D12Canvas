using System.Diagnostics;
using Xunit;

[assembly: AssemblyFixture(typeof(D12Canvas.VisualTests.DemoAppFixture))]

namespace D12Canvas.VisualTests;

// Boots the real D12Canvas.Demo WASM app once for the whole assembly so Playwright can drive it,
// per the layered-testing-strategy decision (.scratch/d12canvas-next/issues/04-layered-testing-strategy.md).
public sealed class DemoAppFixture : IAsyncLifetime
{
    public const string BaseUrl = "http://127.0.0.1:5299";

    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);

    private Process? _demoProcess;

    public async ValueTask InitializeAsync()
    {
        var demoProjectPath = FindDemoProjectPath();

        // Streams are left inherited (not redirected) rather than captured: nothing here reads them,
        // and Blazor's dev-server build output is enough to fill the redirect buffer and deadlock the process.
        _demoProcess = Process.Start(
            new ProcessStartInfo("dotnet", $"run --no-launch-profile --urls {BaseUrl}")
            {
                WorkingDirectory = Path.GetDirectoryName(demoProjectPath),
                UseShellExecute = false,
            }
        );

        if (_demoProcess is null)
        {
            throw new InvalidOperationException("Failed to start the D12Canvas.Demo process.");
        }

        await WaitUntilRespondingAsync(BaseUrl, StartupTimeout);
    }

    public ValueTask DisposeAsync()
    {
        if (_demoProcess is { HasExited: false })
        {
            _demoProcess.Kill(entireProcessTree: true);
            _demoProcess.WaitForExit((int)TimeSpan.FromSeconds(10).TotalMilliseconds);
        }

        _demoProcess?.Dispose();
        return ValueTask.CompletedTask;
    }

    private static string FindDemoProjectPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (
            directory is not null && !File.Exists(Path.Combine(directory.FullName, "D12Canvas.sln"))
        )
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            throw new InvalidOperationException(
                "Could not locate the repository root (D12Canvas.sln) above "
                    + AppContext.BaseDirectory
            );
        }

        return Path.Combine(directory.FullName, "D12Canvas.Demo", "D12Canvas.Demo.csproj");
    }

    private static async Task WaitUntilRespondingAsync(string baseUrl, TimeSpan timeout)
    {
        using var httpClient = new HttpClient();
        var deadline = DateTime.UtcNow + timeout;
        Exception? lastFailure = null;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                using var response = await httpClient.GetAsync(baseUrl);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                lastFailure = ex;
            }

            await Task.Delay(PollInterval);
        }

        throw new TimeoutException(
            $"D12Canvas.Demo did not respond at {baseUrl} within {timeout}.",
            lastFailure
        );
    }
}
