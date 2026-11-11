namespace D12Canvas.App.Pages;

public sealed class RestartableDelay(TimeProvider clock, TimeSpan delay) : IDisposable
{
    private CancellationTokenSource? _pending;

    public void Restart(Func<Task> then)
    {
        Cancel();
        var pending = new CancellationTokenSource();
        _pending = pending;
        _ = RunAsync(then, pending.Token);
    }

    public void Cancel() => _pending?.Cancel();

    public void Dispose() => Cancel();

    private async Task RunAsync(Func<Task> then, CancellationToken token)
    {
        try
        {
            await Task.Delay(delay, clock, token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        await then();
    }
}
