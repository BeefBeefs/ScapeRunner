namespace OSRSIdle;

/// <summary>
/// Controls the pace of live gameplay without changing wall-clock time.
/// Save and offline-progress timestamps must continue to use DateTime.UtcNow.
/// </summary>
public static class GameClock
{
    public const int StandardTickMilliseconds = 600;
    public const int DebugSpeedMultiplier = 20;
    public const int ExtremeDebugSpeedMultiplier = 100;

    private static bool _isDebugSpeedEnabled;
    private static int _debugSpeedMultiplier = DebugSpeedMultiplier;

    public static bool IsDebugSpeedEnabled => _isDebugSpeedEnabled;

    public static int SpeedMultiplier =>
        _isDebugSpeedEnabled ? _debugSpeedMultiplier : 1;

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

    public static void SetDebugSpeedMultiplier(int multiplier)
    {
        if (multiplier != DebugSpeedMultiplier &&
            multiplier != ExtremeDebugSpeedMultiplier)
        {
            throw new ArgumentOutOfRangeException(nameof(multiplier));
        }

        bool changed = _debugSpeedMultiplier != multiplier ||
            !_isDebugSpeedEnabled;

        _debugSpeedMultiplier = multiplier;
        _isDebugSpeedEnabled = true;

        if (changed)
            SpeedChanged?.Invoke(null, EventArgs.Empty);
    }
}
