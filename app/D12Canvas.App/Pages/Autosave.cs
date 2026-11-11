namespace D12Canvas.App.Pages;

public sealed record SaveStatus(string? Icon, string Text);

// Delayed saves and the end of the saved notice come back through dispatch, the component's
// InvokeAsync, so they run on the renderer's thread.
public sealed class Autosave : IDisposable
{
    private static readonly TimeSpan Debounce = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan SavedNoticeDuration = TimeSpan.FromSeconds(2);

    private readonly Func<Task> _save;
    private readonly Func<Func<Task>, Task> _dispatch;
    private readonly Action _stateChanged;
    private readonly RestartableDelay _pendingSave;
    private readonly RestartableDelay _savedNotice;
    private int _changeCount;
    private bool _showSavedNotice;

    public Autosave(
        TimeProvider clock,
        Func<Task> save,
        Func<Func<Task>, Task> dispatch,
        Action stateChanged
    )
    {
        _save = save;
        _dispatch = dispatch;
        _stateChanged = stateChanged;
        _pendingSave = new RestartableDelay(clock, Debounce);
        _savedNotice = new RestartableDelay(clock, SavedNoticeDuration);
    }

    public bool Enabled { get; set; } = true;

    public bool IsDirty { get; private set; }

    public SaveStatus Status =>
        (Enabled, IsDirty) switch
        {
            (true, true) => new("cloud-arrow-up", "Saving…"),
            (true, false) when _showSavedNotice => new("cloud-check", "Saved"),
            (true, false) => new(null, ""),
            (false, true) => new("cloud", "Unsaved changes"),
            (false, false) => new("cloud-check", "Saved"),
        };

    // A burst of quick edits, several nudges say, ends in one save: each edit restarts the debounce.
    public void BoardChanged()
    {
        IsDirty = true;
        _changeCount++;

        if (Enabled)
        {
            HideSavedNotice();
            _pendingSave.Restart(() => _dispatch(SaveAsync));
        }
    }

    public async Task SetEnabledAsync(bool enabled)
    {
        Enabled = enabled;

        if (!enabled)
        {
            _pendingSave.Cancel();
            HideSavedNotice();
        }
        else if (IsDirty)
        {
            await SaveAsync();
        }
    }

    public async Task SaveAsync()
    {
        var changeCountAtSave = _changeCount;
        await _save();

        if (_changeCount != changeCountAtSave)
        {
            return;
        }

        IsDirty = false;
        if (Enabled)
        {
            ShowSavedNotice();
        }

        _stateChanged();
    }

    public void Dispose()
    {
        _pendingSave.Dispose();
        _savedNotice.Dispose();
    }

    private void ShowSavedNotice()
    {
        _showSavedNotice = true;
        _savedNotice.Restart(
            () =>
                _dispatch(() =>
                {
                    _showSavedNotice = false;
                    _stateChanged();
                    return Task.CompletedTask;
                })
        );
    }

    private void HideSavedNotice()
    {
        _savedNotice.Cancel();
        _showSavedNotice = false;
    }
}
