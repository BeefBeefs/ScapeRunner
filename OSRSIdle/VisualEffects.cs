namespace OSRSIdle;

public static class VisualEffects
{
    public static async Task PulseAsync(VisualElement target, double scale = 1.06, uint duration = 140)
    {
        try
        {
            target.AbortAnimation("visualPulse");
            await target.ScaleToAsync(scale, duration, Easing.CubicOut);
            await target.ScaleToAsync(1, duration + 40, Easing.CubicIn);
        }
        catch
        {
            target.Scale = 1;
        }
    }

    public static async Task ShowFloatingTextAsync(
        AbsoluteLayout host,
        string text,
        Color color,
        double x = 0.5,
        double y = 0.4)
    {
        Label label = new()
        {
            Text = text,
            TextColor = color,
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center,
            WidthRequest = 150,
            HeightRequest = 30,
            Opacity = 0,
            InputTransparent = true
        };
        AbsoluteLayout.SetLayoutFlags(label, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.PositionProportional);
        AbsoluteLayout.SetLayoutBounds(label, new Rect(x, y, 150, 30));
        host.Children.Add(label);
        try
        {
            await Task.WhenAll(
                label.FadeToAsync(1, 90),
                label.TranslateToAsync(0, -18, 90, Easing.CubicOut));
            await Task.WhenAll(
                label.FadeToAsync(0, 420, Easing.CubicIn),
                label.TranslateToAsync(0, -48, 420, Easing.CubicIn));
        }
        finally
        {
            host.Children.Remove(label);
        }
    }
}
