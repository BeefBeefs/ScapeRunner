namespace OSRSIdle;

/// <summary>
/// Collapses bursts of model events into one main-thread render pass.
/// </summary>
public sealed class UiUpdateCoalescer : IDisposable
{
    private readonly Action _update;
    private int _pending;
    private bool _disposed;

    public UiUpdateCoalescer(Action update)
    {
        _update = update;
    }

    public void Request()
    {
        if (_disposed || Interlocked.Exchange(ref _pending, 1) != 0)
            return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            Interlocked.Exchange(ref _pending, 0);
            if (!_disposed)
                _update();
        });
    }

    public void Dispose()
    {
        _disposed = true;
    }
}
