namespace OSRSIdle;

/// <summary>
/// Serializes transient notifications so level-ups, rare drops, and other
/// celebratory messages are shown in arrival order instead of competing for
/// the same overlay slot.
/// </summary>
public sealed class NotificationQueue : IDisposable
{
    private readonly object _sync = new();
    private readonly Queue<Func<CancellationToken, Task>> _pending = new();
    private CancellationTokenSource _lifetime = new();
    private bool _processing;
    private bool _disposed;

    public void Enqueue(Func<CancellationToken, Task> notification)
    {
        ArgumentNullException.ThrowIfNull(notification);

        lock (_sync)
        {
            if (_disposed)
                return;

            _pending.Enqueue(notification);
            if (_processing)
                return;

            _processing = true;
            _ = ProcessAsync(_lifetime.Token);
        }
    }

    private async Task ProcessAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            Func<CancellationToken, Task>? notification;
            lock (_sync)
            {
                if (_pending.Count == 0 || _disposed)
                {
                    _processing = false;
                    return;
                }

                notification = _pending.Dequeue();
            }

            try
            {
                await notification(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Disposal intentionally cancels the active notification.
                return;
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Notification failed: {exception}");
            }
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
                return;

            _disposed = true;
            _pending.Clear();
            _lifetime.Cancel();
            _lifetime.Dispose();
        }
    }
}
