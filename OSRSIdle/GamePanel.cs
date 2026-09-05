namespace OSRSIdle;

public static class GamePanel
{
    public static Border Create(View content, double padding = 10)
    {
        return new Border
        {
            Content = content,
            Padding = padding,
            BackgroundColor = Color.FromArgb("#E64A4A4A"),
            Stroke = Color.FromArgb("#D99032"),
            StrokeThickness = 2,
            VerticalOptions = LayoutOptions.Start
        };
    }
}
