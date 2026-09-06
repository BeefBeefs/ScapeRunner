namespace OSRSIdle;

/// <summary>
/// Controls the pace of live gameplay without changing wall-clock time.
/// Save and offline-progress timestamps must continue to use DateTime.UtcNow.
/// </summary>
public static class GameClock
{
    public const int StandardTickMilliseconds = 600;
    public const int DebugSpeedMultiplier = 20;

    private static bool _isDebugSpeedEnabled;

    public static bool IsDebugSpeedEnabled => _isDebugSpeedEnabled;

    public static int SpeedMultiplier =>
        _isDebugSpeedEnabled ? DebugSpeedMultiplier : 1;

    public static TimeSpan TickInterval =>
        TimeSpan.FromMilliseconds(
            StandardTickMilliseconds / (double)SpeedMultiplier);

    public static event EventHandler? SpeedChanged;

    public static void SetDebugSpeedEnabled(bool enabled)
    {
        if (_isDebugSpeedEnabled == enabled)
            return;

        _isDebugSpeedEnabled = enabled;
        SpeedChanged?.Invoke(null, EventArgs.Empty);
    }
}
