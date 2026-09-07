namespace OSRSIdle;

/// <summary>
/// Controls the pace of live gameplay without changing wall-clock time.
/// Save and offline-progress timestamps must continue to use DateTime.UtcNow.
/// </summary>
public static class GameClock
{
    public const int StandardTickMilliseconds = 600;

    // Accelerated play advances exactly one game tick per callback. Keeping
    // the callback at 60 ms avoids flooding Android's dispatcher and combat
    // animation queue as the former 20x and 100x modes could do.
    public const int SpeedUpTickMilliseconds = 60;
    public const int SpeedUpMultiplier =
        StandardTickMilliseconds / SpeedUpTickMilliseconds;

    private static bool _isSpeedUpEnabled;

    public static bool IsSpeedUpEnabled => _isSpeedUpEnabled;

    public static int SpeedMultiplier =>
        _isSpeedUpEnabled ? SpeedUpMultiplier : 1;

    public static TimeSpan TickInterval =>
        TimeSpan.FromMilliseconds(
            _isSpeedUpEnabled
                ? SpeedUpTickMilliseconds
                : StandardTickMilliseconds);

    public static event EventHandler? SpeedChanged;

    public static void SetSpeedUpEnabled(bool enabled)
    {
        if (_isSpeedUpEnabled == enabled)
            return;

        _isSpeedUpEnabled = enabled;
        SpeedChanged?.Invoke(null, EventArgs.Empty);
    }
}
