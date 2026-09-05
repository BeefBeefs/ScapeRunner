namespace OSRSIdle;

/// <summary>
/// Screen-dependent measurements shared by controls. Values are recalculated
/// only when the available window dimensions actually change.
/// </summary>
public static class UiLayoutMetrics
{
    private static double _width = 450;
    private static double _height = 800;

    public static double NavigationBarHeight { get; private set; } = 53;
    public static double NavigationIconSize { get; private set; } = 32;
    public static double ActionButtonHeight { get; private set; } = 32;
    public static double TierBannerHeight { get; private set; } = 96;
    public static double CommonSpacing { get; private set; } = 8;

    public static bool Update(double width, double height)
    {
        if (width <= 0 || height <= 0 ||
            (Math.Abs(width - _width) < 0.5 && Math.Abs(height - _height) < 0.5))
        {
            return false;
        }

        _width = width;
        _height = height;

        double shortEdge = Math.Min(width, height);
        NavigationBarHeight = Math.Clamp(shortEdge * 0.118, 49, 58);
        NavigationIconSize = Math.Clamp(NavigationBarHeight - 21, 28, 36);
        ActionButtonHeight = Math.Clamp(shortEdge * 0.082, 32, 40);
        TierBannerHeight = Math.Clamp(shortEdge * 0.24, 88, 106);
        CommonSpacing = Math.Clamp(shortEdge * 0.02, 6, 10);
        return true;
    }
}
