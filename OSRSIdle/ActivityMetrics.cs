namespace OSRSIdle;

public static class ActivityMetrics
{
    public const double SecondsPerTick = 0.6d;
    // Activities are intentionally paced at about 300 starter actions/hour.
    // Existing data was authored before the slower idle cadence was adopted.
    public const double ActivitySpeedMultiplier = 10d / 3d;

    public static int EffectiveActionTicks(SkillActivity activity) =>
        Math.Max(1, (int)Math.Ceiling(activity.ActionTicks * ActivitySpeedMultiplier));

    public static double ActionsPerHour(SkillActivity activity)
    {
        double seconds = EffectiveActionTicks(activity) * SecondsPerTick;
        return 3600d / seconds;
    }

    public static double XpPerHour(SkillActivity activity) =>
        activity.XP * ActionsPerHour(activity);

    public static double ItemsPerHour(SkillActivity activity) =>
        activity.ItemReward == null ? 0 : ActionsPerHour(activity);

    public static string FormatRate(double value)
    {
        if (value >= 1_000_000)
            return $"{value / 1_000_000:0.0}m";

        if (value >= 1_000)
            return $"{value / 1_000:0.0}k";

        return $"{value:0}";
    }

    public static string FormatDuration(double seconds)
    {
        if (seconds <= 0 || double.IsInfinity(seconds))
            return "—";

        TimeSpan duration = TimeSpan.FromSeconds(seconds);
        if (duration.TotalHours >= 1)
            return $"{(int)duration.TotalHours}h {duration.Minutes}m";

        if (duration.TotalMinutes >= 1)
            return $"{duration.Minutes}m {duration.Seconds}s";

        return $"{Math.Max(1, duration.Seconds)}s";
    }
}
