namespace OSRSIdle;

public static class DebugSettings
{
    public static bool IsInstakillEnabled { get; private set; }

    public static event EventHandler? Changed;

    public static void SetInstakillEnabled(bool enabled)
    {
        if (IsInstakillEnabled == enabled)
            return;

        IsInstakillEnabled = enabled;
        Changed?.Invoke(null, EventArgs.Empty);
    }

    public static int GetAutoFightRespawnTicks(int normalTicks)
    {
        return IsInstakillEnabled ? 1 : normalTicks;
    }
}
